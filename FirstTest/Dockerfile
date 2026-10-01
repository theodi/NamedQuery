FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
COPY Web /src/Web
COPY ValidateResources /src/ValidateResources

# Requires named build context when building (https://docs.docker.com/build/concepts/context/#named-contexts)
# E.g. `docker build --build-context resources=PATH`
COPY --from=endpoint-definition-folder . /src/Web/Resources/

RUN dotnet publish /src/Web -o /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0-noble-chiseled
WORKDIR /app
COPY --from=build /app .

ENTRYPOINT ["dotnet", "Web.dll"]
