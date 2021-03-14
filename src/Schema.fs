namespace FSharp.Data.GraphQL.Samples.StarWarsApi

open FSharp.Data.GraphQL
open FSharp.Data.GraphQL.Types
open FSharp.Data.GraphQL.Server.Middleware

#nowarn "40"

type Sprint =
    { TimeStamp : string option
      SprintName : string option
      WorkItemId : int
      ChangedDate : string option
      WorkItemType : string option
      CreatedDate : string option
      ClosedDate : string option
      LeadTimeDays : string option
      CycleTimeDays : string option
      StoryPoints : int option
      RevisedDate : string option
      Priority : int option
      Title : string option
      SprintNumber : int option
      State : string option}

type Root =
    { RequestId: string }

module Schema =
    let sprints =
        [ { TimeStamp = Some "03/11/2021 10:34:15"
            SprintName = Some null
            WorkItemId = 81347
            ChangedDate = Some "04/10/2019 11:14:04"
            WorkItemType = Some "User Story"
            CreatedDate = Some "03/12/2019 11:50:47"
            ClosedDate = Some "04/10/2019 11:14:04"
            LeadTimeDays = Some "28.9745023"
            CycleTimeDays = Some "7.1225925"
            StoryPoints = Some 5
            RevisedDate = Some "01/01/9999 00:00:00"
            Priority = Some 2
            Title = Some "user can select transformation"
            SprintNumber = Some 0
            State = Some "Done"}]
        

    let getSprint id =
        sprints |> List.tryFind (fun s -> s.WorkItemId = id)

    let SprintType =
        Define.Object<Sprint>(
            name = "Sprint",
            description = "A sprint",
            fieldsFn = fun () ->
                [
                    Define.Field("TimeStamp", Nullable String, "The Time Stamp", fun _ (s : Sprint) -> s.TimeStamp)
                    Define.Field("SprintName", Nullable String, "The Time Stamp", fun _ (s : Sprint) -> s.SprintName)
                    Define.Field("WorkItemId", Int, "The Time Stamp", fun _ (s : Sprint) -> s.WorkItemId)
                    Define.Field("ChangedDate", Nullable String, "The Time Stamp", fun _ (s : Sprint) -> s.ChangedDate)
                    Define.Field("WorkItemType", Nullable String, "The Time Stamp", fun _ (s : Sprint) -> s.WorkItemType)
                    Define.Field("CreatedDate", Nullable String, "The Time Stamp", fun _ (s : Sprint) -> s.CreatedDate)
                    Define.Field("ClosedDate", Nullable String, "The Time Stamp", fun _ (s : Sprint) -> s.ClosedDate)
                    Define.Field("LeadTimeDays", Nullable String, "The Time Stamp", fun _ (s : Sprint) -> s.LeadTimeDays)
                    Define.Field("CycleTimeDays", Nullable String, "The Time Stamp", fun _ (s : Sprint) -> s.CycleTimeDays)
                    Define.Field("StoryPoints", Nullable Int, "The Time Stamp", fun _ (s : Sprint) -> s.StoryPoints)
                    Define.Field("RevisedDate", Nullable String, "The Time Stamp", fun _ (s : Sprint) -> s.RevisedDate)
                    Define.Field("Priority", Nullable Int, "The Time Stamp", fun _ (s : Sprint) -> s.Priority)
                    Define.Field("Title", Nullable String, "The Time Stamp", fun _ (s : Sprint) -> s.Title)
                    Define.Field("SprintNumber", Nullable Int, "The Time Stamp", fun _ (s : Sprint) -> s.SprintNumber)
                    Define.Field("State", Nullable String, "The Time Stamp", fun _ (s : Sprint) -> s.State)
                ])

    let RootType =
        Define.Object<Root>(
            name = "Root",
            description = "The Root type to be passed to all our resolvers.",
            isTypeOf = (fun o -> o :? Root),
            fieldsFn = fun () ->
            [
                Define.Field("requestId", String, "The ID of the client.", fun _ (r : Root) -> r.RequestId)
            ])

    let Query =
        Define.Object<Root>(
            name = "Query",
            fields = [
                Define.Field("sprint", Nullable SprintType, "Gets Sprint", [ Define.Input("id", Int) ], fun ctx _ -> getSprint (ctx.Arg("id") ) ) ] )

    let schemaConfig = SchemaConfig.Default

    let schema : ISchema<Root> = upcast Schema(Query)

    let middlewares = 
        [ Define.QueryWeightMiddleware(2.0, true)
          Define.LiveQueryMiddleware() ]

    let executor = Executor(schema, middlewares)