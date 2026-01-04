#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/runtime:8.0 AS base
RUN mkdir -p /app && chmod 777 /app
USER app
WORKDIR /app

ARG APPLICATION_NAME
ENV ASPNETCORE_APPLICATION=${APPLICATION_NAME}

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["NGA.Console/NGA.Console.csproj", "NGA.Console/"] 
COPY ["NGA.Models/NGA.Models.csproj", "NGA.Models/"]
RUN dotnet restore "./NGA.Console/./NGA.Console.csproj"
COPY . .
WORKDIR "/src/NGA.Console"
RUN dotnet build "./NGA.Console.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./NGA.Console.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENV ASPNETCORE_HTTP_PORT=
ENTRYPOINT ["dotnet", "NGA.Console.dll"]