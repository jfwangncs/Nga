# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy csproj and restore dependencies
COPY ["NGA.Api.csproj", "./"]
RUN dotnet restore "NGA.Api.csproj"

# Copy everything else and build
COPY . .
RUN dotnet build "NGA.Api.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "NGA.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app 

# Copy published app
COPY --from=publish /app/publish . 

# Set environment variables
ENV ASPNETCORE_URLS=http://+:5000
ENV ASPNETCORE_ENVIRONMENT=Production

# Expose port
EXPOSE 5000
EXPOSE 9464
 
# Run the application
ENTRYPOINT ["dotnet", "NGA.Api.dll"]
