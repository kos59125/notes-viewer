module RecycleBin.Notes.App

open System
open System.Web
open Microsoft.AspNetCore.Components
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Options
open Bolero
open Elmish

#if DEBUG
open System.Net.Http
open System.Text.Json
//open Bolero.Templating.Client
#endif

type private AppTemplate = Template<const(__SOURCE_DIRECTORY__ + "/App.html")>

type Page =
   | EmptyPage
   | Explorer of string option
   | Article of string

type GitHubRateLimit = {
   RateLimit : int
   RateLimitRemaining : int
   RateLimitReset : DateTimeOffset
}

#if DEBUG
type DebugModel = {
   ClientIPAddress : string
}
#endif

type Model = {
   Page : Page
   ShowDropdownMenu : bool
   Notification : Notification.Model list
   GitHubRateLimit : GitHubRateLimit option
#if DEBUG
   ShowDebug : bool
   DebugInfo : DebugModel
#endif
   // page contents
   Explorer : AsyncModel<Explorer.Model>
   Article : AsyncModel<Article.Model>
}

type Message =
   | SetPage of Page
   | InitializePage
   | HideDropdownMenu
   | ToggleDropdownMenu
   | ReceiveNotificationMessage of Notification.Message
   | Error of exn
   | GetGitHubRateLimit
#if DEBUG
   | ToggleDebugView
   | GetClientIPAddress
   | GotClientIPAddress of string
#endif
   // Explorer
   | StartExplorer of string option
   | ReceiveExplorerMessage of Explorer.Message
   // Article
   | StartArticle of string
   | ReceiveArticleMessage of Article.Message

//let router = Router.infer SetPage (fun model -> model.Page)

let private parseQueryString (path:string) =
   let url = Uri(Uri("https://www.example.com"), path)  // dummy host
   let query = HttpUtility.ParseQueryString(url.Query)
   query.AllKeys
   |> Array.fold (fun map key -> Map.add key query.[key] map) Map.empty

let (|QueryArticle|_|) (q:Map<string, string>) =
   Map.tryFind "path" q |> Option.map (HttpUtility.UrlDecode >> Article)
let (|QueryExplorer|) (q:Map<string, string>) =
   Map.tryFind "dir" q |> Option.map HttpUtility.UrlDecode |> Explorer

let router : Router<Page, Model, Message> = {
   getEndPoint = fun model ->
      model.Page
   setRoute = fun path ->
      match parseQueryString path with
      | QueryArticle(article) -> SetPage(article) |> Some
      | QueryExplorer(explorer) -> SetPage(explorer) |> Some
   getRoute = function
      | EmptyPage -> ""
      | Explorer(None) -> "/"
      | Explorer(Some(path)) -> sprintf "/?dir=%s" path
      | Article(path) -> sprintf "/?path=%s" <| HttpUtility.UrlEncode(path)
}

let init () =
   let model = {
      Page = EmptyPage
      ShowDropdownMenu = false
      Notification = []
      Explorer = Loading
      Article = Loading
      GitHubRateLimit = None
#if DEBUG
      ShowDebug = false
      DebugInfo = {
         ClientIPAddress = ""
      }
#endif
   }
   let cmd = Cmd.none
   model, cmd

let private updateExplorer model (explorer, cmd) =
   { model with Explorer = Loaded(explorer) }, Cmd.map ReceiveExplorerMessage cmd
let private updateArticle model (article, cmd) =
   { model with Article = Loaded(article) }, Cmd.map ReceiveArticleMessage cmd

let private getGitHubClient (program:ProgramComponent<_, _>) =
   program.Services.GetService<GitHub.GitHubRestClient>()

let private getGitHubRateLimit (model, cmd) = model, Cmd.batch [cmd; Cmd.ofMsg GetGitHubRateLimit]

#if DEBUG
let private getHttpClient (program:ProgramComponent<_, _>) =
   let http = program.Services.GetService<IHttpClientFactory>()
   http.CreateClient()

let private getClientIpAddress (client:HttpClient) = async {
   let! response = client.GetAsync("https://httpbin.org/ip") |> Async.AwaitTask
   use! json = response.EnsureSuccessStatusCode().Content.ReadAsStreamAsync() |> Async.AwaitTask
   let! document = JsonDocument.ParseAsync(json) |> Async.AwaitTask
   return document.RootElement.GetProperty("origin").GetString()
}
#endif

let update options program message model =
   match message with
   | SetPage(page) ->
      if model.Page = page then
         model, Cmd.none
      else
         let cmd = Cmd.batch [
            Cmd.ofMsg HideDropdownMenu
            Cmd.ofMsg InitializePage
         ]
         { model with Page = page }, cmd
   | InitializePage ->
      match model.Page with
      | EmptyPage -> model, Cmd.none
      | Explorer(path) -> { model with Explorer = Loading }, Cmd.ofMsg <| StartExplorer(path)
      | Article(path) -> { model with Article = Loading }, Cmd.ofMsg <| StartArticle(path)
   | HideDropdownMenu ->
      { model with ShowDropdownMenu = false }, Cmd.none
   | ToggleDropdownMenu ->
      { model with ShowDropdownMenu = not model.ShowDropdownMenu }, Cmd.none
   | ReceiveNotificationMessage(message) ->
      match List.map (Notification.update message) model.Notification with
      | [] -> model, Cmd.none
      | notification -> { model with Notification = notification }, Cmd.none
   | Error(ex) ->
      { model with Notification = Notification.error ex.Message :: model.Notification }, Cmd.ofMsg GetGitHubRateLimit
#if DEBUG
   | ToggleDebugView ->
      let cmd =
         match model.DebugInfo.ClientIPAddress, model.ShowDebug with
         | "", false -> Cmd.ofMsg GetClientIPAddress
         | _ -> Cmd.none
      { model with ShowDebug = not model.ShowDebug }, cmd
   | GetClientIPAddress ->
      let client = getHttpClient program
      let cmd = Cmd.ofAsync getClientIpAddress client GotClientIPAddress Error
      model, cmd
   | GotClientIPAddress(ip) ->
      let info = {
         model.DebugInfo with
            ClientIPAddress = ip
      }
      { model with DebugInfo = info }, Cmd.none
#endif
   | GetGitHubRateLimit ->
      let github = getGitHubClient program
      let rateLimit = {
         RateLimit = github.RateLimit
         RateLimitRemaining = github.RateLimitRemaining
         RateLimitReset = github.RateLimitReset
      }
      { model with GitHubRateLimit = Some(rateLimit) }, Cmd.none
   // Explorer
   | StartExplorer(path) ->
      let path = GitHub.makeFilePath options.Owner options.Repository (Option.defaultValue "" path)
      Explorer.init path |> updateExplorer model |> getGitHubRateLimit
   | ReceiveExplorerMessage(Explorer.Message.Error(ex)) ->
      model, Cmd.ofMsg <| Error(ex)
   | ReceiveExplorerMessage(Explorer.ReceiveFileMessage(index, Explorer.File.GotFile(file)) as message) ->
      let github = getGitHubClient program
      let url = Uri(router.Link(Article(file.Path)), UriKind.Relative)
      let setLink = Cmd.ofMsg (Explorer.File.SetLink(url)) |> Cmd.map (fun cmd -> Explorer.ReceiveFileMessage(index, cmd))
      match AsyncModel.map (Explorer.update github message) model.Explorer with
      | Loaded(explorer, cmd) -> updateExplorer model (explorer, Cmd.batch [setLink; cmd]) |> getGitHubRateLimit
      | _ -> model, Cmd.map ReceiveExplorerMessage setLink
   | ReceiveExplorerMessage(Explorer.ReceiveFileMessage(index, Explorer.File.GotDirectory(directory)) as message) ->
      let github = getGitHubClient program
      let url = Uri(router.Link(Explorer(Some(directory.Path))), UriKind.Relative)
      let setLink = Cmd.ofMsg (Explorer.File.SetLink(url)) |> Cmd.map (fun cmd -> Explorer.ReceiveFileMessage(index, cmd))
      match AsyncModel.map (Explorer.update github message) model.Explorer with
      | Loaded(explorer, cmd) -> updateExplorer model (explorer, Cmd.batch [setLink; cmd]) |> getGitHubRateLimit
      | _ -> model, Cmd.map ReceiveExplorerMessage setLink
   | ReceiveExplorerMessage(Explorer.ReceiveFileMessage(explorer, Explorer.File.OpenFile(path))) ->
      let cmd = Cmd.ofMsg <| SetPage(Article(path.Path))
      model, cmd
   | ReceiveExplorerMessage(Explorer.ReceiveFileMessage(explorer, Explorer.File.OpenDirectory(path))) ->
      let cmd = Cmd.ofMsg <| SetPage(Explorer(Some(path.Path)))
      model, cmd
   | ReceiveExplorerMessage(message) ->
      let github = getGitHubClient program
      match AsyncModel.map (Explorer.update github message) model.Explorer with
      | Loaded(explorer, cmd) -> updateExplorer model (explorer, cmd)
      | _ -> model, Cmd.none
   // Article
   | StartArticle(path) ->
      let path = GitHub.makeFilePath options.Owner options.Repository path
      Article.init path |> updateArticle model |> getGitHubRateLimit
   | ReceiveArticleMessage(Article.Message.Error(ex)) ->
      model, Cmd.ofMsg <| Error(ex)
   | ReceiveArticleMessage(message) ->
      let github = getGitHubClient program
      match AsyncModel.map (Article.update github program.JSRuntime message) model.Article with
      | Loaded(article, cmd) -> updateArticle model (article, cmd)
      | _ -> model, Cmd.none

let homeLink =
   Html.a [router.HRef(Explorer(None))] [
      Html.span [Html.attr.classes ["icon"; "is-small"]] [
         Html.i [Html.attr.classes ["fas"; "fa-home"]] []
      ]
      Html.span [] [Html.text "Home"]
   ]

let view options model dispatch =
   AppTemplate()
      .Title(options.Title)
      .Copyright(options.Copyright)
      .GitHub(
         AppTemplate.GitHubAccountLink()
            .GitHubAccountUrl(options.GitHub)
            .Elt()
      )
      .Twitter(
         AppTemplate.TwitterAccountLink()
            .TwitterAccountUrl(options.Twitter)
            .Elt()
      )
      .IsDropdownActive(if model.ShowDropdownMenu then "is-active" else "")
      .ToggleDropdownMenu(fun _ -> dispatch ToggleDropdownMenu)
      .Breadcrumb(
         Html.cond model.Page <| function
            | EmptyPage -> Html.li [] [Html.ecomp<Loader.View, _, _> [] () ignore]
            | Explorer(None)
            | Explorer(Some("")) -> Html.li [Html.attr.classes ["is-active"]] [homeLink]
            | Explorer(Some(path))
            | Article(path) as current ->
               let breadcrumbs = GitHub.Path.breadcrumb path
               Html.concat [
                  yield Html.li [] [homeLink]
                  for index = 0 to breadcrumbs.Length - 2 do
                     yield Html.li [] [
                        Html.a [router.HRef(Explorer(Some(breadcrumbs.[index])))] [
                           Html.text (GitHub.Path.basename breadcrumbs.[index])
                        ]
                     ]
                  yield Html.li [Html.attr.classes ["is-active"]] [
                     Html.a [router.HRef(current); Attr("aria-current", "page")] [
                        Html.text (GitHub.Path.basename path)
                     ]
                  ] 
               ]
      )
      .Main(
         Html.cond model.Page <| function
            | EmptyPage -> Html.ecomp<Loader.View, _, _> [] () ignore
            | Explorer(_) -> Html.ecomp<Explorer.View, _, _> [] model.Explorer (ReceiveExplorerMessage >> dispatch)
            | Article(_) -> Html.ecomp<Article.View, _, _> [] model.Article (ReceiveArticleMessage >> dispatch)
      )
      .Notification(
         Html.forEach model.Notification <| fun notification ->
            Html.ecomp<Notification.View, _, _> [] notification (ReceiveNotificationMessage >> dispatch)
      )
      .GitHubRateLimit(
         Html.cond model.GitHubRateLimit <| function
            | None -> Html.empty
            | Some(rateLimit) ->
               AppTemplate.GitHubRateLimitInfo()
                  .RateLimit(string rateLimit.RateLimit)
                  .RateLimitRemaining(string rateLimit.RateLimitRemaining)
                  .RateLimitReset(rateLimit.RateLimitReset.ToLocalTime().ToString("yyyy-MM-dd'T'HH:mm:sszzz"))
                  .Elt()
      )
#if DEBUG
      .DebugButton(
         Html.span [Html.attr.classes ["navbar-item"]] [
            Html.button [Html.attr.classes ["button"; "is-light"]; Html.on.click (fun _ -> ToggleDebugView |> dispatch)] [
               Html.span [Html.attr.classes ["icon"]] [
                  Html.i [Html.attr.classes ["fas"; "fa-bug"]] []
               ]
               Html.span [] [Html.text (if model.ShowDebug then "Hide" else "Show")]
            ]
         ]
      )
      .DebugView(
         match model.ShowDebug with
         | false -> Html.empty
         | true ->
            AppTemplate.DebugInfo()
               .ClientIPAddress(model.DebugInfo.ClientIPAddress)
               .Elt()
      )
#endif
      .Elt()

type App() =
   inherit ProgramComponent<Model, Message>()

   override this.Program =
      let note = this.Services.GetService<IOptionsSnapshot<NoteOptions>>().Value
      Program.mkProgram (fun _ -> init ()) (update note this) (view note)
      |> Program.withRouter router
//#if DEBUG
//      |> Program.withHotReload
//#endif
