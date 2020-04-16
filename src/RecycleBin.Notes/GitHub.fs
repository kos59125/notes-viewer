module RecycleBin.Notes.GitHub

open System
open System.Collections.Generic
open System.Collections.Concurrent
open System.Net.Http
open System.Net.Http.Headers
open System.Text
open Microsoft.Extensions.Options
open FSharp.Data

type GitHubFilePath = {
   Owner : string
   Repository : string
   Path : string
}

let emptyFilePath = {
   Owner = ""
   Repository = ""
   Path = ""
}

let makeFilePath owner repo path = {
   Owner = owner
   Repository = repo
   Path = path
}

type RepositoryCommits = JsonProvider<"""[
  {
    "url": "https://api.github.com/repos/octocat/Hello-World/commits/6dcb09b5b57875f334f61aebed695e2e4193db5e",
    "sha": "6dcb09b5b57875f334f61aebed695e2e4193db5e",
    "node_id": "MDY6Q29tbWl0NmRjYjA5YjViNTc4NzVmMzM0ZjYxYWViZWQ2OTVlMmU0MTkzZGI1ZQ==",
    "html_url": "https://github.com/octocat/Hello-World/commit/6dcb09b5b57875f334f61aebed695e2e4193db5e",
    "comments_url": "https://api.github.com/repos/octocat/Hello-World/commits/6dcb09b5b57875f334f61aebed695e2e4193db5e/comments",
    "commit": {
      "url": "https://api.github.com/repos/octocat/Hello-World/git/commits/6dcb09b5b57875f334f61aebed695e2e4193db5e",
      "author": {
        "name": "Monalisa Octocat",
        "email": "support@github.com",
        "date": "2011-04-14T16:00:49Z"
      },
      "committer": {
        "name": "Monalisa Octocat",
        "email": "support@github.com",
        "date": "2011-04-14T16:00:49Z"
      },
      "message": "Fix all the bugs",
      "tree": {
        "url": "https://api.github.com/repos/octocat/Hello-World/tree/6dcb09b5b57875f334f61aebed695e2e4193db5e",
        "sha": "6dcb09b5b57875f334f61aebed695e2e4193db5e"
      },
      "comment_count": 0,
      "verification": {
        "verified": false,
        "reason": "unsigned",
        "signature": null,
        "payload": null
      }
    },
    "author": {
      "login": "octocat",
      "id": 1,
      "node_id": "MDQ6VXNlcjE=",
      "avatar_url": "https://github.com/images/error/octocat_happy.gif",
      "gravatar_id": "",
      "url": "https://api.github.com/users/octocat",
      "html_url": "https://github.com/octocat",
      "followers_url": "https://api.github.com/users/octocat/followers",
      "following_url": "https://api.github.com/users/octocat/following{/other_user}",
      "gists_url": "https://api.github.com/users/octocat/gists{/gist_id}",
      "starred_url": "https://api.github.com/users/octocat/starred{/owner}{/repo}",
      "subscriptions_url": "https://api.github.com/users/octocat/subscriptions",
      "organizations_url": "https://api.github.com/users/octocat/orgs",
      "repos_url": "https://api.github.com/users/octocat/repos",
      "events_url": "https://api.github.com/users/octocat/events{/privacy}",
      "received_events_url": "https://api.github.com/users/octocat/received_events",
      "type": "User",
      "site_admin": false
    },
    "committer": {
      "login": "octocat",
      "id": 1,
      "node_id": "MDQ6VXNlcjE=",
      "avatar_url": "https://github.com/images/error/octocat_happy.gif",
      "gravatar_id": "",
      "url": "https://api.github.com/users/octocat",
      "html_url": "https://github.com/octocat",
      "followers_url": "https://api.github.com/users/octocat/followers",
      "following_url": "https://api.github.com/users/octocat/following{/other_user}",
      "gists_url": "https://api.github.com/users/octocat/gists{/gist_id}",
      "starred_url": "https://api.github.com/users/octocat/starred{/owner}{/repo}",
      "subscriptions_url": "https://api.github.com/users/octocat/subscriptions",
      "organizations_url": "https://api.github.com/users/octocat/orgs",
      "repos_url": "https://api.github.com/users/octocat/repos",
      "events_url": "https://api.github.com/users/octocat/events{/privacy}",
      "received_events_url": "https://api.github.com/users/octocat/received_events",
      "type": "User",
      "site_admin": false
    },
    "parents": [
      {
        "url": "https://api.github.com/repos/octocat/Hello-World/commits/6dcb09b5b57875f334f61aebed695e2e4193db5e",
        "sha": "6dcb09b5b57875f334f61aebed695e2e4193db5e"
      }
    ]
  }
]""">

type private RepositoryDirectoryContents = JsonProvider<"""[
  {
    "type": "file",
    "size": 625,
    "name": "octokit.rb",
    "path": "lib/octokit.rb",
    "sha": "fff6fe3a23bf1c8ea0692b4a883af99bee26fd3b",
    "url": "https://api.github.com/repos/octokit/octokit.rb/contents/lib/octokit.rb",
    "git_url": "https://api.github.com/repos/octokit/octokit.rb/git/blobs/fff6fe3a23bf1c8ea0692b4a883af99bee26fd3b",
    "html_url": "https://github.com/octokit/octokit.rb/blob/master/lib/octokit.rb",
    "download_url": "https://raw.githubusercontent.com/octokit/octokit.rb/master/lib/octokit.rb",
    "_links": {
      "self": "https://api.github.com/repos/octokit/octokit.rb/contents/lib/octokit.rb",
      "git": "https://api.github.com/repos/octokit/octokit.rb/git/blobs/fff6fe3a23bf1c8ea0692b4a883af99bee26fd3b",
      "html": "https://github.com/octokit/octokit.rb/blob/master/lib/octokit.rb"
    }
  },
  {
    "type": "dir",
    "size": 0,
    "name": "octokit",
    "path": "lib/octokit",
    "sha": "a84d88e7554fc1fa21bcbc4efae3c782a70d2b9d",
    "url": "https://api.github.com/repos/octokit/octokit.rb/contents/lib/octokit",
    "git_url": "https://api.github.com/repos/octokit/octokit.rb/git/trees/a84d88e7554fc1fa21bcbc4efae3c782a70d2b9d",
    "html_url": "https://github.com/octokit/octokit.rb/tree/master/lib/octokit",
    "download_url": null,
    "_links": {
      "self": "https://api.github.com/repos/octokit/octokit.rb/contents/lib/octokit",
      "git": "https://api.github.com/repos/octokit/octokit.rb/git/trees/a84d88e7554fc1fa21bcbc4efae3c782a70d2b9d",
      "html": "https://github.com/octokit/octokit.rb/tree/master/lib/octokit"
    }
  }
]""">

type private RepositoryFileContents = JsonProvider<"""{
  "type": "file",
  "encoding": "base64",
  "size": 5362,
  "name": "README.md",
  "path": "README.md",
  "content": "encoded content ...",
  "sha": "3d21ec53a331a6f037a91c368710b99387d012c1",
  "url": "https://api.github.com/repos/octokit/octokit.rb/contents/README.md",
  "git_url": "https://api.github.com/repos/octokit/octokit.rb/git/blobs/3d21ec53a331a6f037a91c368710b99387d012c1",
  "html_url": "https://github.com/octokit/octokit.rb/blob/master/README.md",
  "download_url": "https://raw.githubusercontent.com/octokit/octokit.rb/master/README.md",
  "_links": {
    "git": "https://api.github.com/repos/octokit/octokit.rb/git/blobs/3d21ec53a331a6f037a91c368710b99387d012c1",
    "self": "https://api.github.com/repos/octokit/octokit.rb/contents/README.md",
    "html": "https://github.com/octokit/octokit.rb/blob/master/README.md"
  }
}""">

type GitHubRestClient(http:IHttpClientFactory, options:IOptionsSnapshot<NoteOptions>) =

   let buildQueryString (query:seq<string * string>) = async {
      use content = new FormUrlEncodedContent(Seq.map (fun (key, value) -> KeyValuePair(key, value)) query)
      return! content.ReadAsStringAsync() |> Async.AwaitTask
   }

   let cache = ConcurrentDictionary<Uri, string>()

   let mutable rateLimit = 0
   let mutable rateLimitRemaining = 0
   let mutable rateLimitReset = DateTimeOffset.UnixEpoch

   member _.RateLimit
      with get () = rateLimit
      and private set value = rateLimit <- value
   
   member _.RateLimitRemaining
      with get () = rateLimitRemaining
      and private set value = rateLimitRemaining <- value
   
   member _.RateLimitReset
      with get () = rateLimitReset
      and private set value = rateLimitReset <- value

   member val AccessToken : string option = Option.ofObj options.Value.AccessToken

   member private this.GetAsync (url:string, ?query:(string * string) list) = async {
      let! query = defaultArg query [] |> buildQueryString
      let builder = UriBuilder(url, Query =  query)
      match cache.TryGetValue(builder.Uri) with
      | false, _ ->
         let client = http.CreateClient()
         match this.AccessToken with
         | Some(token) -> client.DefaultRequestHeaders.Authorization <- AuthenticationHeaderValue("token", token)
         | None -> ()
         let! response = client.GetAsync(builder.Uri) |> Async.AwaitTask
         this.RateLimit <- response.Headers.GetValues("X-RateLimit-Limit") |> Seq.head |> int
         this.RateLimitRemaining <- response.Headers.GetValues("X-RateLimit-Remaining") |> Seq.head |> int
         this.RateLimitReset <- response.Headers.GetValues("X-RateLimit-Reset") |> Seq.head |> int64 |> DateTimeOffset.FromUnixTimeSeconds
         let response = response.EnsureSuccessStatusCode()
         let! content = response.Content.ReadAsStringAsync() |> Async.AwaitTask
         cache.TryAdd(builder.Uri, content) |> ignore
         return content
      | true, content ->
         return content
   }
   
   member this.GetFileHistoryAsync path = async {
      let url = sprintf "https://api.github.com/repos/%s/%s/commits" path.Owner path.Repository
      let query = ["path", path.Path]
      let! content = this.GetAsync(url, query)
      return RepositoryCommits.Parse(content)
   }

   member this.ListDirectoryAsync path = async {
      let url = sprintf "https://api.github.com/repos/%s/%s/contents/%s" path.Owner path.Repository path.Path
      let! content = this.GetAsync(url)
      return RepositoryDirectoryContents.Parse(content)
   }

   member this.GetFileContentsAsync path = async {
      let url = sprintf "https://api.github.com/repos/%s/%s/contents/%s" path.Owner path.Repository path.Path
      let! content = this.GetAsync(url)
      return RepositoryFileContents.Parse(content)
   }

let decodeContent = Convert.FromBase64String >> Encoding.UTF8.GetString

[<RequireQualifiedAccess>]
module Path =
   [<Literal>]
   let PathSeparator = '/'
   let join (dir:string) (file:string) = sprintf "%s/%s" dir file
   let dirname (path:string) =
      let lastIndex = path.LastIndexOf(PathSeparator)
      path.[..lastIndex - 1]
   let basename (path:string) =
      let lastIndex = path.LastIndexOf(PathSeparator)
      path.[lastIndex + 1..]
   let breadcrumb (path:string) =
      match path.Split(PathSeparator, StringSplitOptions.RemoveEmptyEntries) with
      | [||] -> [||]
      | [|_|] as r -> r
      | segments ->
         for index = 1 to segments.Length - 1 do
            segments.[index] <- join segments.[index - 1] segments.[index]
         segments
