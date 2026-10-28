FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
COPY Directory.Build.props Directory.Packages.props .editorconfig /src/
COPY App/App.csproj /src/App/
COPY Web/Web.csproj /src/Web/
COPY ValidateResources/ValidateResources.csproj /src/ValidateResources/
RUN dotnet restore /src/ValidateResources

COPY App /src/App
COPY Web /src/Web
COPY ValidateResources /src/ValidateResources

# Requires named build context when building (https://docs.docker.com/build/concepts/context/#named-contexts)
# E.g. `docker build --build-context endpoint-definition-folder=PATH`
COPY --from=endpoint-definition-folder . /endpoint-definitions/

# Communicate endpoint definition folder to build so files become DLL embedded resources
ENV EndpointDefinitionFolder=/endpoint-definitions

# On failure, check for lines with `: error :` above for endpoint definition folder validation errors
RUN dotnet publish /src/App -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0-noble-chiseled
WORKDIR /app
COPY --from=build /app .

ENTRYPOINT ["dotnet", "App.dll"]
