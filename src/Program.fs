namespace FSharp.Data.GraphQL.Samples.StarWarsApi

open Microsoft.AspNetCore
open Microsoft.AspNetCore.Hosting
open data
open Schema
module Program =
    let exitCode = 0

    let [<Literal>] BaseAddress = "0.0.0.0:8085"

    let buildWebHost args =
        WebHost
            .CreateDefaultBuilder(args)
            .UseStartup<Startup>()
            .UseUrls(sprintf "http://%s" BaseAddress)
   
    [<EntryPoint>]
    let main args =
        buildWebHost(args).Build().Run()
        exitCode
