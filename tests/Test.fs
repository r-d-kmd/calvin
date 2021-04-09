open System
open System.Threading

open Xunit
open FSharp.Data

open FSharp.Data.GraphQL.Samples.StarWarsApi
open FSharp.Data

[<Fact>]
let ``GET /graphql should return a HTML file`` () = async {
    let runServer =
        async {
            let returnCode = Program.main [||]
            Assert.Equal (returnCode, 0)
        }

    use cancellationSource = new CancellationTokenSource()
    Async.Start (runServer, cancellationSource.Token)
    Thread.Sleep(1000)

    let resp = Http.Request ("http://127.0.0.1:8085/graphql")
    Assert.Equal (resp.StatusCode, 200)
    printfn "Headers %A" resp.Headers
    Assert.Equal ("text/html; charset=utf-8", resp.Headers.["Content-Type"])

    cancellationSource.Cancel()
}

(*
[<Fact>]
let ``GET /graphql should return a JSON file`` () = async {
    let runServer =
        async {
            let returnCode = Program.main [||]
            Assert.Equal (returnCode, 0)
        }

    Thread.Sleep(1000)
    use cancellationSource = new CancellationTokenSource()
    Async.Start (runServer, cancellationSource.Token)
    Thread.Sleep(1000)

    let resp = Http.Request ("http://127.0.0.1:8085/graphql", httpMethod = "POST",
        body = TextRequest """ {"operationName":null,"variables":{},"query":"{ __schema { __typename } }" """)
    Assert.Equal (resp.StatusCode, 200)
    printfn "Headers %A" resp.Headers
    Assert.Equal ("text/json; charset=utf-8", resp.Headers.["Content-Type"])

    cancellationSource.Cancel()
}
*)
