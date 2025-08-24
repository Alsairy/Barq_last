FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["Backend/src/BARQ.API/BARQ.API.csproj", "src/BARQ.API/"]
COPY ["Backend/src/BARQ.Application/BARQ.Application.csproj", "src/BARQ.Application/"]
COPY ["Backend/src/BARQ.Infrastructure/BARQ.Infrastructure.csproj", "src/BARQ.Infrastructure/"]
COPY ["Backend/src/BARQ.Core/BARQ.Core.csproj", "src/BARQ.Core/"]
RUN dotnet restore "src/BARQ.API/BARQ.API.csproj"
COPY Backend/ .
WORKDIR "/src/src/BARQ.API"
RUN dotnet build "BARQ.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "BARQ.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "BARQ.API.dll"]
