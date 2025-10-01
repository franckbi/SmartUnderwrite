# Multi-stage build for SmartUnderwrite Frontend
FROM node:20-alpine AS build
WORKDIR /app

# Copy package files and install dependencies
COPY package*.json ./
RUN npm ci
# Workaround for Rollup ARM64 issue
RUN npm install @rollup/rollup-linux-arm64-musl --save-dev || true

# Copy source code and build
COPY . .
RUN npm run build

FROM nginx:alpine AS final

# Copy built application
COPY --from=build /app/dist /usr/share/nginx/html

# Copy custom nginx configuration (after copying the app)
COPY nginx.conf /etc/nginx/nginx.conf

# Note: Running as root in container is acceptable for nginx

EXPOSE 3000

CMD ["nginx", "-g", "daemon off;"]