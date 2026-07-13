# Build stage image
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /
COPY . .
WORKDIR "/"
RUN dotnet restore be4fe-cattle-home.slnx
RUN dotnet test be4fe-cattle-home.slnx --no-restore
RUN dotnet publish src/Api/Api.csproj -c Release -o /app/publish /p:UseAppHost=false --no-restore

# Final production image
FROM mcr.microsoft.com/dotnet/aspnet:10.0 
WORKDIR /app

# Add curl to template, CDP PLATFORM HEALTHCHECK REQUIREMENT
RUN apt update && \
    apt install curl -y && \
    apt-get clean && \
    rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .
EXPOSE 8085
ENTRYPOINT ["dotnet", "Defra.Lis.Be4Fe.CattleHome.Api.dll"]
