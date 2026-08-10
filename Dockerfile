FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY src/SopmineWorkshop.API/SopmineWorkshop.API.csproj src/SopmineWorkshop.API/
COPY src/SopmineWorkshop.Application/SopmineWorkshop.Application.csproj src/SopmineWorkshop.Application/
COPY src/SopmineWorkshop.Contracts/SopmineWorkshop.Contracts.csproj src/SopmineWorkshop.Contracts/
COPY src/SopmineWorkshop.Domain/SopmineWorkshop.Domain.csproj src/SopmineWorkshop.Domain/
COPY src/SopmineWorkshop.Infrastructure/SopmineWorkshop.Infrastructure.csproj src/SopmineWorkshop.Infrastructure/

RUN dotnet restore src/SopmineWorkshop.API/SopmineWorkshop.API.csproj

COPY src/ src/
COPY ["Frontend/", "Frontend/"]
RUN dotnet publish src/SopmineWorkshop.API/SopmineWorkshop.API.csproj \
    --configuration Release \
    --output /app/publish \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 8080

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "SopmineWorkshop.API.dll"]
