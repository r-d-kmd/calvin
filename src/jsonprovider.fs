namespace FSharp.Data.GraphQL.Samples.StarWarsApi

open FSharp.Data
open Microsoft.FSharp.Linq.NullableOperators
module data =

   type Data = FSharp.Data.JsonProvider<"""[{
  "Sprint Name": null,
  "WorkItemId": 442401,
  "ChangedDate": "11/19/2020 07:41:51",
  "WorkItemType": "User Story",
  "CreatedDate": "09/21/2020 18:50:11",
  "ClosedDate": "11/19/2020 07:41:51",
  "LeadTimeDays": "58.5358796",
  "CycleTimeDays": "3.47E-05",
  "StoryPoints": 2,
  "RevisedDate": "01/01/9999 00:00:00",
  "Title": "Web UI for visualiser",
  "State": "Done",
  "Sprint Number" : null
}, {
    "TimeStamp": "03/11/2021 10:34:15",
    "Sprint Name": "null",
    "WorkItemId": 79312,
    "ChangedDate": "04/30/2019 14:57:50",
    "WorkItemType": "User Story",
    "CreatedDate": "02/27/2019 12:27:11",
    "ClosedDate": "04/30/2019 14:57:50",
    "LeadTimeDays": "62.104618",
    "CycleTimeDays": "27.2778819",
    "StoryPoints": null,
    "RevisedDate": "01/01/9999 00:00:00",
    "Priority": 2,
    "Title": "Cake celebration at this point",
    "Sprint Number": 5,
    "State": "Done"
  }]""" >

   // OLD type Structure
   type Sprint = {
      SprintNumber  : int option 
      SprintName : string
      WorkItems : Data.Root seq
   }

   type Project = {
      SprintName : string
      Sprints : Sprint seq
   }

   type SprintLayer = {
      SprintLayerNumber : int option
      Projects : Project seq
   }

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
      ConfigList.Load "http://configurations-svc:8085/meta/category/workitems"     
      |> Array.collect(fun config -> 
         let key = config.Id
         try 
            key 
            |> sprintf"http://uniformdata-svc:8085/dataset/read/%s"
            |> Data.Load
         with e -> 
            eprintfn"Exeption when calling uniformdata, defaulting to sampledata %s %s" e.Message e.StackTrace
            Data.GetSamples()
      )
   
   let datasamples = Data.GetSamples()

   let projectMap() = 
      //datasamples
      configurationlist()
      |> Seq.groupBy(fun p -> p.SprintName)
      |> Seq.map(fun (pn,record) -> 
         pn,record 
            |> Seq.groupBy(fun project -> project.SprintNumber)
         ) 
         |> Map.ofSeq  

   let typeProjectMap() =
      //datasamples
      configurationlist()
      |> Seq.groupBy(fun p -> p.SprintName)
      |> Seq.map(fun (pn,sp) ->
         match pn with
         |None -> 
            {
               SprintName = null
               Sprints = ( sp
                  |> Seq.groupBy(fun spn -> spn.SprintNumber)
                  |> Seq.map(fun (spnmbr,wi) -> {
                     SprintNumber = spnmbr
                     SprintName = null
                     WorkItems = wi
                  })
               )    
            }
         |Some x ->
          {
            SprintName = x
            Sprints = ( sp
               |> Seq.groupBy(fun spn -> spn.SprintNumber)
               |> Seq.map(fun (spnmbr,wi) -> {
                  SprintNumber = spnmbr
                  SprintName = x
                  WorkItems = wi
               })
            )    
         })
   
   let typeDataHierarchy() = 
      let projectMap = projectMap()
      configurationlist()
      //datasamples
      |> Array.groupBy(fun sp -> sp.SprintNumber)

      // Type SprintLayer is assigned a SprintLayerNumber
      // and a Sequence of associated Projects.
      |> Seq.map(fun (sn,records) -> 
         assert(match sn with
                | Some v -> v > -1
                | None -> true)
         {        
         SprintLayerNumber = sn
         Projects = 
            (records
            |> Seq.map(fun record -> record.SprintName)
            |> Set.ofSeq
            // Type Project gets assigned with a ProjectName and a 
            // Sequence of associated Sprints.
            |> Seq.map(fun pn -> 
               //assert(pn.Length > 0)
               match pn with
               |None ->
                  {
                     SprintName = null
                     Sprints = 
                        ( 
                        match projectMap |> Map.tryFind pn with
                        | Some sp -> 
                           sp |>  Seq.map(fun (sp,wi) ->
                              {SprintNumber = sp
                               SprintName = null
                               WorkItems = wi})
                        | None -> Seq.empty     
                        )
                  }
               |Some x ->
                  {
                     SprintName = x
                     Sprints = 
                        ( 
                        match projectMap |> Map.tryFind pn with
                        | Some sp -> 
                           sp |>  Seq.map(fun (sp,wi) ->
                              {SprintNumber = sp
                               SprintName = x
                               WorkItems = wi})
                        | None -> Seq.empty     
                        )
                  }
                )) 
            })