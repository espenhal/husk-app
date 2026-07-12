FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY src/Husk.Api/Husk.Api.csproj src/Husk.Api/
RUN dotnet restore src/Husk.Api/Husk.Api.csproj

COPY src/Husk.Api src/Husk.Api
RUN dotnet publish src/Husk.Api/Husk.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080
ENV HUSK_DATA_PATH=/data/husk/tasks.json
EXPOSE 8080

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Husk.Api.dll"]
