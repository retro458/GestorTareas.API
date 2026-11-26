# TaskFlow — demo pública

Demo pública de TaskFlow para el portafolio: `https://taskflow-demo.nodesv.com`.
Corre en RHEL 9 con Podman, detrás de Cloudflare Tunnel, con datos ficticios
("Comercial El Roble") que se reinician cada noche.

Todo lo de la demo se activa **solo** con `Demo__Activo=true`. Sin esa variable
la app se comporta exactamente igual que en producción.

## Qué cambia con `Demo__Activo=true`

| Área | Comportamiento |
|---|---|
| Front | Franja fija "Demo con datos ficticios · se reinicia cada noche". La pantalla de entrada muestra las tres cuentas con "Entrar como…". |
| Cuentas de demo | `jefe.demo`, `encargado.demo` y `empleado.demo` no se pueden editar, desactivar ni cambiar de contraseña: la API responde 403 "No disponible en la demo." y el front oculta esas acciones. |
| Topes | 200 tareas, 300 comentarios y 20 usuarios creados. Al llegar: 409 "La demo llegó a su límite; se reinicia esta noche." |
| Textos | Largo máximo validado en la API (títulos 200, descripciones 2000, comentarios 1000, nombres según la columna de la base). |
| Correo | No se envía nada (Resend desactivado); queda una línea en el log sin el destinatario. No hacen falta credenciales de correo. |
| Login | Rate limiting: 5 intentos por minuto por IP (toma la IP real de `CF-Connecting-IP`). |
| Encabezados | `X-Robots-Tag: noindex`, `X-Content-Type-Options`, `X-Frame-Options`, `Referrer-Policy`, `Permissions-Policy`. El front lleva `<meta name="robots" content="noindex">`. |
| Scalar/OpenAPI | Apagados (también en Development). |
| Base vacía | Al arrancar crea las tablas si la base no tiene ninguna y siembra la demo si no hay datos. |

El código de la demo está en `Demo/` (API) y `src/stores/demo.ts`,
`src/components/FranjaDemo.vue` (front).

### Cuentas

| Usuario | Contraseña | Rol |
|---|---|---|
| `jefe.demo` | `Jefe2026!` | Jefe |
| `encargado.demo` | `Encargado2026!` | Encargado de Contabilidad |
| `empleado.demo` | `Empleado2026!` | Empleado de Contabilidad |

Además hay 5 empleados sin acceso (contraseña aleatoria) para que las listas no
se vean vacías. Las contraseñas se definen en `Demo/DemoSembrador.cs`.

## Construir la imagen

El front y la API están en repos distintos. Clonar los dos en una misma carpeta,
con estos nombres, y construir desde esa carpeta:

```
taskflow/
├── GestorTareas.API/   (este repo)
└── GestorTareas.Web/
```

```bash
cd taskflow
podman build --format docker \
  -f GestorTareas.API/Containerfile \
  --ignorefile GestorTareas.API/.containerignore \
  -t taskflow-demo .
```

- `--format docker` es necesario: con el formato OCI (el de Podman por defecto)
  se pierde el `HEALTHCHECK`.
- La imagen no lleva secretos ni `.env`: toda la configuración entra por
  variables de entorno.
- Etapas: Node compila el front → SDK de .NET 9 publica la API con el front en
  `wwwroot` → `aspnet:9.0` corre como usuario `app` (sin privilegios) en el 8080.

## Variables

Ver `deploy/demo/taskflow-demo.env.example`. Copiarlo a, por ejemplo,
`/etc/taskflow/taskflow-demo.env` con permisos `600` y completar:

| Variable | Uso |
|---|---|
| `Demo__Activo` | `true` |
| `DB_CONNECTION_STRING` | SQL Server en un contenedor aparte |
| `JWT_SECRET_KEY` | Al menos 32 bytes aleatorios (`openssl rand -hex 32`) |
| `JWT_ISSUER`, `JWT_AUDIENCE` | `https://taskflow-demo.nodesv.com` |
| `FRONTEND_URL` | `https://taskflow-demo.nodesv.com` |

### Base de datos

La base, su login y su usuario se crean a mano en el servidor; la app **no** los
crea. Al arrancar con la demo activa:

- Si la base **no tiene ninguna tabla**, crea el esquema completo.
- Si ya tiene tablas, no toca el esquema (si faltara alguna tabla suelta, no la
  agrega: en ese caso vaciar la base y dejar que la cree de nuevo).
- Si no hay usuarios, siembra la demo.

El usuario de la base necesita poder crear tablas y reiniciar los contadores
`IDENTITY`: `db_owner` en esa base (o `db_ddladmin` + `db_datareader` +
`db_datawriter`).

## Correr el contenedor

Ejemplo con una red de Podman compartida con el contenedor de SQL Server
(`taskflow-sql`). El puerto se publica solo en `127.0.0.1`: el acceso público es
por Cloudflare Tunnel (que soporta WebSockets, necesarios para SignalR).

```bash
podman network create taskflow            # una sola vez
# (el contenedor de SQL Server debe estar en la misma red: --network taskflow)

podman run -d --name taskflow-demo \
  --network taskflow \
  --env-file /etc/taskflow/taskflow-demo.env \
  -p 127.0.0.1:8080:8080 \
  --restart=always \
  taskflow-demo
```

- Logs: salida estándar, en una línea (`podman logs -f taskflow-demo`, o
  journald si se corre con systemd/Quadlet).
- Salud: `GET /salud` responde 200 si la app y la base responden, 503 si no hay
  base. `podman healthcheck run taskflow-demo`.
- En el túnel de Cloudflare, apuntar `taskflow-demo.nodesv.com` a
  `http://127.0.0.1:8080`.

## Reiniciar la demo

```bash
podman exec taskflow-demo dotnet GestorTareas.API.dll demo-reiniciar
```

Borra **todos** los datos y vuelve a sembrar, en una transacción. Las fechas son
relativas al día de la siembra (vence mañana, venció hace 3 días...) y los Ids se
reinician, así que dos reinicios el mismo día dejan los mismos datos. Sin
`Demo__Activo=true` termina con código 1 sin conectarse a la base.

Reinicio nocturno con systemd (3:00 a. m.):

```bash
sudo cp deploy/demo/taskflow-demo-reiniciar.{service,timer} /etc/systemd/system/
sudo systemctl daemon-reload
sudo systemctl enable --now taskflow-demo-reiniciar.timer
systemctl list-timers taskflow-demo-reiniciar.timer
```

(Si el contenedor corre con Podman sin root, instalar las unidades en
`~/.config/systemd/user/` y usar `systemctl --user`.)

> **Cuidado en desarrollo:** `Program.cs` carga el `.env` de la carpeta actual
> con DotNetEnv y ese archivo **reemplaza** las variables de entorno. Correr
> `demo-reiniciar` desde la carpeta de la API con `Demo__Activo=true` usaría la
> `DB_CONNECTION_STRING` del `.env` local. No correrlo nunca contra una base que
> no sea la de la demo.

## Pruebas

```bash
deploy/demo/probar-demo.sh https://taskflow-demo.nodesv.com   # o http://127.0.0.1:8080
```

Verifica `/salud`, el front con `noindex`, OpenAPI apagado, que entren las tres
cuentas, las cuentas protegidas, el largo de textos, el tope de tareas y el rate
limiting del login. **La prueba del tope llena la demo hasta 200 tareas**;
después, reiniciarla con `demo-reiniciar`.
