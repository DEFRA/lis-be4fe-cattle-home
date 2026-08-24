ARG PARENT_VERSION=10.0
ARG PORT=8085

# Build stage image
FROM mcr.microsoft.com/dotnet/sdk:${PARENT_VERSION} AS build
WORKDIR /src
COPY . .
WORKDIR "/src"
RUN dotnet test be4fe-cattle-home.slnx
RUN dotnet publish src/Api -c Release -o /app/publish /p:UseAppHost=false

# Final production image
FROM mcr.microsoft.com/dotnet/aspnet:${PARENT_VERSION} as production
ARG PORT 
WORKDIR /app

# Add curl to template, CDP PLATFORM HEALTHCHECK REQUIREMENT
RUN apt update && \
    apt install curl -y && \
    apt-get clean && \
    rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .
EXPOSE ${PORT}
ENV ASPNETCORE_URLS=http://+:${PORT}
ENTRYPOINT ["dotnet", "Defra.Lis.Be4Fe.Api.dll"]
