#r "paket:
nuget Fake ~> 5 //
nuget Fake.Core ~> 5 //
nuget Fake.Core.Target  //
nuget Fake.DotNet //
nuget Fake.DotNet.AssemblyInfoFile //
nuget Fake.DotNet.Cli //
nuget Fake.DotNet.NuGet //
nuget Fake.IO.FileSystem //
nuget Fake.Tools.Git ~> 5 //"
#load "./.fake/build.fsx/intellisense.fsx"


#if !FAKE
#r "netstandard"
#r "Facades/netstandard" // https://github.com/ionide/ionide-vscode-fsharp/issues/839#issuecomment-396296095
#endif

open Fake.Core
open Fake.DotNet
open Fake.IO

[<RequireQualifiedAccess>]
type Targets = 
   Build 
   | Package
   | Push
   | Test
   | Release
   | InstallDependencies
   | Generic of string

let targetName = 
    function
        Targets.Build -> "build"
        | Targets.InstallDependencies -> "installdependencies"
        | Targets.Package -> "package"
        | Targets.Push -> "push"
        | Targets.Test -> "test"
        | Targets.Release -> "release"
        | Targets.Generic s -> s

open Fake.Core.TargetOperators
let inline (==>) (lhs : Targets) (rhs : Targets) =
    Targets.Generic((targetName lhs) ==> (targetName rhs))

let inline (?=>) (lhs : Targets) (rhs : Targets) =
    Targets.Generic((targetName lhs) ?=> (targetName rhs))

let inline (<===) (lhs : Targets) (rhs : Targets) =
    rhs ==> lhs //deliberately changing order of arguments

let create target = 
    target
    |> targetName
    |> Target.create

let runOrDefaultWithArguments =
    targetName
    >> Target.runOrDefaultWithArguments 

let run command workingDir args = 
    let arguments = 
        match args |> String.split ' ' with
        [""] -> Arguments.Empty
        | args -> args |> Arguments.OfArgs
    RawCommand (command, arguments)
    |> CreateProcess.fromCommand
    |> CreateProcess.withWorkingDirectory workingDir
    |> CreateProcess.ensureExitCode
    |> Proc.run
    |> ignore

let nugetFeedUrl = Environment.environVarOrNone "FEED_URL"

let buildConfiguration = 
        DotNet.BuildConfiguration.Release
  
open System.IO
let verbosity = Quiet
    
let package conf outputDir projectFile =
    DotNet.publish (fun opts -> 
                        { opts with 
                               OutputPath = Some outputDir
                               Configuration = conf
                               MSBuildParams = 
                                   { opts.MSBuildParams with
                                          Verbosity = Some verbosity
                                   }    
                        }
                   ) projectFile
let srcPath = "src/"
let testsPath = "tests/"

let getProjectFile folder = 
    if Directory.Exists folder then
        Directory.EnumerateFiles(folder,"*.?sproj")
        |> Seq.tryExactlyOne
    else
        eprintfn "Folder (%s) doesn't exist" folder
        None

let paket workDir args = 
    run "dotnet" workDir ("paket " + args) 



create Targets.Release ignore
create Targets.InstallDependencies (fun _ ->
    paket srcPath "install"
)

create Targets.Build (fun _ ->    
    let projectFile = 
        srcPath
        |> getProjectFile
    printfn "Building %A" projectFile
    package buildConfiguration "./package" projectFile.Value
)
    
create Targets.Package (fun _ ->
    let packages = Directory.EnumerateFiles(srcPath, "*.nupkg")
    let version = "0.1.local"
    let packageVersion = 
        match Environment.environVarOrNone "BUILD_VERSION" with
        None -> version
        | Some bv ->
            sprintf "1.1.%s" bv
    File.deleteAll packages
    sprintf "pack --version %s ." packageVersion
    |> paket srcPath 
)

create Targets.Push (fun _ ->
    let nupkgFilePath = 
        Directory.EnumerateFiles(srcPath, "*.nupkg")
        |> Seq.exactlyOne

    let key = 
        match Environment.environVarOrNone "KEY" with
        None -> failwith "No nuget feed key found (set env var KEY)"
        | Some k -> k
        
    (match nugetFeedUrl with
     Some nugetFeedUrl ->
        sprintf "push --url %s --api-key %s %s" nugetFeedUrl
     | None -> 
        sprintf "push --api-key %s %s") key nupkgFilePath
    |> paket "./"
)

create Targets.Test (fun _ ->
    match testsPath |> getProjectFile with
    Some tests -> 
        tests |> DotNet.test id
    | None -> printfn "Skipping tests because no tests was found. Create a project in the folder 'tests/' wo have tests run"
)

Targets.Build
    ==> Targets.Package

Targets.Build
    ?=> Targets.Test
    ?=> Targets.Package
    ?=> Targets.Push

Targets.InstallDependencies
    ?=> Targets.Build

Targets.Release
    <=== Targets.Push
    <=== Targets.Package
    <=== Targets.Test
    <=== Targets.InstallDependencies
    
Targets.Package
|> runOrDefaultWithArguments 