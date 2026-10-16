# Dependencias y licencias

| Componente | Origen | Licencia |
|---|---|---|
| .NET 10 (runtime, ASP.NET Core, System.Text.Json) | Microsoft | MIT |
| Blazor WebAssembly (`Microsoft.AspNetCore.Components.WebAssembly` y `.DevServer`) | Microsoft | MIT |
| Bootstrap (incluido en `wwwroot/lib` por la plantilla de Blazor) | Bootstrap authors | MIT |
| Escritor CSV (RFC 4180) | Implementación propia en este repo | MIT (este repo) |

**No se usa CsvHelper.** El documento de especificación lo proponía como "MIT", pero CsvHelper se
distribuye bajo doble licencia **MS-PL / Apache-2.0**, no MIT. Para cumplir el requisito de
licenciamiento se reemplazó por un escritor propio (`CsvWriterRfc4180`).

El backend no referencia ningún paquete NuGet; el frontend solo los dos paquetes de Blazor de Microsoft.
