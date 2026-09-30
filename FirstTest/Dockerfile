FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG RESOURCES
COPY Web /src
COPY $RESOURCES/ /src/Resources/
RUN dotnet publish /src -o /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0-noble-chiseled
WORKDIR /app
COPY --from=build /app .

ENTRYPOINT ["dotnet", "Web.dll"]
