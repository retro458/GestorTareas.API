#!/usr/bin/env bash
# ============================================================
# Pruebas de humo de la demo publica de TaskFlow.
#
#   deploy/demo/probar-demo.sh [URL]      (por defecto http://127.0.0.1:8080)
#
# OJO: la prueba del tope crea tareas hasta llenar la demo (200). Correrla
# solo contra la demo y luego reiniciarla:
#   podman exec taskflow-demo dotnet GestorTareas.API.dll demo-reiniciar
# ============================================================
set -uo pipefail

BASE="${1:-http://127.0.0.1:8080}"
API="$BASE/api"
TMP="$(mktemp -d)"
trap 'rm -rf "$TMP"' EXIT
fallos=0

ok()    { echo "  OK    $1"; }
falla() { echo "  FALLA $1"; fallos=$((fallos + 1)); }
espera() { # espera <descripcion> <esperado> <obtenido>
  if [ "$2" = "$3" ]; then ok "$1"; else falla "$1 (esperado $2, obtenido $3)"; fi
}

login() { # login <usuario> <password> -> codigo HTTP; guarda la cookie
  curl -s -o /dev/null -w '%{http_code}' -c "$TMP/$1.jar" \
    -H 'Content-Type: application/json' \
    -d "{\"nombreUsuario\":\"$1\",\"password\":\"$2\"}" "$API/auth/login"
}

echo "== Contenedor y front ($BASE)"
espera "/salud responde" 200 "$(curl -s -o /dev/null -w '%{http_code}' "$BASE/salud")"
espera "sirve el front en /" 200 "$(curl -s -o /dev/null -w '%{http_code}' "$BASE/")"
curl -s "$BASE/" | grep -q 'name="robots" content="noindex' && ok "meta robots noindex" || falla "meta robots noindex"
curl -s -D - -o /dev/null "$BASE/" | grep -qi '^x-robots-tag: noindex' && ok "cabecera X-Robots-Tag" || falla "cabecera X-Robots-Tag"
espera "ruta del SPA (/login) devuelve el front" 200 "$(curl -s -o /dev/null -w '%{http_code}' "$BASE/login")"
espera "OpenAPI apagado" 404 "$(curl -s -o /dev/null -w '%{http_code}' "$BASE/openapi/v1.json")"
curl -s "$API/demo" | grep -q '"activo":true' && ok "GET /api/demo -> activo" || falla "GET /api/demo -> activo"

echo "== Las tres cuentas entran"
espera "jefe.demo" 200 "$(login jefe.demo 'Jefe2026!')"
espera "encargado.demo" 200 "$(login encargado.demo 'Encargado2026!')"
espera "empleado.demo" 200 "$(login empleado.demo 'Empleado2026!')"

echo "== Cuentas de demo protegidas"
id_de() { # id del usuario por nombre de usuario, visto por el jefe
  curl -s -b "$TMP/jefe.demo.jar" "$API/usuario/empleados/todos" "$API/usuario/empleados/inactivos" \
    | tr '{' '\n' | grep "\"nombreUsuario\":\"$1\"" | head -1 | sed 's/.*"id":\([0-9]*\).*/\1/'
}
ID_EMPLEADO="$(id_de empleado.demo)"
espera "no se cambia la contraseña de jefe.demo (id 1)" 403 "$(curl -s -o /dev/null -w '%{http_code}' -b "$TMP/jefe.demo.jar" \
  -X PATCH -H 'Content-Type: application/json' -d '{"nuevaPassword":"OtraClave123"}' "$API/usuario/1/restablecer-password")"
espera "no se cambia la contraseña de empleado.demo" 403 "$(curl -s -o /dev/null -w '%{http_code}' -b "$TMP/jefe.demo.jar" \
  -X PATCH -H 'Content-Type: application/json' -d '{"nuevaPassword":"OtraClave123"}' "$API/usuario/$ID_EMPLEADO/restablecer-password")"
espera "no se desactiva empleado.demo" 403 "$(curl -s -o /dev/null -w '%{http_code}' -b "$TMP/jefe.demo.jar" \
  -X PATCH "$API/usuario/$ID_EMPLEADO/desactivar")"
espera "no se edita empleado.demo" 403 "$(curl -s -o /dev/null -w '%{http_code}' -b "$TMP/jefe.demo.jar" \
  -X PUT -H 'Content-Type: application/json' -d '{"nombreUsuario":"otro","departamentosIds":[1]}' "$API/usuario/$ID_EMPLEADO/editar")"

echo "== Largo maximo de textos"
LARGO="$(head -c 201 /dev/zero | tr '\0' x)"
espera "titulo de 201 caracteres rechazado" 400 "$(curl -s -o /dev/null -w '%{http_code}' -b "$TMP/jefe.demo.jar" \
  -H 'Content-Type: application/json' -d "{\"titulo\":\"$LARGO\",\"asignadoA\":$ID_EMPLEADO,\"departamentoId\":1,\"prioridadId\":2}" "$API/tareas/crear")"

echo "== Tope de tareas (llena la demo hasta 200)"
ultimo=""; respuesta=""
for i in $(seq 1 200); do
  respuesta="$(curl -s -w ' %{http_code}' -b "$TMP/jefe.demo.jar" -H 'Content-Type: application/json' \
    -d "{\"titulo\":\"Prueba de tope $i\",\"asignadoA\":$ID_EMPLEADO,\"departamentoId\":1,\"prioridadId\":2}" "$API/tareas/crear")"
  ultimo="${respuesta##* }"
  [ "$ultimo" != 200 ] && break
done
espera "al llegar al tope responde 409" 409 "$ultimo"
echo "$respuesta" | grep -q 'La demo llegó a su límite; se reinicia esta noche' && ok "mensaje del tope" || falla "mensaje del tope"

echo "== Rate limiting del login"
codigos=""
for i in 1 2 3 4 5 6; do codigos="$codigos $(login nadie nada)"; done
echo "$codigos" | grep -q 429 && ok "bloquea intentos repetidos (429)" || falla "bloquea intentos repetidos:$codigos"

echo
if [ "$fallos" -eq 0 ]; then echo "Todas las pruebas pasaron."; else echo "$fallos prueba(s) fallaron."; fi
exit "$fallos"
