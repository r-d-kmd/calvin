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
   type ConfigList = JsonProvider<"""[{
            "_id" : "name",
            "source" : {
                "provider" : "azuredevops",
                "id" : "lkjlkj", 
                "project" : "gandalf",
                "dataset" : "commits",
                "server" : "https://analytics.dev.azure.com/kmddk/flowerpot"
            },
            "transformations" : ["jlk","lkjlk"]
        }, {
            "_id" : "name",
            "source" : {
                "id" : "lkjlkj",
                "provider": "merge",
                "datasets" : ["cache key for a data set","lkjlkjlk"]
            },
            "transformations" : ["jlk","lkjlk"]
        }, {
            "_id" : "name",
            "source" : 
                {
                    "provider" : "join",
                    "id" : "kjlkj",
                    "left": "cache key for a data set",
                    "right" : "cache key for a data set",
                    "field" : "name of field to join on "
                },
            "transformations" : ["jlk","lkjlk"]
        }]""">

   let configurationlist() = 
      ConfigList.Load "http://configurations-svc:8085/configurationlist"     
      |> Array.collect(fun config -> 
         let key = config.Id
         try 
            key 
            |> sprintf"http://uniformdata-svc:8085/dataset/read/%s"
            |> Data.Load
         with e -> 
            eprintfn"Exeption when calling data, defaulting to sampledata %s %s" e.Message e.StackTrace
            Data.GetSamples()
      )
   




   let projectMap() = 
      configurationlist()
      |> Seq.groupBy(fun p -> p.ProjectName)
      |> Seq.map(fun (pn,record) -> 
         pn,record 
            |> Seq.groupBy(fun project -> project.SprintNumber)
         ) |> Map.ofSeq  
      


   let sampleDataHierarchy() = 

      let projectMap = projectMap()
      configurationlist()
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


   let test() = 
            sampleDataHierarchy() 
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
