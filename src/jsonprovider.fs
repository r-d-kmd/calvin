namespace sletmig

open FSharp.Data


module data =

   type Data = FSharp.Data.JsonProvider<"""../example.json""">

   let data = 
      Http.RequestString("",
                          httpMethod = "get",
                          headers = [HttpRequestHeaders.BasicAuth "bruger" "pwd"]
      ) |> Data.Load

   let sprintProjectHeirachy = 
       data
       |> Array.groupBy(fun d -> d.SprintNumber)
       |> Seq.map(fun (_,records) ->
          records
          |> Seq.groupBy(fun r -> r.ProjectName)
       )