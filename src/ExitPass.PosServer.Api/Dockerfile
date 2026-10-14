ARG DOTNET_SDK_IMAGE=mcr.microsoft.com/dotnet/sdk@sha256:ec9c0a0dc5f60adc2065762050638dd5ed5facfc53716d4580a42d86027c8e80
ARG DOTNET_ASPNET_IMAGE=mcr.microsoft.com/dotnet/aspnet@sha256:a3cd573ac05cf88ca496e3309cb7f7c44aa16664352d1ee41eda93154f50cdbf

FROM ${DOTNET_SDK_IMAGE} AS build
WORKDIR /src

COPY . .
RUN dotnet restore src/ExitPass.PosServer.Api/ExitPass.PosServer.Api.csproj
RUN dotnet publish src/ExitPass.PosServer.Api/ExitPass.PosServer.Api.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish \
    /p:UseAppHost=false

FROM ${DOTNET_ASPNET_IMAGE} AS final
RUN apt-get update \
    && apt-get install --yes --no-install-recommends ca-certificates curl \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /app
RUN mkdir -p /var/lib/exitpass/annex-e1 \
    && chown -R app:app /app /var/lib/exitpass

COPY --from=build --chown=app:app /app/publish .
COPY --chown=app:app deploy/api/entrypoint.sh /usr/local/bin/exitpass-api-entrypoint
RUN chmod 0555 /usr/local/bin/exitpass-api-entrypoint

USER app
EXPOSE 8443
ENTRYPOINT ["/usr/local/bin/exitpass-api-entrypoint"]
