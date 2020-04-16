namespace RecycleBin.Notes

open System
open Bolero

[<RequireQualifiedAccess>]
module Loader =
   type View() =
      inherit ElmishComponent<unit, unit>()
      override _.View model dispatch =
         Html.concat [
            Html.span [] [Html.text "Loading..."]
            Html.span [Html.attr.classes ["icon"]] [
               Html.i [Html.attr.classes ["fas"; "fa-spinner"; "fa-spin"]] []
            ]
         ]

[<RequireQualifiedAccess>]
module Notification =
   type NotificationStyle =
      | Danger
      | Warning
      | Success
      | Info

   type Model = {
      Shown : bool
      Message : string
      Style : NotificationStyle
   }

   let error message = { Shown = true; Message = message; Style = Danger }
   let warning message = { Shown = true; Message = message; Style = Warning }
   let info message = { Shown = true; Message = message; Style = Info }
   let success message = { Shown = true; Message = message; Style = Success }

   type Message =
      | ShowNotification
      | CloseNotification
      | SetMessage of string
      | SetStyle of NotificationStyle

   let update message model =
      match message with
      | ShowNotification -> { model with Shown = true }
      | CloseNotification -> { model with Shown = false }
      | SetMessage(message) -> { model with Message = message }
      | SetStyle(style) -> { model with Style = style }

   let show = update ShowNotification
   let close = update CloseNotification

   let private getStyleClass = function
      | Danger -> "is-danger"
      | Warning -> "is-warning"
      | Success -> "is-success"
      | Info -> "is-info"

   let view model dispatch =
      Html.cond model.Shown <| function
         | true ->
            Html.div [Html.attr.classes ["notification"; getStyleClass model.Style]] [
               Html.button [Html.attr.classes ["delete"]; Attr("aria-label", "close"); Html.on.click (fun _ -> CloseNotification |> dispatch)] []
               Html.text model.Message
            ]
         | false -> Html.empty

   type View() =
      inherit ElmishComponent<Model, Message>()
      override _.View model dispatch = view model dispatch

[<RequireQualifiedAccess>]
module Pagination =
   type Page = {
      PageIndex : int
      Offset : int
      ItemCount : int
   }
   type Model = {
      CurrentPage : int
      PageSize : int
      PageList : Page[]
      TotalItemCount : int
   }
   
   type Message =
      | SetPage of int

   let fetch pager (items:'a[]) =
      if pager.TotalItemCount <> items.Length then
         invalidArg "items" "TotalItemCount is not equal to pager size"
      else
         let currentPage = pager.PageList.[pager.CurrentPage]
         items.[currentPage.Offset .. currentPage.Offset + currentPage.ItemCount - 1]

   let init pageSize totalItemCount =
      if pageSize <= 0 then
         invalidArg "pageSize" "must be positive"
      if totalItemCount < 0 then
         invalidArg "totalItemCount" "must be positive"
      else
         let generator = function
            | _, 0 -> None
            | index, rest ->
               let offset = index * pageSize
               let count = min pageSize rest
               let page = { PageIndex = index; Offset = offset; ItemCount = count }
               let state = index + 1, rest - count
               Some(page, state)
         let pageList = Array.unfold generator (0, totalItemCount)
         { CurrentPage = 0; PageSize = pageSize; PageList = pageList; TotalItemCount = totalItemCount }

   let update message model =
      match message with
      | SetPage(index) -> { model with CurrentPage = index }

   let view model dispatch =
      match model.PageList with
      | [||]
      | [|_|] -> Html.empty
      | pageList ->
         let currentPage = model.CurrentPage
         let lastPage = pageList.Length - 1
         Html.nav [Html.attr.classes ["pagination"; "is-centered"]] [
            Html.a [
               Html.attr.classes ["pagination-previous"]
               if currentPage <= 0
               then Html.attr.disabled "disabled"
               else Html.on.click (fun _ -> SetPage(0) |> dispatch)
            ] [Html.text "first"]
            Html.a [
               Html.attr.classes ["pagination-previous"]
               if currentPage <= 0
               then Html.attr.disabled "disabled"
               else Html.on.click (fun _ -> SetPage(currentPage - 1) |> dispatch)
            ] [Html.text "previous"]
            Html.a [
               Html.attr.classes ["pagination-next"]
               if currentPage >= lastPage
               then Html.attr.disabled "disabled"
               else Html.on.click (fun _ -> SetPage(currentPage + 1) |> dispatch)
            ] [Html.text "next"]
            Html.a [
               Html.attr.classes ["pagination-next"]
               if currentPage >= lastPage
               then Html.attr.disabled "disabled"
               else Html.on.click (fun _ -> SetPage(lastPage) |> dispatch)
            ] [Html.text "last"]
            Html.ul [Html.attr.classes ["pagination-list"]] [
               Html.li [] [
                  Html.span [Html.attr.classes ["pagination-ellipsis"]] [
                     Html.text <| String.Format("{0:#,##0} / {1:#,##0}", currentPage + 1, lastPage + 1)
                  ]
               ]
            ]
         ]

   type View() =
      inherit ElmishComponent<Model, Message>()
      override _.View model dispatch = view model dispatch
