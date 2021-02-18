namespace Calvin.Querying.GraphQL.Server

open Microsoft.AspNetCore.Hosting
open Serilog
open Serilog.Events
open System
open Microsoft.Extensions.Hosting

open Calvin.Querying.GraphQL.Server.Startup

module Program =
    [<EntryPoint>]
    let main args =
        let createHostBuilder =
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(fun webBuilder ->
                    webBuilder
                        .UseStartup<Startup>() |> ignore
                ).UseSerilog()
        
        Log.Logger <-
            ((new LoggerConfiguration())
                .MinimumLevel.Debug()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
                .Enrich.FromLogContext()
                .WriteTo.Console())
                .CreateLogger() :> ILogger

        try
            try
                Log.Information("Starting host")
                createHostBuilder.Build().Run()
                0
            with ex ->
                Log.Fatal(ex, "Host terminated unexpectedly")
                1
        finally
            Log.CloseAndFlush()