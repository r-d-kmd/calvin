ARG DOTNET_VERSION=5.0
FROM kmdrd/sdk:${DOTNET_VERSION} AS build-base

ARG BUILD_CONFIGURATION_ARG="Release"
ENV BUILD_CONFIGURATION ${BUILD_CONFIGURATION_ARG}

ARG PORT_ARG 8085

RUN dotnet tool restore

FROM build-base as build

COPY ./src /source
WORKDIR /source
COPY paket.dependencies .
RUN dotnet paket update

RUN echo "dotnet \"$(expr $(ls *.?sproj) : '\(.*\)\..sproj').dll\"\n" >> /tmp/start.sh
RUN chmod +x /tmp/start.sh

RUN dotnet publish -c ${BUILD_CONFIGURATION} -o /app

# final stage/image
FROM kmdrd/runtime:${DOTNET_VERSION}
COPY --from=build /tmp/start.sh /tmp/start.sh 
WORKDIR /app
COPY --from=build /app .

ENV port ${PORT_ARG}
ENTRYPOINT /tmp/start.sh
