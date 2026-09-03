FROM node:22-bookworm-slim AS frontend-build
WORKDIR /src/frontend
COPY frontend/package.json ./
RUN npm install
COPY frontend ./
RUN npm run build

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY backend ./backend
COPY tests ./tests
WORKDIR /src/backend
RUN dotnet restore Mercato.Platform.sln
RUN dotnet publish src/Mercato.Api/Mercato.Api.csproj -c Release --no-restore -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
RUN apt-get update \
    && apt-get install -y --no-install-recommends libgssapi-krb5-2 \
    && rm -rf /var/lib/apt/lists/*
COPY --from=build /app/publish .
RUN rm -rf /app/wwwroot && mkdir -p /app/wwwroot
COPY --from=frontend-build /src/frontend/dist/ /app/wwwroot/
EXPOSE 8080
ENTRYPOINT ["dotnet", "Mercato.Api.dll"]
