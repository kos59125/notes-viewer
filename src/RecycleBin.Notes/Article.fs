[<RequireQualifiedAccess>]
module RecycleBin.Notes.Article

open System
open Microsoft.JSInterop
open Bolero
open Bolero.Json
open Elmish

type private ArticleTemplate = Template<const(__SOURCE_DIRECTORY__ + "/Article.html")>

type Model = {
   Path : GitHub.GitHubFilePath
   Title : string
   ContentsHtml : string
   LatestCommitHash : string option
   Date : string option
   [<DateTimeFormat("yyyy-MM-dd")>]
   Published : DateTime option
   [<DateTimeFormat("yyyy-MM-dd")>]
   LastUpdated : DateTime option
}

type Message =
   | GetArticle
   | GotCommits of GitHub.RepositoryCommits.Root[]
   | GotContents of GitHub.RepositoryFileContents.Root
   | RenderContents
   | Highlight
   | HashSmoothScroll
   | JsInvoked
   | Error of exn
   
let init (path:GitHub.GitHubFilePath) : Model * Cmd<Message> =
   let model = {
      Path = path
      Title = ""
      ContentsHtml = ""
      LatestCommitHash = None
      Date = None
      Published = None
      LastUpdated = None
   }
   let cmd = Cmd.ofMsg GetArticle
   model, cmd

let update (github:GitHub.GitHubRestClient) (js:IJSRuntime) message model =
   match message with
   | GetArticle ->
      let cmd = Cmd.ofAsync github.GetFileHistoryAsync model.Path GotCommits Error
      model, cmd
   | GotCommits([|commit|]) ->
      let model = {
         model with
            Published = Some(commit.Commit.Author.Date.DateTime)
            LastUpdated = None
      }
      let cmd = Cmd.ofAsync github.GetFileContentsAsync model.Path GotContents Error
      model, cmd
   | GotCommits(history) ->
      let model = {
         model with
            Published = Some(history.[history.Length - 1].Commit.Author.Date.DateTime)
            LastUpdated = Some(history.[0].Commit.Author.Date.DateTime)
      }
      let cmd = Cmd.ofAsync github.GetFileContentsAsync model.Path GotContents Error
      model, cmd
   | GotContents(contents) ->
      match GitHub.decodeContent contents.Content |> Markdown.parseMarkdown with
      | Markdown.Document(html, _, Some(header)) ->
         { model with Title = header.Title; Date = Option.ofObj header.Date; ContentsHtml = html }, Cmd.ofMsg RenderContents
      | Markdown.Document(html, _, None) ->
         { model with Title = "(No Title)"; Date = None; ContentsHtml = html }, Cmd.ofMsg RenderContents
   | RenderContents ->
      let cmd = Cmd.batch [
         Cmd.ofMsg HashSmoothScroll
         Cmd.ofMsg Highlight
      ]
      model, cmd
   | Highlight ->
      let cmd = Cmd.ofAsync js.HighlightHtml ()  (fun _ -> JsInvoked) Error
      model, cmd
   | HashSmoothScroll ->
      let cmd = Cmd.ofAsync js.SmoothScrollToHash () (fun _ -> JsInvoked) Error
      model, cmd
   | JsInvoked ->
      model, Cmd.none  // do nothing
   | Error(_) ->
      model, Cmd.none  // propagate

let private viewDate date =
   Html.small [] [
      Html.span [Html.attr.classes ["icon"]] [
         Html.i [Html.attr.classes ["fas"; "fa-calendar-alt"]] []
      ]
      Html.text date
   ]

let view model dispatch =
   ArticleTemplate.Contents()
      .Title(model.Title)
      .Date(
         Html.cond model.Date <| function
            | Some(date) -> viewDate date
            | None ->
               Html.cond model.LastUpdated <| function
                  | Some(date) -> viewDate <| date.ToString("yyyy-MM-dd")
                  | None ->
                     Html.cond model.Published <| function
                        | Some(date) -> viewDate <| date.ToString("yyyy-MM-dd")
                        | None -> Html.empty
      )
      .SourceCodeUrl(sprintf "https://github.com/%s/%s/tree/master/%s" model.Path.Owner model.Path.Repository model.Path.Path)
      .Contents(Node.RawHtml(model.ContentsHtml))
      .Elt()

let private notFound model dispatch =
   ArticleTemplate.NotFound()
      .Elt()

type View() =
   inherit ElmishComponent<AsyncModel<Model>, Message>()
   override _.View model dispatch =
      Html.cond model <| function
         | Loading -> Html.ecomp<Loader.View, _, _> [] () ignore
         | Loaded(model) -> view model dispatch
         | LoadError(ex) -> notFound model dispatch
