# syntax=docker/dockerfile:1.7

FROM mcr.microsoft.com/dotnet/sdk:10.0-noble AS build
WORKDIR /src

COPY . .

# A BuildKit secret may supply the environment's proxy CA. It is added only
# while restoring/building and is removed in the same layer.
RUN --mount=type=secret,id=proxy_ca,required=false \
    if [ -f /run/secrets/proxy_ca ]; then \
      cp /run/secrets/proxy_ca /usr/local/share/ca-certificates/build-proxy.crt && update-ca-certificates; \
    fi && \
    dotnet restore MiniPdm.sln && \
    dotnet publish src/MiniPdm.Api/MiniPdm.Api.csproj -c Release --no-restore -o /out/api && \
    dotnet publish src/MiniPdm.Desktop/MiniPdm.Desktop.csproj -c Release --no-restore -o /out/desktop && \
    dotnet tool restore && \
    mkdir -p /out/migrations && \
    PDM_CONNECTION_STRING='Host=localhost;Database=build;Username=build;Password=build' \
      dotnet ef migrations bundle \
        --project src/MiniPdm.Storage/MiniPdm.Storage.csproj \
        --startup-project src/MiniPdm.Storage/MiniPdm.Storage.csproj \
        --context PdmDbContext \
        --configuration Release \
        --output /out/migrations/efbundle && \
    if [ -f /run/secrets/proxy_ca ]; then \
      rm -f /usr/local/share/ca-certificates/build-proxy.crt && update-ca-certificates --fresh; \
    fi

FROM mcr.microsoft.com/dotnet/aspnet:10.0-noble AS api
WORKDIR /app
RUN --mount=type=secret,id=proxy_ca,required=false \
    if [ -f /run/secrets/proxy_ca ] && [ -s /run/secrets/proxy_ca ]; then \
      cp /run/secrets/proxy_ca /usr/local/share/ca-certificates/build-proxy.crt && update-ca-certificates; \
    fi && \
    apt-get update && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/* && \
    if [ -f /usr/local/share/ca-certificates/build-proxy.crt ]; then \
      rm -f /usr/local/share/ca-certificates/build-proxy.crt && update-ca-certificates --fresh; \
    fi
COPY --from=build /out/api/ ./
ENV ASPNETCORE_URLS=http://+:8080 \
    ImportStorage__DataRoot=/var/lib/minipdm/source-files
EXPOSE 8080
ENTRYPOINT ["dotnet", "MiniPdm.Api.dll"]

FROM mcr.microsoft.com/dotnet/aspnet:10.0-noble AS migrations
WORKDIR /app
COPY --from=build /out/migrations/ ./
RUN chmod +x ./efbundle
ENTRYPOINT ["./efbundle"]

FROM mcr.microsoft.com/dotnet/aspnet:10.0-noble AS desktop
ENV DEBIAN_FRONTEND=noninteractive \
    LANG=C.UTF-8 \
    LC_ALL=C.UTF-8 \
    DISPLAY=:0
RUN --mount=type=secret,id=proxy_ca,required=false \
    if [ -f /run/secrets/proxy_ca ] && [ -s /run/secrets/proxy_ca ]; then \
      cp /run/secrets/proxy_ca /usr/local/share/ca-certificates/build-proxy.crt && update-ca-certificates; \
    fi && \
    apt-get update && apt-get install -y --no-install-recommends \
      ca-certificates curl dbus-x11 fluxbox fonts-dejavu-core libatspi2.0-0 libfontconfig1 nginx python3 \
      libfreetype6 libice6 libsm6 libx11-6 libxcomposite1 libxcursor1 \
      libxdamage1 libxext6 libxfixes3 libxi6 libxkbcommon0 libxrandr2 \
      libxrender1 novnc websockify x11-utils x11vnc xvfb \
    && rm -rf /var/lib/apt/lists/* && \
    if [ -f /usr/local/share/ca-certificates/build-proxy.crt ]; then \
      rm -f /usr/local/share/ca-certificates/build-proxy.crt && update-ca-certificates --fresh; \
    fi
WORKDIR /app
COPY --from=build /out/desktop/ ./
COPY docker/desktop-entrypoint.sh /usr/local/bin/desktop-entrypoint
COPY docker/browser/nginx.conf /etc/nginx/nginx.conf
COPY docker/browser/pdm-picker.js /usr/share/novnc/pdm-picker.js
COPY docker/browser/pdm-picker.css /usr/share/novnc/pdm-picker.css
COPY docker/browser/inject-picker.py /usr/local/bin/inject-picker
RUN chmod +x /usr/local/bin/desktop-entrypoint /usr/local/bin/inject-picker && \
    /usr/local/bin/inject-picker /usr/share/novnc/vnc.html
EXPOSE 6080
ENTRYPOINT ["/usr/local/bin/desktop-entrypoint"]
