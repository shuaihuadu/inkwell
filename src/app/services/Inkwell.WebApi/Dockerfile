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
COPY src/core/providers/Inkwell.Persistence.EntityFrameworkCore/Inkwell.Persistence.EntityFrameworkCore.csproj src/core/providers/Inkwell.Persistence.EntityFrameworkCore/
COPY src/core/providers/Inkwell.Persistence.InMemory/Inkwell.Persistence.InMemory.csproj src/core/providers/Inkwell.Persistence.InMemory/
COPY src/core/providers/Inkwell.Persistence.SqlServer/Inkwell.Persistence.SqlServer.csproj src/core/providers/Inkwell.Persistence.SqlServer/
COPY src/core/providers/Inkwell.Queue.Redis/Inkwell.Queue.Redis.csproj src/core/providers/Inkwell.Queue.Redis/
COPY src/app/aspire/Inkwell.ServiceDefaults/Inkwell.ServiceDefaults.csproj src/app/aspire/Inkwell.ServiceDefaults/
COPY src/app/services/Inkwell.WebApi/Inkwell.WebApi.csproj src/app/services/Inkwell.WebApi/

# 还原依赖
RUN dotnet restore src/app/services/Inkwell.WebApi/Inkwell.WebApi.csproj

# 复制全部源码并发布
COPY src/ src/
RUN dotnet publish src/app/services/Inkwell.WebApi/Inkwell.WebApi.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

# ---- Runtime Stage ----
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:5000
EXPOSE 5000

ENTRYPOINT ["dotnet", "Inkwell.WebApi.dll"]
