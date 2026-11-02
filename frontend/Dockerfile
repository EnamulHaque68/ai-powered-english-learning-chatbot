# Multi-stage Dockerfile for Angular 22 Frontend
FROM node:24-alpine AS build
WORKDIR /app

# Install dependencies
COPY frontend/package.json frontend/package-lock.json ./
RUN npm ci

# Copy source and build production bundle
COPY frontend/ .
RUN npm run build -- --configuration production

# Production Nginx runtime
FROM nginx:alpine
COPY --from=build /app/dist/frontend/browser /usr/share/nginx/html
COPY frontend/nginx.conf /etc/nginx/conf.d/default.conf

EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]
