namespace FSharp.Data.GraphQL.Samples.StarWarsApi

open FSharp.Data


module data =

   type Data = FSharp.Data.JsonProvider<"""../example.json""">
   (*
   let data = 
      Http.RequestString("",
                          httpMethod = "get",
                          headers = [HttpRequestHeaders.BasicAuth "bruger" "pwd"]
      ) |> Data.Load
   *)

   let variable =         
      match Http.get ( Http.ConfigurationList |> Http.Configurations) RawdataTypes.ConfigList.Parse with
      Http.Success configs -> 
         configs |> Array.map(fun config -> 
            let key = config.JsonValue.ToString() |> RawdataTypes.Config.Parse |> RawdataTypes.keyFromConfig
            match Http.get (key + ":Json" |> Http.UniformDataService.ReadFormatted |> Http.UniformData) Data.Parse with
            Http.Success json ->
               json
         )


   let datasample = Data.GetSamples()
   let projectMap = 
      datasample
      |> Seq.groupBy(fun p -> p.ProjectName)
      |> Seq.map(fun (pn,record) -> 
         pn,record 
            |> Seq.groupBy(fun project -> project.SprintNumber)
      ) |> Map.ofSeq  
      


   let sampleDataHeirachy = 
      datasample 
      |> Array.groupBy(fun sp -> sp.SprintNumber)
      |> Seq.map(fun (sn,records) ->
         sn,records
            |> Seq.map(fun record -> record.ProjectName)
            |> Set.ofSeq
            |> Seq.map(fun pn -> 
               match projectMap |> Map.tryFind pn with
               |Some wi -> pn,wi
               |None -> pn,Seq.empty
            )
      )


   let test = sampleDataHeirachy 
            |> Seq.find(fun x -> fst x = Some 2)
            |> snd 
            |> Seq.find(fun x -> fst x = "lkjlkj")
            |> snd
            |> Seq.find(fun x -> fst x = Some 3)
            |> snd





   




   //let test2 = Array.iter(fun x -> printfn"%A" x) sampleDataHeirachy


               
(*
   let sprintProjectHeirachy = 
       data
       |> Array.groupBy(fun d -> d.SprintNumber)
       |> Seq.map(fun (_,records) ->
          records
          |> Seq.groupBy(fun r -> r.ProjectName)
       )*)