#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 5002

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY Directory.Build.props /
# copy Directory.Packages.props file for Central Package Management
COPY Directory.Packages.props /

# copy csproj and restore as distinct layers
COPY src/Squidlr.Hosting/Squidlr.Hosting.csproj Squidlr.Hosting/
COPY src/Squidlr/Squidlr.csproj Squidlr/
COPY src/Squidlr.Web/Squidlr.Web.csproj Squidlr.Web/
RUN dotnet restore Squidlr.Web/Squidlr.Web.csproj

# copy and build app
COPY src/Squidlr.Hosting/ Squidlr.Hosting/
COPY src/Squidlr/ Squidlr/
COPY src/Squidlr.Web/ Squidlr.Web/
WORKDIR /src/Squidlr.Web
RUN dotnet build "Squidlr.Web.csproj" -c release --no-restore -o /app/build

FROM build AS publish
RUN dotnet publish "Squidlr.Web.csproj" -c Release --no-restore -o /app/publish

# final stage/image
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Squidlr.Web.dll"]