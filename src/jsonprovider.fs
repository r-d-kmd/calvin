namespace FSharp.Data.GraphQL.Samples.StarWarsApi

open FSharp.Data
open Microsoft.FSharp.Linq.NullableOperators
open System
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

   type Test = Data.Root

   type WorkItem = {
      ProjectName : string
      SprintNumber : int option
      WorkItemId : int
      TimeStamp :  DateTime option
      SprintName : string option
      Priority : int option
      State : string
      ChangedDate:  DateTime
      WorkItemType:  string
      CreatedDate:  DateTime
      ClosedDate:  DateTime
      LeadTimeDays:  decimal
      CycleTimeDays:  float
      StoryPoints:  Option<int>
      RevisedDate:  DateTime
      Title:  string
   }
   type Sprint = {
      SprintNumber  : int option 
      ProjectName : string
      WorkItems : WorkItem seq
   }

   type Project = {
      ProjectName : string
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
      try
      ConfigList.Load "http://configurations-svc:8085/meta/category/workitems"     
      |> Array.collect(fun config -> 
         let key = config.Id
         try 
            key 
            |> sprintf"http://uniformdata-svc:8085/dataset/read/%s"
            |> Data.Load
            |> Array.fold(fun state item ->
               let workItem = {
                  ProjectName = key
                  SprintNumber = item.SprintNumber
                  WorkItemId = item.WorkItemId
                  TimeStamp = item.TimeStamp
                  SprintName = item.SprintName
                  Priority = item.Priority
                  State = item.State
                  ChangedDate = item.ChangedDate
                  WorkItemType = item.WorkItemType
                  CreatedDate = item.CreatedDate
                  ClosedDate = item.ClosedDate
                  LeadTimeDays = item.LeadTimeDays
                  CycleTimeDays = item.CycleTimeDays
                  StoryPoints = item.StoryPoints
                  RevisedDate = item.RevisedDate
                  Title = item.Title
               } 
               workItem :: state) List.empty
            |> Array.ofList
         with e -> 
            eprintfn"Exeption when calling uniformdata, defaulting to sampledata %s %s" e.Message e.StackTrace
            Data.GetSamples()
            |> Array.fold(fun state item ->
               let workItem = {
                  ProjectName = key
                  SprintNumber = item.SprintNumber
                  WorkItemId = item.WorkItemId
                  TimeStamp = item.TimeStamp
                  SprintName = item.SprintName
                  Priority = item.Priority
                  State = item.State
                  ChangedDate = item.ChangedDate
                  WorkItemType = item.WorkItemType
                  CreatedDate = item.CreatedDate
                  ClosedDate = item.ClosedDate
                  LeadTimeDays = item.LeadTimeDays
                  CycleTimeDays = item.CycleTimeDays
                  StoryPoints = item.StoryPoints
                  RevisedDate = item.RevisedDate
                  Title = item.Title
               } 
               workItem :: state) List.empty
            |> Array.ofList
      )
      with _ -> 
         //eprintfn"Exeption when calling uniformdata, defaulting to sampledata %s %s" e.Message e.StackTrace
         Data.GetSamples()
         |> Array.fold(fun state item ->
               let workItem = {
                  ProjectName = "failure"
                  SprintNumber = item.SprintNumber
                  WorkItemId = item.WorkItemId
                  TimeStamp = item.TimeStamp
                  SprintName = item.SprintName
                  Priority = item.Priority
                  State = item.State
                  ChangedDate = item.ChangedDate
                  WorkItemType = item.WorkItemType
                  CreatedDate = item.CreatedDate
                  ClosedDate = item.ClosedDate
                  LeadTimeDays = item.LeadTimeDays
                  CycleTimeDays = item.CycleTimeDays
                  StoryPoints = item.StoryPoints
                  RevisedDate = item.RevisedDate
                  Title = item.Title
               } 
               workItem :: state) List.empty
         |> Array.ofList

   
   

   let projectMap() = 
      //datasamples
      configurationlist()
      |> Seq.groupBy(fun p -> p.ProjectName)
      |> Seq.map(fun (pn,record) -> 
         pn,record 
            |> Seq.groupBy(fun project -> project.SprintNumber)
         ) 
         |> Map.ofSeq  
(*
   let typeProjectMap() =
      //datasamples
      configurationlist()
      |> Seq.groupBy(fun p -> p.ProjectName)
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
   *)
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
            |> Seq.map(fun record -> record.ProjectName)
            |> Set.ofSeq
            // Type Project gets assigned with a ProjectName and a 
            // Sequence of associated Sprints.
            |> Seq.map(fun pn ->
            //assert(pn.Length > 0)
            assert(pn.Length > 0)
            {
               ProjectName = pn
               Sprints = 
                  ( 
                  match projectMap |> Map.tryFind pn with
                  | Some sp -> 
                     sp |>  Seq.map(fun (sp,wi) ->
                        {SprintNumber = sp
                         ProjectName = pn
                         WorkItems = wi})
                  | None -> Seq.empty     
                  )
            })) 
            })