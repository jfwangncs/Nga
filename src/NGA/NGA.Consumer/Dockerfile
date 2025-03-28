#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/runtime:8.0 AS base
USER app
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["NGA.Consumer/NGA.Consumer.csproj", "NGA.Consumer/"]
COPY ["NGA.Base/NGA.Base.csproj", "NGA.Base/"]
COPY ["NGA.Models/NGA.Models.csproj", "NGA.Models/"]
RUN dotnet restore "./NGA.Consumer/./NGA.Consumer.csproj"
COPY . .
WORKDIR "/src/NGA.Consumer"
RUN dotnet build "./NGA.Consumer.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./NGA.Consumer.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "NGA.Consumer.dll"]