# syntax=docker/dockerfile:1

# ---------- BUILD ----------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# 1) Copy solution + csproj first (better caching)
COPY Backend/src/BARQ.API/BARQ.API.csproj Backend/src/BARQ.API/
COPY Backend/src/BARQ.Application/BARQ.Application.csproj Backend/src/BARQ.Application/
COPY Backend/src/BARQ.Infrastructure/BARQ.Infrastructure.csproj Backend/src/BARQ.Infrastructure/
COPY Backend/src/BARQ.Core/BARQ.Core.csproj Backend/src/BARQ.Core/

RUN dotnet restore Backend/src/BARQ.API/BARQ.API.csproj

# 2) Copy the rest of the Backend
COPY Backend/ ./Backend/

# 3) Build
RUN dotnet publish Backend/src/BARQ.API/BARQ.API.csproj -c Release -o /app/publish

# ---------- RUNTIME ----------
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish ./
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "BARQ.API.dll"]
