namespace FSharp.Data.GraphQL.Samples.StarWarsApi

open FSharp.Data.GraphQL
open FSharp.Data.GraphQL.Types
open FSharp.Data.GraphQL.Relay
open FSharp.Data.GraphQL.Server.Middleware
open data
#nowarn "40"

module Schema =
    type Root =
        { RequestId: string }

    let getSprintLayer (sprintNumber:int option) =
        typeDataHierarchy() |> Seq.tryFind (fun sp ->  sp.SprintLayerNumber = sprintNumber)

    let getProjectsSprints projectName =
        typeDataHierarchy()
        |> Seq.collect (fun sl -> sl.Projects |> fun ps -> Seq.filter(fun p -> p.ProjectName = projectName ) ps )
        |> Seq.collect(fun x -> x.Sprints)
        |> Seq.distinctBy(fun x -> x.SprintNumber)
    
    let getProjects sprintNumber =
        sprintNumber |> getSprintLayer
        |> function
            | Some p -> p.Projects
            | None ->  printfn(" fisk fisk fisk " )  ;  Seq.empty

    let getSprints projectName sprintNumber  = 
        getProjects sprintNumber
        |> Seq.tryFind(fun p -> p.ProjectName = projectName) 
        |> function
           | Some w ->  w.Sprints
           | None   -> Seq.empty
        
    let getWorkItems projectName sprintNumber =
        getSprints projectName sprintNumber
        |> Seq.tryFind(fun s ->  s.SprintNumber = sprintNumber)
        |> function 
            |Some wi ->  wi.WorkItems
            |_ ->   Seq.empty 

    let rec JsonProviderType : ObjectDef<Data.Root> =
        Define.Object<Data.Root>(
            "WorkItem",
            [
                Define.Field("ProjectName", String, fun ctx (p:Data.Root) -> p.ProjectName)
                Define.Field("TimeStamp", Date, fun ctx p -> p.TimeStamp )
                Define.Field("SprintNumber", Nullable Int, fun ctx p -> p.SprintNumber)
                ]
        )

    let rec SprintType: ObjectDef<Sprint> =
        Define.Object<Sprint>(
            "Sprint",[
                Define.Field("SprintNumber", Nullable Int,fun ctx p -> p.SprintNumber)
                Define.Field("ProjectName", String,fun ctx p -> p.ProjectName )
                Define.Field("WorkItems",ListOf JsonProviderType, fun ctx p -> getWorkItems p.ProjectName p.SprintNumber )
            ]
        )
    let rec  ProjectType: ObjectDef<Project> = 
        Define.Object<Project>(
            "ProjectLayer",[
                Define.Field("ProjectName",String,fun ctx p -> p.ProjectName)
                Define.Field("Sprints", ListOf SprintType,  fun ctx p -> getProjectsSprints p.ProjectName)
            ]
        )

    let rec SprintLayerType : ObjectDef<SprintLayer> =
        Define.Object<SprintLayer>( "SprintLayer",[
                Define.Field("SprintNumber", Nullable Int,fun ctx p -> p.SprintLayerNumber)
                Define.Field("Projects",ListOf ProjectType, fun ctx p ->getProjects p.SprintLayerNumber)
            ]
        )
        
    let Query : ObjectDef<Root> =
        Define.Object( "Query",[
            Define.Field("GetSprintLayers", ListOf SprintLayerType, fun ctx _ -> typeDataHierarchy())
            Define.Field("GetProjectSprints", ListOf SprintType, [Define.Input("ProjectName",String)], fun ctx _ -> getProjectsSprints (ctx.Arg("ProjectName")) )
            Define.Field("GetWorkItemsFromProjectSprint", ListOf JsonProviderType, [Define.Input("sprintLayer",Nullable Int);Define.Input("ProjectName",String)], fun ctx _ -> getWorkItems (ctx.Arg("ProjectName")) (ctx.Arg("sprintLayer")) )
        ])
    let schema  =  Schema(query = Query, config = {SchemaConfig.Default with Types = [SprintLayerType;ProjectType;SprintType;JsonProviderType]})
    let middlewares =        
        [ 
          Define.LiveQueryMiddleware() ]

    let executor = Executor(schema, middlewares)
