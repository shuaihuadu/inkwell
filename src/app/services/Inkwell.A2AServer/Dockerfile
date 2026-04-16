# ---- Build Stage ----
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# 复制解决方案级别的配置文件
COPY Directory.Build.props ./
COPY Directory.Packages.props ./
COPY Inkwell.slnx ./

# 复制所有 csproj 以利用 Docker 层缓存
COPY src/core/Inkwell.Abstractions/Inkwell.Abstractions.csproj src/core/Inkwell.Abstractions/
COPY src/core/Inkwell.Core/Inkwell.Core.csproj src/core/Inkwell.Core/
COPY src/app/services/Inkwell.A2AServer/Inkwell.A2AServer.csproj src/app/services/Inkwell.A2AServer/

# 还原依赖
RUN dotnet restore src/app/services/Inkwell.A2AServer/Inkwell.A2AServer.csproj

# 复制全部源码并发布
COPY src/ src/
RUN dotnet publish src/app/services/Inkwell.A2AServer/Inkwell.A2AServer.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

# ---- Runtime Stage ----
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:5100
EXPOSE 5100

ENTRYPOINT ["dotnet", "Inkwell.A2AServer.dll"]
