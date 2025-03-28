#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM ccr.ccs.tencentyun.com/jfwang/dotnetcoresdk:3.1 AS runtime
WORKDIR /app
COPY /src/NGA/NGA.UI/publish ./
ENTRYPOINT ["dotnet", "NGA.UI.dll"]