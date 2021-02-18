namespace Calvin.Querying.GraphQL.Server

open GraphQL.Server.Common
open GraphQL.Server.Transports.AspNetCore
open GraphQL.Server.Transports.AspNetCore.Common
open GraphQL.Types
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.Logging
open System
open System.Threading
open System.Threading.Tasks

module Middleware = 
    type GraphQLHttpMiddlewareWithLogs<'TSchema when 'TSchema :> ISchema>(
                                                                            logger : ILogger<GraphQLHttpMiddleware<'TSchema>>,
                                                                            next : RequestDelegate,
                                                                            path : PathString,
                                                                            requestDeserializer : IGraphQLRequestDeserializer
                                                                        ) =
        inherit GraphQLHttpMiddleware<'TSchema>(next, path, requestDeserializer)

        override __.RequestExecutedAsync requestExecutionResult = 
            if requestExecutionResult.Result.Errors |>  isNull |> not then
                if requestExecutionResult.IndexInBatch.HasValue then
                    logger.LogError("GraphQL execution completed in {Elapsed} with error(s) in batch [{Index}]: {Errors}", requestExecutionResult.Elapsed, requestExecutionResult.IndexInBatch, requestExecutionResult.Result.Errors)
                else
                    logger.LogError("GraphQL execution completed in {Elapsed} with error(s): {Errors}", requestExecutionResult.Elapsed, requestExecutionResult.Result.Errors)
            else
                logger.LogInformation("GraphQL execution successfully completed in {Elapsed}", requestExecutionResult.Elapsed)

            base.RequestExecutedAsync(&requestExecutionResult)
        

        override __.GetCancellationToken context =
            let cts = CancellationTokenSource.CreateLinkedTokenSource(
                            base.GetCancellationToken(context), 
                            (new CancellationTokenSource(TimeSpan.FromSeconds(5.0))).Token
                     )
            cts.Token