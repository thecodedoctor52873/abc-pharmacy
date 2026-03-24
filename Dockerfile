# Test stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS test
WORKDIR /src
COPY PharmacyApp.Core/PharmacyApp.Core.csproj PharmacyApp.Core/
COPY PharmacyApp/PharmacyApp.csproj PharmacyApp/
COPY PharmacyApp.Tests/PharmacyApp.Tests.csproj PharmacyApp.Tests/
RUN dotnet restore PharmacyApp.Tests/PharmacyApp.Tests.csproj
COPY . .
RUN dotnet test PharmacyApp.Tests/PharmacyApp.Tests.csproj --no-restore

# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY PharmacyApp.Core/PharmacyApp.Core.csproj PharmacyApp.Core/
COPY PharmacyApp/PharmacyApp.csproj PharmacyApp/
RUN dotnet restore PharmacyApp/PharmacyApp.csproj
COPY PharmacyApp.Core/ PharmacyApp.Core/
COPY PharmacyApp/ PharmacyApp/
RUN dotnet publish PharmacyApp/PharmacyApp.csproj -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "PharmacyApp.dll"]