[<RequireQualifiedAccess>]
module RecycleBin.Notes.Markdown

open System.IO
open Markdig
open Markdig.Extensions
open Markdig.Extensions.Yaml
open Markdig.Parsers
open Markdig.Renderers
open Markdig.Renderers.Html
open Markdig.Renderers.Html.Inlines
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

type ExternalLinkExtension() =
   let linkWriter =
      MarkdownObjectRenderer<HtmlRenderer, LinkInline>.TryWriteDelegate(
         fun _ link ->
            let attributes = link.GetAttributes()
            if link.Url.StartsWith("http") then
               attributes.AddPropertyIfNotExist("target", "_blank")
               attributes.AddPropertyIfNotExist("rel", "noopener")
            false
      )
   interface IMarkdownExtension with
      member _.Setup(_pipeline:MarkdownPipelineBuilder) =
         ()
      member _.Setup(_pipeline:MarkdownPipeline, renderer:IMarkdownRenderer) =
         match renderer.ObjectRenderers.FindExact<LinkInlineRenderer>() with
         | null -> ()
         | linkRenderer ->
            linkRenderer.TryWriters.Remove(linkWriter) |> ignore
            linkRenderer.TryWriters.Add(linkWriter) |> ignore

let private pipeline =
   MarkdownPipelineBuilder()
      .UseYamlFrontMatter()
      .UseAutoIdentifiers()
      .UseAutoLinks()
      .UseFootnotes()
      .UsePipeTables()
      .UseMathematics()
      .Use(ExternalLinkExtension())
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
            |> Seq.fold (fun acc -> function
               | :? LiteralInline as x -> acc + string x
               | :? CodeInline as x -> acc + x.Content
               | _ -> acc
            ) ""
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
