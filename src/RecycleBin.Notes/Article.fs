[<RequireQualifiedAccess>]
module RecycleBin.Notes.Article

open System
open Microsoft.JSInterop
open Bolero
open Bolero.Json
open Elmish

type private ArticleTemplate = Template<const(__SOURCE_DIRECTORY__ + "/Article.html")>

type Model = {
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
   | GetArticle of GitHub.GitHubFilePath
   | GotCommits of GitHub.RepositoryCommits.Root[] * GitHub.GitHubFilePath
   | GotContents of GitHub.RepositoryFileContents.Root
   | Highlight
   | HashSmoothScroll
   | JsInvoked
   | Error of exn
   
let init (path:GitHub.GitHubFilePath) : Model * Cmd<Message> =
   let model = {
      Title = ""
      ContentsHtml = ""
      LatestCommitHash = None
      Date = None
      Published = None
      LastUpdated = None
   }
   let cmd = Cmd.ofMsg <| GetArticle(path)
   model, cmd

let update (github:GitHub.GitHubRestClient) (js:IJSRuntime) message model =
   match message with
   | GetArticle(path) ->
      let cmd = Cmd.ofAsync github.GetFileHistoryAsync path (fun res -> GotCommits(res, path)) Error
      model, cmd
   | GotCommits([|commit|], path) ->
      let model = {
         model with
            Published = Some(commit.Commit.Author.Date.DateTime)
            LastUpdated = None
      }
      let cmd = Cmd.ofAsync github.GetFileContentsAsync path GotContents Error
      model, cmd
   | GotCommits(history, path) ->
      let model = {
         model with
            Published = Some(history.[history.Length - 1].Commit.Author.Date.DateTime)
            LastUpdated = Some(history.[0].Commit.Author.Date.DateTime)
      }
      let cmd = Cmd.ofAsync github.GetFileContentsAsync path GotContents Error
      model, cmd
   | GotContents(contents) ->
      let cmd = Cmd.batch [
         Cmd.ofMsg HashSmoothScroll
         Cmd.ofMsg Highlight
      ]
      match GitHub.decodeContent contents.Content |> Markdown.parseMarkdown with
      | Markdown.Document(html, _, Some(header)) ->
         { model with Title = header.Title; Date = Option.ofObj header.Date; ContentsHtml = html }, cmd
      | Markdown.Document(html, _, None) ->
         { model with Title = "(No Title)"; Date = None; ContentsHtml = html }, cmd
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
                  | None -> Html.empty
      )
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
