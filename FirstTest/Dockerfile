FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
COPY Directory.Packages.props /src/
COPY Web /src/Web
COPY ValidateResources /src/ValidateResources

# Requires named build context when building (https://docs.docker.com/build/concepts/context/#named-contexts)
# E.g. `docker build --build-context endpoint-definition-folder=PATH`
COPY --from=endpoint-definition-folder . /src/Web/Resources/

# On failure, check for lines starting with `ValidateResources : error :` above for endpoint definition folder validation errors
RUN dotnet publish /src/Web -o /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0-noble-chiseled
WORKDIR /app
COPY --from=build /app .

ENTRYPOINT ["dotnet", "Web.dll"]
