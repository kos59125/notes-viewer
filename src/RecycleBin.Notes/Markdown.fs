[<RequireQualifiedAccess>]
module RecycleBin.Notes.Markdown

open System
open System.IO
open Markdig
open Markdig.Extensions
open Markdig.Extensions.Yaml
open Markdig.Parsers
open Markdig.Renderers
open Markdig.Renderers.Html
open Markdig.Syntax
open Markdig.Syntax.Inlines
open YamlDotNet.Serialization
open YamlDotNet.Serialization.NamingConventions

[<CLIMutable>]
type DocumentHeader = {
   Title : string
   Author : string
   Date : string
}

type Model = Document of html:string * digest:string * header:DocumentHeader option

type private HtmlAttributes with
   member this.AddClasses(classes) =
      Seq.iter this.AddClass classes

type private BulmaMarkdownExtension() =
   
   let addBulmaAttributes (document:MarkdownDocument) =
      for node in document.Descendants() do
         match box node with
         | :? Tables.Table -> node.GetAttributes().AddClasses(["table"; "is-striped"; "is-fullwidth"])
         | :? Figures.Figure -> node.GetAttributes().AddClass("figure")
         | _ -> ()

   interface IMarkdownExtension with

      member _.Setup(pipeline:MarkdownPipelineBuilder) =
         pipeline.add_DocumentProcessed(ProcessDocumentDelegate(addBulmaAttributes))
   
      member _.Setup(_pipeline:MarkdownPipeline, _renderer:IMarkdownRenderer) = 
         ()

type private HighlightJsMarkdownExtension() =
   interface IMarkdownExtension with

      member _.Setup(pipeline:MarkdownPipelineBuilder) =
         match pipeline.BlockParsers.Find<FencedCodeBlockParser>() with
         | null -> ()
         | parser -> parser.InfoPrefix <- ""

      member _.Setup(_pipeline:MarkdownPipeline, _renderer:IMarkdownRenderer) = 
         ()

let private pipeline =
   MarkdownPipelineBuilder()
      .UseYamlFrontMatter()
      .UseAutoIdentifiers()
      .UseAutoLinks()
      .UseFootnotes()
      .UsePipeTables()
      .UseMathematics()
      .Use(BulmaMarkdownExtension())
      .Use(HighlightJsMarkdownExtension())
      .Build()

let private deserializer =
   DeserializerBuilder()
      .WithNamingConvention(CamelCaseNamingConvention.Instance)
      .IgnoreUnmatchedProperties()
      .Build()

let parseMarkdown = function
   | null -> Document("", "", None)
   | markdownText ->
      let document = Markdown.Parse(markdownText, pipeline)

      let header =
         document.Descendants()
         |> Seq.map box
         |> Seq.tryFind (function
            | :? YamlFrontMatterBlock -> true
            | _ -> false
         )
         |> Option.map (fun node -> node :?> YamlFrontMatterBlock)
         |> Option.map (fun yamlBlock ->
            let lines = yamlBlock.Lines.ToString()
            use reader = new StringReader(lines)
            deserializer.Deserialize<DocumentHeader>(reader)
         )
      
      let html =
         use writer = new StringWriter()
         let renderer = HtmlRenderer(writer)
         pipeline.Setup(renderer)
         renderer.Render(document) |> ignore
         writer.ToString()

      let digest =
         document.Descendants()
         |> Seq.map box
         |> Seq.tryFind (function
            | :? ParagraphBlock -> true
            | _ -> false
         )
         |> Option.map (fun node -> node :?> ParagraphBlock)
         |> Option.map (fun paragraph ->
            paragraph.Inline.Descendants()
            |> Seq.map box
            |> Seq.filter (function
               | :? LiteralInline -> true
               | _ -> false
            )
            |> Seq.map string
            |> String.concat ""
         )
         |> function
            | None -> ""
            | Some(text) -> text

      Document(html, digest, header)

let isMarkdownFileName (fileName:string) =
   match Path.GetExtension(fileName).ToLowerInvariant() with
   | ".md"
   | ".markdown" -> true
   | _ -> false
