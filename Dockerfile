# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /source

# Copia tutto il codice sorgente
COPY . .

# Ripristina le dipendenze
RUN dotnet restore "Xplore.sln"

# Compila l'API in configurazione Release
WORKDIR /source/src/Xplore.API
RUN dotnet publish "Xplore.API.csproj" -c Release -o /app/publish

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# Usa la porta 8080 standard per le app docker su Render
EXPOSE 8080
ENV ASPNETCORE_HTTP_PORTS=8080
ENV ASPNETCORE_ENVIRONMENT=Production

# Disabilita l'attesa per i container dipendenti (RabbitMQ/Qdrant) per non bloccare l'API su cloud
ENV DOTNET_EnableAspireQdrant=false
ENV DOTNET_EnableAspireRabbitMQ=false

ENTRYPOINT ["dotnet", "Xplore.API.dll"]
