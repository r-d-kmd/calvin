# This is a temporary file do not edit
                            # edit D:\Dokumenter\KMDproject\hobbes\docker/Dockerfile.service instead
                         FROM builder AS build
ARG EXECUTABLE
ENV EXECUTABLE=${EXECUTABLE}

# final stage/image
FROM kmdrd/runtime
COPY --from=build /tmp/start.sh /tmp/start.sh 
WORKDIR /app
COPY --from=build /app .

ENV port 8085
ENTRYPOINT /tmp/start.sh