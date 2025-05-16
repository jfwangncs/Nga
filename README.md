# Nga
Nga Reptile



sudo docker run --name ui -d -p 80:80 -e ASPNETCORE_ENVIRONMENT=Production -v /app/ngb/ui:/app -w /app  mcr.microsoft.com/dotnet/core/aspnet:2.2-stretch-slim dotnet Display.dll



sudo docker run --name producer1 -d -e ASPNETCORE_ENVIRONMENT=Production -e SPEED=quickly -v /app/ngb/producer:/app -v /etc/localtime:/etc/localtime:ro -w /app mcr.microsoft.com/dotnet/core/runtime:2.2-stretch-slim dotnet NGA.Producer.dll



sudo docker run --name consumer1 -d -e ASPNETCORE_ENVIRONMENT=Production -e SPEED=quickly -v /app/ngb/consumer:/app -v /etc/localtime:/etc/localtime:ro -w /app mcr.microsoft.com/dotnet/core/runtime:2.2-stretch-slim dotnet NGA.Consumer.dll