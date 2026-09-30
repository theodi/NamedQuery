FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
COPY Web /src
RUN dotnet publish /src -o /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0-noble-chiseled
WORKDIR /app
COPY --from=build /app .

ARG RESOURCES
COPY $RESOURCES/ ./resources/
ENV Options__BasePath=/app/resources/

ENTRYPOINT ["dotnet", "Web.dll"]
