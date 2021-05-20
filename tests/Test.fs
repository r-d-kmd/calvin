open System
open System.Threading

open Xunit
open FSharp.Data
open FSharp.Data.GraphQL.Execution
open FSharp.Data.GraphQL.Samples.StarWarsApi
open FSharp.Data.GraphQL.Samples.StarWarsApi.Schema
open FSharp.Data
open FSharp.Data.GraphQL




[<Fact>]
let ``GET /graphql should return a HTML file`` () = async {
    let runServer =
        async {
            let returnCode = Program.main [||]
            Assert.Equal (returnCode, 0)
        }
    let cancellationSource = new CancellationTokenSource()
    Async.Start (runServer, cancellationSource.Token)

    Thread.Sleep(1000)
    let resp = Http.Request ("http://localhost:8085/graphql")
    Assert.Equal (resp.StatusCode, 200)
    printfn "Headers %A" resp.Headers
    Assert.Equal ("text/html; charset=utf-8", resp.Headers.["Content-Type"])
    cancellationSource.Cancel()
    
}

[<Fact>] 
let GetSprintLayersReturnsSampleDataExpectedSprintlayers () =  async {
    let query = """
    {
        GetSprintLayers{
            SprintNumber
        }
    }
        """

    let expected = """[data, { GetSprintLayers: [{ SprintNumber: 5 }, { SprintNumber: null }, ] }]]"""


    let! response = Schema.executor.AsyncExecute(query)
    let actual = response.Content.ToString()
    Assert.Contains(expected, actual)
}

[<Fact>]
let GetSprintLayersReturnsSampleDataExpectedProjects () = async {
    let query = """
    {
        GetSprintLayers{
          SprintNumber
          Projects {
            ProjectName
          }
       }
    }
    """
    let expected = """      [data, { GetSprintLayers: [{ SprintNumber: 5,
                                Projects: [{ ProjectName: "failure" }, ] }, { SprintNumber: null,
                                Projects: [{ ProjectName: "failure" }, ] }, ] }]],"""

    let! response = Schema.executor.AsyncExecute(query)
    let actual = response.Content.ToString()
    Assert.Contains(expected, actual)
}


(*
[<Fact>]
let ``GET /graphql should return a JSON file`` () = async {

    Thread.Sleep(2000)
    let resp = Http.Request ("http://localhost:8085/graphql", httpMethod = "POST",
        body = TextRequest """ {"operationName":null,"variables":{},"query":"{ __schema { __typename } }" """)
    Assert.Equal (resp.StatusCode, 200)
    printfn "Headers %A" resp.Headers
    Assert.Equal ("text/json; charset=utf-8", resp.Headers.["Content-Type"])

    cancellationSource.Cancel()
}*)

