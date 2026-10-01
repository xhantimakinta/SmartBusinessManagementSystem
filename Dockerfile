FROM node:22-alpine AS frontend-build
WORKDIR /workspace/frontend
COPY frontend/package*.json ./
RUN npm ci
COPY frontend/ ./
ENV VITE_BASE_PATH=/
RUN npm run build

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS api-build
WORKDIR /src
COPY backend/ ./
RUN dotnet restore SmartBusiness.Api/SmartBusiness.Api.csproj
RUN dotnet publish SmartBusiness.Api/SmartBusiness.Api.csproj -c Release -o /app/publish --no-restore --no-self-contained

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=api-build /app/publish .
COPY --from=frontend-build /workspace/frontend/dist ./wwwroot
EXPOSE 10000
ENV ASPNETCORE_HTTP_PORTS=10000
USER $APP_UID
ENTRYPOINT ["dotnet", "SmartBusiness.Api.dll"]