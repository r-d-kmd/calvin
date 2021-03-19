namespace FSharp.Data.GraphQL.Samples.StarWarsApi

open FSharp.Data.GraphQL
open FSharp.Data.GraphQL.Types
open FSharp.Data.GraphQL.Server.Middleware

#nowarn "40"

type Sprint = { 
    Number : int
    Projects : Project list   
} 

and Project = {
    Sprints : SprintInstance list
    Name : string
}

and SprintInstance = {
    SprintName : string option
    CreatedDate : string option
    ClosedDate : string option
    WorkItem : WorkItem list
}

and WorkItem = {
      LeadTimeDays : string option
      CycleTimeDays : string option
      StoryPoints : int option
      RevisedDate : string option
      Priority : int option
      Title : string option
      SprintNumber : int option
      State : string option
      WorkItemType : string option
      WorkItemId : int
}

type Root =
    { RequestId: string }

module Schema =
    
    let sprints = 
    [{  Number = Some 0
        Projects = Some Option.Nullable }]

    let sprintInstances = 
    [{  SprintName = Some Option.Nullable
        CreatedDate = Some "03/12/2019 11:50:47"
        ClosedDate = Some "04/10/2019 11:14:04"
        WorkItem : WorkItem list }]

    let projects = 
    [{  Sprints : SprintInstance list
        Name : string }]

    let workItems = 
    [{  LeadTimeDays = Some "28.9745023"
        CycleTimeDays = Some "7.1225925"
        StoryPoints = Some 5
        RevisedDate = Some "01/01/9999 00:00:00"
        Priority = Some 2
        Title = Some "user can select transformation"
        SprintNumber = Some 0
        State = Some "Done"
        WorkItemType = Some "User Story"
        WorkItemId = 81347 }]
        
    let getSprint id =
        sprints |> List.tryFind (fun s -> s.WorkItemId = id)

    let getSprintInstance name =
        sprintInstances |> List.tryFind (fun s-> .SprintName = name)
    
    let getProject name =
        projects |> List.tryFind (fun p -> p.Name = name)

    let getWorkItem title =
        workItems |> List.tryFind (fun w -> w.title = title)

    let SprintInstanceType =
        Define.Object<SprintInstance>(
            name = "Sprint Instance",
            description = "A Sprint Instance",
            fieldsFn = fun () -> 
                [
                    Define.Field("Sprint Name", Nullable String, "The name of the sprint.", fun _ (s : SprintInstance) -> s.SprintName)
                    Define.Field("Created Date", Nullable String, "The date of the sprint creation", fun _ (s : SprintInstance) -> s.CreatedDate)
                    Define.Field("Closed Date", Nullable String, "The day the sprint closed.", fun _ (s : SprintInstance) -> s.ClosedDate)
                    Define.Field("Work Items", ListOf WorkItem, "A list of work items in the sprint.", fun _ (s : SprintInstance) -> s.WorkItem)
                ]
        )

    let SprintType =
        Define.Object<Sprint>(
            name = "Sprint",
            description = "A sprint",
            fieldsFn = fun () ->
                [
                    Define.Field("Sprint Number", Nullable Int, "The number of the sprint", fun _ (s : Sprint) -> s.Number)
                    Define.Field("Projects", ListOf Project, "Projects related to the sprint", fun _ (s : Sprint) -> s.Projects)
                ]) 

    let ProjectType =
        Define.Object<Project>,
            name = "Project",
            description = "A collection of sprints",
            fieldsFn = fun () ->
                [
                    Define.Field("Sprints", ListOf SprintInstance, "The sprints in the project", fun _ (p : Project) -> p.Sprints)
                    Define.Field("Name", String, "The name of the project", fun _ (p : Project) -> p.Name)
                ]
    
    let WorkItemType =
        Define.Object<WorkItem>,
            name = "WorkItem",
            description = "A task that needs to be done in the sprint",
            fieldsFn = fun () ->
                [
                    Define.Field("LeadTimeDays", Nullable String, "The amount of days worked on the workitem", fun _ (w : WorkItem) -> w.LeadTimeDays)
                    Define.Field("CycleTimeDays", Nullable String, "The amount of days worked on the workitem", fun _ (w : WorkItem) -> w.CycleTimeDays)
                    Define.Field("StoryPoints", Nullable Int, "An estimate of the amount of time to solve the workitem", fun _ (w : WorkItem) -> w.StoryPoints)
                    Define.Field("RevisedDate", Nullable String, "The date where the workitem date was changed", fun _ (w : WorkItem) -> w.RevisedDate)
                    Define.Field("Priority", Nullable Int, "The priority of the workitem", fun _ (w : WorkItem) -> w.Priority)
                    Define.Field("Title", Nullable String, "The title of the workitem", fun _ (w : WorkItem) -> w.Title)
                    Define.Field("SprintNumber", Nullable Int, "The  number of the sprint in a project", fun _ (w : WorkItem) -> w.Priority)
                    Define.Field("State", Nullable String, "The state of the workitem", fun _ (w : WorkItem) -> w.State)
                    Define.Field("WorkItemType", Nullable String, "The type of the workitem", fun _ (w : WorkItem) -> w.WorkItemType)
                    Define.Field("WorkItemId", Int, "The identifier of the workitem", fun _ (w : WorkItem) -> w.ID)
                ]

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
                Define.Field("sprint", Nullable SprintType, "Gets Sprint", [ Define.Input("id", Int) ], fun ctx _ -> getSprint (ctx.Arg("id") ) ) 
                Define.Field("sprint", Nullable SprintType, "Gets Sprint", [ Define.Input("id", Int) ], fun ctx _ -> getSprint (ctx.Arg("id") ) )
            ] 
        )

    let schemaConfig = SchemaConfig.Default

    let schema : ISchema<Root> = upcast Schema(Query)

    let middlewares = 
        [ Define.QueryWeightMiddleware(2.0, true)
          Define.LiveQueryMiddleware() ]

    let executor = Executor(schema, middlewares)