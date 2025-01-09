#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 5001

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY Directory.Build.props /
# copy Directory.Packages.props file for Central Package Management
COPY Directory.Packages.props /

# copy csproj and restore as distinct layers
COPY src/Squidlr.Hosting/Squidlr.Hosting.csproj Squidlr.Hosting/
COPY src/Squidlr/Squidlr.csproj Squidlr/
COPY src/Squidlr.Api/Squidlr.Api.csproj Squidlr.Api/
RUN dotnet restore Squidlr.Api/Squidlr.Api.csproj

# copy and build app
COPY src/Squidlr.Hosting/ Squidlr.Hosting/
COPY src/Squidlr/ Squidlr/
COPY src/Squidlr.Api/ Squidlr.Api/
WORKDIR /src/Squidlr.Api
RUN dotnet build "Squidlr.Api.csproj" -c release --no-restore -o /app/build

FROM build AS publish
RUN dotnet publish "Squidlr.Api.csproj" -c Release --no-restore -o /app/publish

# final stage/image
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Squidlr.Api.dll"]