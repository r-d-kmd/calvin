ARG DOTNET_VERSION=5.0
FROM kmdrd/sdk:${DOTNET_VERSION} AS build-base

ARG BUILD_CONFIGURATION_ARG="Release"
ENV BUILD_CONFIGURATION ${BUILD_CONFIGURATION_ARG}

ARG PORT_ARG 8085

ENV PAKET_SKIP_RESTORE_TARGETS=true

COPY .fake/build.fsx/.paket/Paket.Restore.targets /.paket/Paket.Restore.targets
COPY paket.lock .
COPY paket.dependencies .

RUN dotnet tool restore
RUN dotnet paket restore

FROM build-base as build

COPY ./src /source
WORKDIR /source

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
