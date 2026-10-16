# JsonToCsv

Convierte archivos **JSON → CSV** (UTF-8 con BOM, RFC 4180) con **.NET 10** y **Blazor WebAssembly**,
en dos soluciones independientes. Licencia **MIT**; sin librerías de licencia distinta (ver `THIRD-PARTY-LICENSES.md`).

```
backend/    JsonToCsv.Backend.sln   Web API, arquitectura cebolla (Domain / Application / Infrastructure / WebApi) + pruebas
frontend/   JsonToCsv.Frontend.sln  Cliente Blazor WebAssembly
samples/    JSON de ejemplo y su salida esperada
```

## Requisitos
SDK de .NET 10 (`dotnet --version` ≥ 10.0).

## Ejecutar
```bash
# 1) Backend  -> http://localhost:5100
cd backend/src/4.Presentation/JsonToCsv.WebApi && dotnet run --launch-profile http

# 2) Frontend -> http://localhost:5200   (otra terminal)
cd frontend/src/JsonToCsv.Client && dotnet run
```
Abra http://localhost:5200, elija un `.json`, el delimitador (coma, punto y coma, tabulación) y pulse **Convertir y descargar CSV**.

- URL del backend usada por el frontend: `frontend/src/JsonToCsv.Client/wwwroot/appsettings.json` (`ApiBaseUrl`).
- Orígenes CORS permitidos en el backend: clave `Cors:AllowedOrigins` (por defecto `http://localhost:5200` y `https://localhost:7200`).

## Probar
```bash
cd backend
dotnet build JsonToCsv.Backend.sln -c Release
dotnet run --project tests/JsonToCsv.Tests -c Release      # 19 pruebas, sin dependencias externas
```
Prueba manual de la API:
```bash
curl -F "file=@samples/ejemplo.json" --form-string "delimiter=;" http://localhost:5100/api/converter/transform -o salida.csv
```
(En `curl -F`, el `;` separa opciones; por eso se usa `--form-string` para el delimitador.)

## Reglas de conversión
- Objetos anidados → `padre.hijo`; arreglos → `campo[0]`, `campo[1]`.
- Encabezados = unión de todas las llaves, en orden de primera aparición; celdas faltantes quedan vacías.
- `null` → vacío; booleanos → `true`/`false`; números se conservan tal cual.
- Raíz objeto = 1 fila; raíz arreglo = 1 fila por elemento; elementos escalares → columna `value`.
- Campos con delimitador, comillas o saltos de línea se entrecomillan (comillas duplicadas); fin de línea CRLF.
- Límite de archivo: 15 MB (frontend) / 20 MB (backend).

## API
`POST /api/converter/transform` (multipart/form-data): `file` (.json) y `delimiter` (`,` `;` `tab`).
Responde `200 text/csv` con el archivo, o `400 {"error": "..."}`. `GET /health` → `{"status":"ok"}`.

## Integración continua (opcional)
`ci/github-actions-ci.yml` compila backend (+ pruebas) y frontend. Para activarlo cópielo a `.github/workflows/ci.yml`
(requiere un token/credencial con permiso `workflow`, o hacerlo desde la web de GitHub).
