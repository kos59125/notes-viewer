module RecycleBin.Notes.Program

open System.IO
open System.Reflection
open Microsoft.AspNetCore.Components.WebAssembly.Hosting
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.DependencyInjection

let environment =
#if DEBUG
   "Development"
#else
   "Production"
#endif

type IConfigurationBuilder with
   member config.AddJsonStreamOrIgnore (stream:Stream) =
      match isNull stream with
      | true -> config
      | false -> config.AddJsonStream(stream)

let buildConfiguration () =
   let assembly = Assembly.GetExecutingAssembly()
   let assemblyName = assembly.GetName().Name
   use baseSettingsStream = assembly.GetManifestResourceStream(sprintf "%s.appsettings.json" assemblyName)
   use envSettingsStream = assembly.GetManifestResourceStream(sprintf "%s.appsettings.%s.json" assemblyName environment)

   ConfigurationBuilder()
      .AddJsonStreamOrIgnore(baseSettingsStream)
      .AddJsonStreamOrIgnore(envSettingsStream)
      .Build()

let configureServices (services:IServiceCollection) =
   let config = buildConfiguration ()
   services
      .AddOptions()
      .Configure<NoteOptions>(fun options ->
         config.GetSection("Note").Bind(options)
      )
      //.AddBaseAddressHttpClient()
      .AddHttpClient()
      .AddSingleton<GitHub.GitHubRestClient>()
   |> ignore

[<EntryPoint>]
[<CompiledName("Main")>]
let main args =
   let builder = WebAssemblyHostBuilder.CreateDefault(args)
   builder.RootComponents.Add<App.App>("#app")
   configureServices builder.Services
   builder.Build().RunAsync() |> ignore
   0
