# Introduction
Calvin is a service of the hobbes platform. It provides a GraphQL interface for specific types of data in the platform

# Getting Started
The best way to get started is to clone the hobbes repository including submodules. 
`git clone --recursive https://github.com/r-d-kmd/hobbes.git`
The Calvin services is a submodule og the hobbes repository.
Having cloned the hobbes reposiroty you can install the required tools `dotnet tool restore` and then run the applications by `dotnet fake run`

# Installation process
To clone the repository, including the submodules run:

Software dependencies
 - docker
 - minikube/kubernetes
 - dotnet cli
 - SAFE stack
 - FSharp.Data

Build and Test
 - paket
 - fake
 - Github actions
 
# Boilerplate

The example code is taken from https://fsprojects.github.io/FSharp.Data.GraphQL/
