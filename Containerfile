# ============================================================
# TaskFlow — imagen de la demo publica (Podman, RHEL 9)
# ============================================================
# El front y la API estan en repos distintos, asi que el contexto de build es
# la carpeta que contiene a los dos (GestorTareas.API/ y GestorTareas.Web/):
#
#   podman build --format docker \
#     -f GestorTareas.API/Containerfile \
#     --ignorefile GestorTareas.API/.containerignore \
#     -t taskflow-demo .
#
# --format docker es necesario para que Podman conserve el HEALTHCHECK.
# Toda la configuracion entra por variables de entorno (ver
# deploy/demo/taskflow-demo.env.example); la imagen no lleva secretos.

# --- 1. Front (Vue + Vite) ---
FROM docker.io/library/node:22-alpine AS front
WORKDIR /src
RUN npm install -g pnpm@11.18.0
COPY GestorTareas.Web/package.json GestorTareas.Web/pnpm-lock.yaml ./
RUN pnpm install --frozen-lockfile
COPY GestorTareas.Web/ ./
# Mismo origen que la API: rutas relativas. VITE_DEMO agrega la meta noindex.
ENV VITE_API_URL=/api \
    VITE_HUB_URL=/hubs/tareas \
    VITE_DEMO=true
RUN pnpm run build

# --- 2. API (.NET 9) con el front dentro de wwwroot ---
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS api
WORKDIR /src
COPY GestorTareas.API/GestorTareas.API.csproj ./
RUN dotnet restore
COPY GestorTareas.API/ ./
COPY --from=front /src/dist ./wwwroot
RUN dotnet publish -c Release -o /app --no-restore /p:UseAppHost=false
# Algunos archivos del repo tienen permisos 600; el usuario sin privilegios
# de la imagen final debe poder leerlos (pero no modificarlos: dueño root).
RUN chmod -R a+rX /app

# --- 3. Imagen final ---
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=api /app ./

# Logs a la salida estandar (los recoge journald); sin el SQL de cada consulta.
ENV ASPNETCORE_HTTP_PORTS=8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    Logging__Console__FormatterName=simple \
    Logging__Console__FormatterOptions__SingleLine=true \
    Logging__LogLevel__Default=Information \
    Logging__LogLevel__Microsoft.AspNetCore=Warning \
    Logging__LogLevel__Microsoft.EntityFrameworkCore=Warning

# Usuario sin privilegios que ya trae la imagen de Microsoft.
USER $APP_UID
EXPOSE 8080

HEALTHCHECK --interval=30s --timeout=5s --start-period=40s --retries=3 \
    CMD ["dotnet", "GestorTareas.API.dll", "verificar-salud"]

ENTRYPOINT ["dotnet", "GestorTareas.API.dll"]
