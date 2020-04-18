[<AutoOpen>]
module RecycleBin.Notes.JsExtern

open Microsoft.JSInterop

type IJSRuntime with

   member js.setTitle(title:string) =
      js.InvokeVoidAsync("RecycleBin.Notes.setTitle", title).AsTask() |> Async.AwaitTask

   member js.setIcon(icon:Icon) =
      js.InvokeVoidAsync("RecycleBin.Notes.setIcon", icon.IconUrl, icon.MediaType).AsTask() |> Async.AwaitTask

   member js.HighlightHtml() =
      js.InvokeVoidAsync("RecycleBin.Notes.highlight").AsTask() |> Async.AwaitTask

   member js.SmoothScrollToHash() =
      js.InvokeVoidAsync("RecycleBin.Notes.smoothScroll").AsTask() |> Async.AwaitTask
