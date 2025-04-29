ARG auth=pat

# this is the base image ===================================================================
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

# this is the build image  =================================================================
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY . .
COPY Sim.Api/appsettings.docker.json /src/Sim.Api/appsettings.json

WORKDIR "/src/Sim.Api"
RUN dotnet build "Sim.Api.csproj" -c Release

# This is the publishing image ============================================================
FROM build AS publish
RUN dotnet publish "Sim.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# This is the final image =================================================================
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Sim.Api.dll"]
