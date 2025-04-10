#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM ccr.ccs.tencentyun.com/jfwang/dotnetcoresdk:3.1 AS runtime
WORKDIR /app
COPY /src/NGA/NGA.UI/publish ./
ENTRYPOINT ["dotnet", "NGA.UI.dll"]

 

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["NGA.UI/NGA.UI.csproj", "NGA.UI/"]
RUN dotnet restore "./NGA.UI/./NGA.UI.csproj"
COPY . .
WORKDIR "/src/NGA.UI"
RUN dotnet build "./NGA.UI.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./NGA.UI.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "NGA.UI.dll"]