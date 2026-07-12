# Build Stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /source
COPY . .
RUN dotnet restore
RUN dotnet publish -c Release -o /app

# Runtime Stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .
# Render uses a dynamic PORT, so we tell .NET to listen to it
ENV ASPNETCORE_URLS=http://+:10000
ENTRYPOINT ["dotnet", "HospitalFlow.dll"]