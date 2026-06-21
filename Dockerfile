# syntax=docker/dockerfile:1
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY BitMono.ObfuscationService/BitMono.ObfuscationService.csproj BitMono.ObfuscationService/
RUN dotnet restore BitMono.ObfuscationService/BitMono.ObfuscationService.csproj
COPY . .
RUN dotnet publish BitMono.ObfuscationService/BitMono.ObfuscationService.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
# Listen port comes from HTTP_PORTS (Aspire) or ASPNETCORE_URLS (standalone docker run).
ENTRYPOINT ["dotnet", "BitMono.ObfuscationService.dll"]
