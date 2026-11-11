# Makers Loans — prueba técnica full stack y datos

Adaptación del desafío de préstamos bancarios a **.NET, Angular y Python**, orientada a una vacante full stack con énfasis en datos. Incluye autenticación JWT, autorización por rol, solicitudes y decisiones, PostgreSQL, caché, pruebas y un flujo analítico reproducible.

## Tecnologías

- .NET 10, ASP.NET Core, Entity Framework Core y xUnit
- PostgreSQL 18
- Angular 21 con formularios reactivos, guards y signals
- Python 3.13, pandas, matplotlib y pytest
- Docker Compose para ejecución local

## Inicio rápido con Docker

Requiere Docker Desktop con el motor Linux activo.

```powershell
Copy-Item .env.example .env
# Cambia los valores de .env
docker compose up --build
```

Abre `http://localhost:4200`. El seed crea datos sintéticos y estas cuentas:

- Usuario: `usuario@test.com`
- Administrador: `admin@test.com`
- Contraseña: el valor de `SEED_PASSWORD` (`Demo123456!` si se usan los valores locales predeterminados)

Para detener los servicios usa `docker compose down`. Agregar `-v` elimina también el volumen local de PostgreSQL.

## Inicio sin Docker

Requiere .NET SDK 10, Node 22 y PostgreSQL. Ajusta `ConnectionStrings__Database`, `Jwt__Key` y `Seed__Password` en variables de entorno.

```powershell
dotnet run --project src/backend/Makers.Api
Set-Location src/frontend
npm ci
npm start
```

La API queda en `http://localhost:5080` y Angular en `http://localhost:4200`. Las migraciones se aplican al arrancar.

## API principal

| Método | Ruta | Acceso |
|---|---|---|
| POST | `/api/auth/login` | Público |
| POST | `/api/auth/register` | Público |
| GET/POST | `/api/loans` | Usuario o administrador |
| GET/PUT/DELETE | `/api/loans/{id}` | Propietario o administrador |
| PATCH | `/api/loans/{id}/decision` | Administrador |
| CRUD | `/api/users` | Administrador |
| GET | `/api/analytics/loans` | Administrador |

Listados usan `page`, `size` y, para préstamos, `status`. Los errores siguen `ProblemDetails` con códigos HTTP claros.

## Análisis con Python

El repositorio incluye un snapshot sintético para ejecutar el análisis sin levantar la API:

```powershell
py -m pip install -e "analytics[test]"
py -m loan_analytics.cli --input analytics/examples/loans.json --output artifacts/analytics
```

Para extraer los datos actuales de la API:

```powershell
py analytics/extract.py --password "TU_SEED_PASSWORD" --output analytics/data/loans.json
py -m loan_analytics.cli --input analytics/data/loans.json --output artifacts/analytics
```

Se generan `metrics.csv`, `monthly_summary.csv`, `portfolio_overview.png` y `report.md`. El export no incluye correos, nombres ni contraseñas.

## Pruebas y validación

```powershell
dotnet test Makers.slnx
Set-Location src/frontend
npm run build
Set-Location ../..
py -m pytest analytics/tests
docker compose config
```

## Documentación

- [Respuestas teóricas](docs/RESPUESTAS_TEORICAS.md)
- [Arquitectura y defensa](docs/ARQUITECTURA.md)
- Enunciado original: `Prueba Técnica para Desarrolladores-SpringBoot Final.pdf`

## Limitaciones conocidas

La caché es por instancia y está pensada para la demostración. El almacenamiento del JWT en `sessionStorage` se reemplazaría por cookies seguras y un backend for frontend en un sistema financiero real. Los datos son sintéticos y no permiten evaluar mora, capacidad de pago o rentabilidad.
