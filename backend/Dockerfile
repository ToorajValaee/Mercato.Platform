FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# The solution lives under backend/, but it references the active test projects
# from the repository-level tests/ directory. Keep both trees available inside
# the build context so solution restore resolves every project reference.
COPY backend ./backend
COPY tests ./tests

WORKDIR /src/backend
RUN dotnet restore Mercato.Platform.sln
RUN dotnet publish src/Mercato.Api/Mercato.Api.csproj -c Release --no-restore -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Npgsql can probe GSS/Kerberos support at runtime. Install the native library so
# the container does not emit libgssapi_krb5.so.2 load failures.
RUN apt-get update \
    && apt-get install -y --no-install-recommends libgssapi-krb5-2 \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .

EXPOSE 8080
ENTRYPOINT ["dotnet", "Mercato.Api.dll"]
