FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
COPY Directory.Build.props Directory.Packages.props /src/
COPY Web /src/Web
COPY dotNetRDF.Wrapping /src/dotNetRDF.Wrapping
COPY ValidateResources /src/ValidateResources

# Requires named build context when building (https://docs.docker.com/build/concepts/context/#named-contexts)
# E.g. `docker build --build-context endpoint-definition-folder=PATH`
COPY --from=endpoint-definition-folder . /endpoint-definitions/

# Communicate endpoint definition folder to build so files become DLL embedded resources
ENV EndpointDefinitionFolder=/endpoint-definitions

# On failure, check for lines starting with `ValidateResources : error :` above for endpoint definition folder validation errors
RUN dotnet publish /src/Web -o /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0-noble-chiseled
WORKDIR /app
COPY --from=build /app .

ENTRYPOINT ["dotnet", "Web.dll"]
