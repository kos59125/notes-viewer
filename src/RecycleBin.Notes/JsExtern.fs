[<AutoOpen>]
module RecycleBin.Notes.JsExtern

open Microsoft.JSInterop

type IJSRuntime with

   member js.HighlightHtml() =
      js.InvokeVoidAsync("RecycleBin.Notes.highlight").AsTask() |> Async.AwaitTask

   member js.SmoothScrollToHash() =
      js.InvokeVoidAsync("RecycleBin.Notes.smoothScroll").AsTask() |> Async.AwaitTask
