FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY src/Husk.Web/Husk.Web.csproj src/Husk.Web/
RUN dotnet restore src/Husk.Web/Husk.Web.csproj

COPY src/Husk.Web src/Husk.Web
RUN dotnet publish src/Husk.Web/Husk.Web.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080
ENV HUSK_API_BASE_URL=http://husk-api:8080
EXPOSE 8080

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Husk.Web.dll"]
