# ---- Build Stage ----
FROM node:20-slim AS build
WORKDIR /app

COPY src/app/webapp/package.json src/app/webapp/package-lock.json ./

# Vite 8 使用 Rolldown + LightningCSS（Rust native），Windows lockfile 不含 Linux binding
# 使用 npm install 并显式安装缺失的平台 native bindings
RUN npm install \
    && npm install @rolldown/binding-linux-x64-gnu lightningcss-linux-x64-gnu --no-save

COPY src/app/webapp/ ./

# 构建时注入 API 地址（运行时通过 nginx 反代到 webapi）
ENV VITE_API_BASE=""
RUN npm run build

# ---- Runtime Stage ----
FROM nginx:alpine AS runtime

# 复制构建产物
COPY --from=build /app/dist /usr/share/nginx/html

# 复制 nginx 配置
COPY docker/nginx.conf /etc/nginx/conf.d/default.conf

EXPOSE 80

CMD ["nginx", "-g", "daemon off;"]
