[<RequireQualifiedAccess>]
module RecycleBin.Notes.Explorer

open System
open Bolero
open Elmish

type private ExplorerTemplate = Template<const(__SOURCE_DIRECTORY__ + "/Explorer.html")>

module File =
   type FileDisplay =
      | Unknown
      | File of string * string
      | Directory of string

   type Model = {
      Path : GitHub.GitHubFilePath
      LinkUrl : string
      FileDisplay : FileDisplay
      ErrorMessage : string option
   }

   type Message =
      | SetLink of Uri
      | OpenFile of GitHub.GitHubFilePath
      | OpenDirectory of GitHub.GitHubFilePath
      | GetFile of GitHub.RepositoryDirectoryContents.Root
      | GotFile of GitHub.RepositoryFileContents.Root
      | GotDirectory of GitHub.RepositoryDirectoryContents.Root
      | Error of exn

   let init (path, file) =
      let model = {
         Path = path
         LinkUrl = ""
         FileDisplay = Unknown
         ErrorMessage = None
      }
      let cmd = Cmd.ofMsg <| GetFile(file)
      model, cmd

   let update (github:GitHub.GitHubRestClient) message model =
      match message with
      | SetLink(url) ->
         { model with LinkUrl = url.ToString() }, Cmd.none
      | OpenFile(_)
      | OpenDirectory(_) ->
         model, Cmd.none  // propagate
      | GetFile(file) ->
         if file.Type = "file" then
            let cmd = Cmd.ofAsync github.GetFileContentsAsync model.Path GotFile Error
            model, cmd
         else
            let cmd = Cmd.ofMsg <| GotDirectory(file)
            model, cmd
      | GotFile(file) ->
         match GitHub.decodeContent file.Content |> Markdown.parseMarkdown with
         | Markdown.Document(_, digest, Some(header)) ->
            { model with FileDisplay = File(header.Title, digest) }, Cmd.none
         | Markdown.Document(_, digest, None) ->
            { model with FileDisplay = File("(No Title)", digest) }, Cmd.none
      | GotDirectory(directory) ->
         { model with FileDisplay = Directory(directory.Name) }, Cmd.none
      | Error(ex) ->
         { model with ErrorMessage = Some(ex.Message) } , Cmd.none  // propagate

   let view model dispatch =
      Html.cond model.ErrorMessage <| function
         | None ->
            Html.cond model.FileDisplay <| function
               | Unknown ->
                  Html.article [Html.attr.classes ["media"]] [
                     Html.div [Html.attr.classes ["media-content"]] [
                        Html.ecomp<Loader.View, _, _> [] () ignore
                     ]
                  ]
               | File(title, digest) ->
                  ExplorerTemplate.File()
                     .Title(title)
                     .ContentsDigest(digest)
                     .ReadMore(fun _ -> OpenFile(model.Path) |> dispatch)
                     .LinkUrl(model.LinkUrl)
                     .Elt()
               | Directory(name) ->
                  ExplorerTemplate.Directory()
                     .Name(name)
                     .OpenDirectory(fun _ -> OpenDirectory(model.Path) |> dispatch)
                     .LinkUrl(model.LinkUrl)
                     .Elt()
         | Some(message) ->
            ExplorerTemplate.LoadFailed()
               .Reason(message)
               .Elt()

   type View() =
      inherit ElmishComponent<Model, Message>()
      override _.View model dispatch = view model dispatch

type Model = {
   Path : GitHub.GitHubFilePath
   DirectoryContents : GitHub.RepositoryDirectoryContents.Root[]
   DisplayFileList : File.Model[]
   Pager : Pagination.Model option
}

type Message =
   | GetFileList
   | GotFileList of GitHub.RepositoryDirectoryContents.Root[]
   | GetFileContents
   | ReceiveFileMessage of int * File.Message
   | ReceivePagerMessage of Pagination.Message
   | Error of exn

let init path : Model * Cmd<Message> =
   let model = {
      Path = path
      DirectoryContents = Array.empty
      DisplayFileList = Array.empty
      Pager = None
   }
   let cmd = Cmd.ofMsg GetFileList
   model, cmd

let filterFile (file: GitHub.RepositoryDirectoryContents.Root) =
   not (file.Name.StartsWith("."))
   && not (file.Name.ToUpperInvariant().StartsWith("README"))
   && (
      file.Type = "dir"
      || (file.Type = "file" && Markdown.isMarkdownFileName file.Path)
   )

let update (github:GitHub.GitHubRestClient) message model =
   match message with
   | GetFileList ->
      let cmd = Cmd.ofAsync github.ListDirectoryAsync model.Path GotFileList Error
      { model with DirectoryContents = Array.empty; Pager = None }, cmd
   | GotFileList(files) ->
      let fileList =
         files
         |> Array.filter filterFile
         |> Array.sortWith (fun f1 f2 ->
            match f1.Type, f2.Type with
            | "dir", "dir"
            | "file", "file" -> compare f1.Name f2.Name
            | "dir", "file" -> -1
            | "file", "dir" -> 1
            | _, _ -> failwith "never"
         )
      let pager = Pagination.init 10 fileList.Length
      let cmd = Cmd.ofMsg GetFileContents
      { model with DirectoryContents = fileList; Pager = Some(pager) }, cmd
   | GetFileContents ->
      match model.Pager with
      | None -> model, Cmd.none
      | Some(pager) ->
         let files = model.DirectoryContents |> Pagination.fetch pager
         let modelList, cmdList =
            Array.mapFoldBack (fun (file:GitHub.RepositoryDirectoryContents.Root) cmdList ->
               let path = { model.Path with Path = file.Path }
               let model, cmd = File.init (path, file)
               model, cmd::cmdList
            ) files []
         let cmdList = cmdList |> List.mapi (fun index -> Cmd.map (fun cmd -> ReceiveFileMessage(index, cmd)))
         { model with DisplayFileList = modelList }, Cmd.batch cmdList
   | ReceiveFileMessage(index, message) ->
      let articleDigest, cmd = File.update github message model.DisplayFileList.[index]
      model.DisplayFileList.[index] <- articleDigest
      model, Cmd.map (fun cmd -> ReceiveFileMessage(index, cmd)) cmd
   | ReceivePagerMessage(message) ->
      match Option.map (Pagination.update message) model.Pager with
      | None -> model, Cmd.none
      | Some(pager) -> { model with Pager = Some(pager) }, Cmd.ofMsg GetFileContents
   | Error(_) ->
      model, Cmd.none  // propagate

let view model dispatch =
   ExplorerTemplate()
      .FileList(
         model.DisplayFileList
         |> Array.mapi (fun index article ->
            Html.ecomp<File.View, _, _> [] article (fun cmd -> ReceiveFileMessage(index, cmd) |> dispatch)
         )
         |> fun articleList -> Html.forEach articleList id
      )
      .Pager(
         Html.cond model.Pager <| function
            | None -> Html.empty
            | Some(pager) -> Html.ecomp<Pagination.View, _, _> [] pager (ReceivePagerMessage >> dispatch)
      )
      .Elt()
      
let private notFound model dispatch =
   ExplorerTemplate.NotFound()
      .Elt()

type View() =
   inherit ElmishComponent<AsyncModel<Model>, Message>()
   override _.View model dispatch =
      Html.cond model <| function
         | Loading -> Html.ecomp<Loader.View, _, _> [] () ignore
         | Loaded(model) -> view model dispatch
         | LoadError(ex) -> notFound model dispatch
