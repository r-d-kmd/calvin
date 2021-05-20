﻿namespace FSharp.Data.GraphQL.Samples.StarWarsApi

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
        assert(match sprintNumber with
                | Some v -> v > -1
                | None -> true)
        typeDataHierarchy() |> Seq.tryFind (fun sp ->  sp.SprintLayerNumber = sprintNumber)

    let getProjectsSprints (ProjectName : string) =
        assert(ProjectName.Length > 0)
        typeDataHierarchy()
        |> Seq.collect (fun sl -> sl.Projects |> fun ps -> Seq.filter(fun p -> p.ProjectName = ProjectName) ps )
        |> Seq.collect(fun pr -> pr.Sprints)
        |> Seq.distinctBy(fun sp -> sp.SprintNumber)
    
    let getProjects sprintNumber =
        assert(match sprintNumber with
                | Some v -> v > -1
                | None -> true)
        sprintNumber |> getSprintLayer
        |> function
            | Some p -> p.Projects
            | None ->  Seq.empty

    let getSprints projectName sprintNumber  = 
        assert(match sprintNumber with
                | Some v -> v > -1
                | None -> true)
        getProjects sprintNumber
        |> Seq.tryFind(fun p -> p.ProjectName = projectName) 
        |> function
           | Some w ->  w.Sprints
           | None   -> Seq.empty
        
    let getWorkItems projectName sprintNumber =
        assert(match sprintNumber with
                | Some v -> v > -1
                | None -> true)
        getSprints projectName sprintNumber
        |> Seq.tryFind(fun s ->  s.SprintNumber = sprintNumber)
        |> function 
            |Some wi ->  wi.WorkItems
            |_ ->   Seq.empty 

    let rec WorkItemType : ObjectDef<WorkItem> = 
        Define.Object<WorkItem>(
            "WorkItem",
            [
                Define.Field("SprintName", String, fun ctx (p:WorkItem) -> p.ProjectName)
                Define.Field("WorkItemID", Int, fun ctx (p:WorkItem) -> p.WorkItemData.WorkItemId)
                Define.Field("TimeStamp", Nullable Date, fun ctx p -> p.WorkItemData.TimeStamp )
                Define.Field("SprintNumber", Nullable Int, fun ctx p -> p.WorkItemData.SprintNumber)

            ]
        )

    let rec JsonProviderType : ObjectDef<Data.Root> =
        Define.Object<Data.Root>(
            "WorkItem",
            [
                Define.Field("SprintName", Nullable String, fun ctx (p:Data.Root) -> p.SprintName)
                Define.Field("WorkItemID", Int, fun ctx (p:Data.Root) -> p.WorkItemId)
                Define.Field("TimeStamp", Nullable Date, fun ctx p -> p.TimeStamp )
                Define.Field("SprintNumber", Nullable Int, fun ctx p -> p.SprintNumber)
                ]
        )

    let rec SprintType: ObjectDef<Sprint> =
        Define.Object<Sprint>(
            "Sprint",[
                Define.Field("SprintNumber", Nullable Int,fun ctx p -> p.SprintNumber)
                Define.Field("ProjectName", String,fun ctx p -> p.ProjectName )
                Define.Field("WorkItems",ListOf WorkItemType, fun ctx p -> getWorkItems p.ProjectName p.SprintNumber )
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
        Define.Object<SprintLayer>( 
            "SprintLayer",[
                Define.Field("SprintNumber", Nullable Int,fun ctx p -> p.SprintLayerNumber)
                Define.Field("Projects",ListOf ProjectType, fun ctx p ->getProjects p.SprintLayerNumber)
            ]
        )
        
    let Query : ObjectDef<Root> =
        Define.Object( "Query",[
            Define.Field("GetSprintLayers", ListOf SprintLayerType, fun ctx _ -> typeDataHierarchy())
            Define.Field("GetProjectSprints", ListOf SprintType, [Define.Input("SprintName",String)], fun ctx _ -> getProjectsSprints (ctx.Arg("SprintName")) )
            Define.Field("GetWorkItemsFromProjectSprint", ListOf WorkItemType, [Define.Input("sprintLayer",Nullable Int);Define.Input("SprintName",String)], fun ctx _ -> getWorkItems (ctx.Arg("ProjectName")) (ctx.Arg("sprintLayer")) )
        ])

    let schema = 
        Schema(query = Query, 
            config = {SchemaConfig.Default 
                        with Types = 
                                [
                                SprintLayerType
                                ;ProjectType
                                ;SprintType
                                ;JsonProviderType
                                ]
                     }
            )
    
    let middlewares =        
        [Define.LiveQueryMiddleware()]

    let executor = Executor(schema, middlewares)



