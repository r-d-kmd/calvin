namespace FSharp.Data.GraphQL.Samples.StarWarsApi

open System.Text
open Giraffe
open Microsoft.AspNetCore.Http
open Newtonsoft.Json
open FSharp.Data.GraphQL.Execution
open System.IO
open FSharp.Data.GraphQL
open FSharp.Data.GraphQL.Types
open FSharp.Control.Tasks
open Newtonsoft.Json.Linq
open Schema
type HttpHandler = HttpFunc -> HttpContext -> HttpFuncResult

module HttpHandlers =
    let private converters : JsonConverter [] = [| OptionConverter() |]
    let private jsonSettings = jsonSerializerSettings converters

    let internalServerError : HttpHandler = setStatusCode 500

    let okWithStr str : HttpHandler = setStatusCode 200 >=> text str

    let setCorsHeaders : HttpHandler =
        setHttpHeader "Access-Control-Allow-Origin" "*"
        >=> setHttpHeader "Access-Control-Allow-Headers" "content-type"

    let setContentTypeAsJson : HttpHandler =
        setHttpHeader "Content-Type" "application/json"

    let setContentTypeAsHtml : HttpHandler =
        setHttpHeader "Content-Type" "application/html"

    let private graphQL (next : HttpFunc) (ctx : HttpContext) = task {
        let serialize d = JsonConvert.SerializeObject(d, jsonSettings)

        let deserialize (data : string) =
            let getMap (token : JToken) = 
                let rec mapper (name : string) (token : JToken) =
                    match name, token.Type with
                    | "variables", JTokenType.Object -> token.Children<JProperty>() |> Seq.map (fun x -> x.Name, mapper x.Name x.Value) |> Map.ofSeq |> box
                    | name, JTokenType.Array -> token |> Seq.map (fun x -> mapper name x) |> Array.ofSeq |> box
                    | _ -> (token :?> JValue).Value
                token.Children<JProperty>()
                |> Seq.map (fun x -> x.Name, mapper x.Name x.Value)
                |> Map.ofSeq
            if System.String.IsNullOrWhiteSpace(data) 
            then None
            else data |> JToken.Parse |> getMap |> Some
        
        let json =
            function
            | Direct (data, _) ->
                JsonConvert.SerializeObject(data, jsonSettings)
            | Deferred (data, _, deferred) ->
                deferred |> Observable.add(fun d -> printfn "Deferred: %s" (serialize d))
                JsonConvert.SerializeObject(data, jsonSettings)
            | Stream data ->  
                data |> Observable.add(fun d -> printfn "Subscription data: %s" (serialize d))
                "{}"
        
        let removeWhitespacesAndLineBreaks (str : string) = str.Trim().Replace("\r\n", " ")
        
        let readStream (s : Stream) =
            use ms = new MemoryStream(4096)
            s.CopyTo(ms)
            ms.ToArray()
        
        let data = Encoding.UTF8.GetString(readStream ctx.Request.Body) |> deserialize
        
        let query =
            data |> Option.bind (fun data ->
                if data.ContainsKey("query")
                then
                    match data.["query"] with
                    | :? string as x -> Some x
                    | _ -> failwith "Failure deserializing repsonse. Could not read query - it is not stringified in request."
                else None)
        
        let variables =
            data |> Option.bind (fun data ->
                if data.ContainsKey("variables")
                then
                    match data.["variables"] with
                    | null -> None
                    | :? string as x -> deserialize x
                    | :? Map<string, obj> as x -> Some x
                    | _ -> failwith "Failure deserializing response. Could not read variables - it is not a object in the request."
                else None)
        
        match query, variables  with
        | Some query, Some variables ->
            printfn "Received query: %s" query
            printfn "Received variables: %A" variables
            let query = removeWhitespacesAndLineBreaks query
            let root = { RequestId = System.Guid.NewGuid().ToString() }
            let result = Schema.executor.AsyncExecute(query, root, variables) |> Async.RunSynchronously
            printfn "Result metadata: %A" result.Metadata
            return! okWithStr (json result) next ctx
        | Some query, None ->
            printfn "Received query: %s" query
            let query = removeWhitespacesAndLineBreaks query
            let result = Schema.executor.AsyncExecute(query) |> Async.RunSynchronously
            printfn "Result metadata: %A" result.Metadata
            return! okWithStr (json result) next ctx
        | None, _ ->
            let result = Schema.executor.AsyncExecute(Introspection.IntrospectionQuery) |> Async.RunSynchronously
            printfn "Result metadata: %A" result.Metadata
            return! okWithStr (json result) next ctx
    }

    let webApp : HttpHandler =
        setCorsHeaders
        >=> choose [
            GET >=> 
                choose [
                    route "/graphql"
                    >=> htmlString """
                        <!DOCTYPE html>
                        <html>
                        <!--
                            License: MIT
                            Source: https://github.com/graphql/graphql-playground/blob/master/packages/graphql-playground-html/minimal.html
                        -->
                        <head>
                          <meta charset=utf-8/>
                          <meta name="viewport" content="user-scalable=no, initial-scale=1.0, minimum-scale=1.0, maximum-scale=1.0, minimal-ui">
                          <title>GraphQL Playground</title>
                          <link rel="stylesheet" href="//cdn.jsdelivr.net/npm/graphql-playground-react/build/static/css/index.css" />
                          <link rel="shortcut icon" href="//cdn.jsdelivr.net/npm/graphql-playground-react/build/favicon.png" />
                          <script src="//cdn.jsdelivr.net/npm/graphql-playground-react/build/static/js/middleware.js"></script>
                        </head>

                        <body>
                          <div id="root">
                            <style>
                              body {
                                background-color: rgb(23, 42, 58);
                                font-family: Open Sans, sans-serif;
                                height: 90vh;
                              }

                              #root {
                                height: 100%;
                                width: 100%;
                                display: flex;
                                align-items: center;
                                justify-content: center;
                              }

                              .loading {
                                font-size: 32px;
                                font-weight: 200;
                                color: rgba(255, 255, 255, .6);
                                margin-left: 20px;
                              }

                              img {
                                width: 78px;
                                height: 78px;
                              }

                              .title {
                                font-weight: 400;
                              }
                            </style>
                            <img src='//cdn.jsdelivr.net/npm/graphql-playground-react/build/logo.png' alt=''>
                            <div class="loading"> Loading
                              <span class="title">GraphQL Playground</span>
                            </div>
                          </div>
                          <script>window.addEventListener('load', function (event) {
                              GraphQLPlayground.init(document.getElementById('root'), {
                                // options as 'endpoint' belong here
                                shareEnabled: true,
                                settings: {
                                    'request.credentials': 'same-origin',
                                },
                              })
                            })</script>
                        </body>

                        </html>
                    """
                    ]
            POST >=> 
                choose [
                    route "/graphql"
                    >=> graphQL
                    >=> setContentTypeAsJson ]
        ]
