namespace Calvin.Querying.GraphQL.Server

open GraphQL.Server
open GraphQL.Types
open GraphQL.Server.Ui.GraphiQL
open GraphQL.Server.Ui.Playground
open GraphQL.Server.Ui.Altair
open GraphQL.Server.Ui.Voyager
open GraphQL.Server.Transports.AspNetCore.SystemTextJson
open GraphQL.Server.Transports.AspNetCore

open Microsoft.AspNetCore.Http
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.DependencyInjection
open System.Collections.Generic
open Microsoft.Extensions.Logging
open Microsoft.Extensions.Hosting


open Calvin.Querying.GraphQL.Server.Middleware
module Startup =
    type IChat = 
        abstract member Message : string with get
    type Chat = 
        interface IChat with
            member __.Message with get() = "message"
    type ChatSchema(chat : IChat, provider) = 
        inherit Schema(provider)

    type Startup(configuration :IConfiguration, environment : IWebHostEnvironment) =
        // This method gets called by the runtime. Use this method to add services to the container.
        member __.ConfigureServices(services: IServiceCollection) = 
            services
                .AddSingleton<IChat,Chat>()
                .AddSingleton<ChatSchema>()
                .AddGraphQL(fun options provider ->
                    options.EnableMetrics <- environment.IsDevelopment()
                    let logger = provider.GetRequiredService<ILogger<Startup>>()
                    let handler = 
                        System.Action<GraphQL.Execution.UnhandledExceptionContext>(fun ctx -> logger.LogError("{Error} occured", ctx.OriginalException.Message))
                    options.UnhandledExceptionDelegate <- handler
                )
                .AddSystemTextJson((fun deserializerSettings -> ()), (fun serializerSettings -> ()))
                .AddErrorInfoProvider(fun opt -> opt.ExposeExceptionStackTrace <- environment.IsDevelopment())
                .AddDataLoader()
                .AddGraphTypes(typeof<ChatSchema>)
        

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        member __.Configure(app:IApplicationBuilder) =
            if environment.IsDevelopment() then
                app.UseDeveloperExceptionPage() |> ignore

            app.UseGraphQL<ChatSchema, GraphQLHttpMiddlewareWithLogs<ChatSchema>>("/graphql") |> ignore
            
            let playground = GraphQLPlaygroundOptions()
            playground.Path <- PathString("/ui/playground")
            playground.BetaUpdates <- true
            playground.RequestCredentials <- RequestCredentials.Omit
            playground.HideTracingResponse <- false

            playground.EditorCursorShape <- EditorCursorShape.Line
            playground.EditorTheme <- EditorTheme.Light
            playground.EditorFontSize <- 14
            playground.EditorReuseHeaders <- true
            playground.EditorFontFamily <- "Consolas"

            playground.PrettierPrintWidth <- 80
            playground.PrettierTabWidth <- 2
            playground.PrettierUseTabs <- true
          
            playground.SchemaDisableComments <- false
            playground.SchemaPollingEnabled <- true
            playground.SchemaPollingEndpointFilter <- "*localhost*"
            playground.SchemaPollingInterval <- 5000

            playground.Headers <- 
               System.Collections.Generic.Dictionary(
                                                       [
                                                        "MyHeader1",box "MyValue"
                                                        "MyHeader2",box 42
                                                       ] |> Map.ofList
                                                    )

            app.UseGraphQLPlayground(playground) |> ignore
            let options = GraphiQLOptions()
            options.Path <- PathString "/ui/graphiql"
            options.GraphQLEndPoint <- PathString "/graphql"
            app.UseGraphiQLServer(options) |> ignore


            let options = GraphQLAltairOptions()
            options.Path <- PathString "/ui/altair"
            options.GraphQLEndPoint <- PathString "/graphql"
            options.Headers <- 
               System.Collections.Generic.Dictionary(
                   [
                       "X-api-token", "130fh9823bd023hd892d0j238dh"
                   ] |> Map.ofList)
            app.UseGraphQLAltair(options) |> ignore

            let options = GraphQLVoyagerOptions()
            options.Path <- PathString "/ui/voyager"
            options.GraphQLEndPoint <- PathString "/graphql"
            options.Headers <- 
               System.Collections.Generic.Dictionary(
                                                       [
                                                        "MyHeader1",box "MyValue"
                                                        "MyHeader2",box 42
                                                       ] |> Map.ofList
                                                    )
            app.UseGraphQLVoyager(options) |> ignore