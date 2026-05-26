# Setup — Summary Microservice

## Requisitos

- [Docker](https://docs.docker.com/get-docker/) (para SQL Server)
- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- Clon del repositorio

---

## 1. Archivo `.env`

Ubicado en la raíz del proyecto. Ya incluye valores por defecto pero podés modificarlos:

```bash
GROQ_API_KEY=gsk_tu_key_aqui
GEMINI_API_KEY=AIza_tu_key_aqui
SQL_CONNECTION_STRING=Server=localhost,1433;Database=SummaryService;User Id=sa;Password=SummarySrv2024!;TrustServerCertificate=True
```

| Variable | Descripción |
|---|---|
| `GROQ_API_KEY` | API key de Groq |
| `GEMINI_API_KEY` | API key de Gemini |
| `SQL_CONNECTION_STRING` | Connection string a SQL Server |

> ⚠️ El `.env` está en `.gitignore` y `.dockerignore` — no se sube al repo.

---

## 2. Levantar SQL Server

```bash
docker compose up -d
```

Esto levanta un contenedor `mcr.microsoft.com/mssql/server:2022-latest` en `localhost:1433`.

Para verificar que está listo:

```bash
docker compose ps
# Name: summary-sqlserver   State: healthy
```

---

## 3. Inicializar la base de datos

El script SQL está en `Scripts/init.sql`.

Podés conectarte con cualquier cliente SQL o directamente con Docker:

```bash
docker exec -i summary-sqlserver /opt/mssql-tools18/bin/sqlcmd \
  -S localhost -U sa -P "SummarySrv2024!" -C \
  -d master \
  -i /scripts/init.sql
```

> El contenedor monta la carpeta `./Scripts` en `/scripts/` si configuraste el volumen en `docker-compose.yml`.

Esto crea:
- Base `SummaryService`
- Tabla `Clients` (empresas registradas)
- Tabla `ApiKeys` (hashes de API keys)
- **Admin key** de bootstrap: `smm_adminadmin8c0013772288e07a80a60b5775e2304840376e93038336325feb5c79af0e67c7`

---

## 4. Ejecutar el microservicio

```bash
dotnet run --project src/SummaryService.Api
```

El servicio arranca en `http://localhost:5171`.

Endpoints:

| Método | Ruta | Auth | Descripción |
|---|---|---|---|
| `GET` | `/health` | ❌ | Health check |
| `POST` | `/api/v1/summaries/stream` | ✅ API Key | Subir PDF/TXT y recibir resumen via SSE |
| `POST` | `/api/v1/admin/apikeys` | ✅ Admin key | Registrar un cliente y generar API key |
| `PUT` | `/api/v1/admin/apikeys/regenerate` | ✅ Admin key | Regenerar API key de un tenant |

---

## 5. Autenticación con API Keys

No se usan JWTs. Cada request autenticado debe incluir:

```
Authorization: Bearer <api-key>
```

El formato de las API keys es:

```
smm_ + 10-char-prefix + 64-hex-chars (256 bits)
Ejemplo: smm_adminadmin8c0013772288e07a80a60b5775e2304840376e93038336325feb5c79af0e67c7
```

### Admin key de bootstrap

La key de admin viene insertada por `init.sql`:

```
smm_adminadmin8c0013772288e07a80a60b5775e2304840376e93038336325feb5c79af0e67c7
```

Usala para registar clientes y administrar el sistema.

---

## 6. Registrar un cliente (admin)

```bash
KEY="smm_adminadmin8c0013772288e07a80a60b5775e2304840376e93038336325feb5c79af0e67c7"

curl -X POST http://localhost:5171/api/v1/admin/apikeys \
  -H "Authorization: Bearer $KEY" \
  -H "Content-Type: application/json" \
  -d '{
    "companyName": "Mi Empresa S.A.",
    "email": "admin@miempresa.com",
    "contactName": "Juan Pérez"
  }'
```

Respuesta:

```json
{
  "apiKey": "smm_a3b2c1d9e0...",
  "tenantId": "tenant_1",
  "companyName": "Mi Empresa S.A."
}
```

> La API key del cliente se muestra **una sola vez**. Guardala.

---

## 7. Usar la interfaz web

Abrí `stream.html` en el navegador (no necesita servidor, funciona directo desde el explorador de archivos).

### Pasos:

1. **API Key** — el campo viene precargado con la admin key de bootstrap
2. **Quick-select** — clickeá `adminadmin` o `tenant_1`, `tenant_2`... para cambiar de key
3. **Registrar cliente** — si estás usando la admin key, aparecerá el botón `📋 Registrar Cliente`
4. **Arrastrá o seleccioná** un PDF/TXT
5. Elegí **Provider**, **Modelo**, **Temperatura** y **Max Tokens**
6. Presioná **Enviar** y observá el resumen en vivo via SSE

> Las keys se guardan automáticamente en `localStorage`. Al registrar un cliente desde la UI, su key se agrega automáticamente a los botones de quick-select.

---

## 8. Probar con curl

### Resumir un documento:

```bash
KEY="smm_adminadmin8c0013772288e07a80a60b5775e2304840376e93038336325feb5c79af0e67c7"

curl -X POST http://localhost:5171/api/v1/summaries/stream \
  -H "Authorization: Bearer $KEY" \
  -F "document=@/ruta/al/documento.pdf" \
  -F "provider=groq" \
  -F "model=llama-3.3-70b-versatile"
```

---

## 9. Arquitectura

```
stream.html  (cliente SSE)
     │ POST con API Key en Authorization header
     ▼
SummaryService.Api
  ├── Middleware/ExceptionMiddleware.cs
  ├── Middleware/HttpTenantContext.cs       ← extrae tenant_id + role de la API Key
  ├── Authentication/ApiKeyAuthHandler.cs  ← busca por prefix, verifica SHA256
  ├── Endpoints/SummaryEndpoints.cs        ← /api/v1/summaries/stream
  ├── Endpoints/ApiKeyEndpoints.cs         ← /admin/apikeys, /admin/apikeys/regenerate
  │
  ├── SummaryService.Application
  │   ├── UseCases/
  │   │   ├── SummarizeDocumentUseCase     ← orquesta resumen con AI
  │   │   ├── RegisterClientUseCase        ← registra empresa + genera API key
  │   │   └── RegenerateApiKeyUseCase      ← regenera key de un tenant
  │   ├── Services/
  │   │   ├── Prompts.cs                   ← templates de prompts (static)
  │   │   └── TokenEstimator.cs            ← estimador de tokens (static)
  │   └── Interfaces/
  │       ├── IApiKeyHashService
  │       ├── IApiKeyRepository
  │       ├── IClientRepository
  │       └── IStreamingTextGenerator
  │
  ├── SummaryService.Infrastructure
  │   ├── Persistence/
  │   │   ├── BaseRepository.cs            ← base class con GetConnection()
  │   │   ├── ApiKeyRepository.cs          ← Dapper + SQL Server
  │   │   └── ClientRepository.cs          ← Dapper + SQL Server
  │   ├── Encryption/ApiKeyHashService.cs  ← SHA256 + generación de keys
  │   └── Factory/KernelFactory.cs         ← crea Kernel con API key del .env
  │
  └── SummaryService.Domain
      ├── Entities/ApiKey.cs
      └── Entities/Client.cs
```

### Flujo de datos

```
API Key → ApiKeyAuthHandler
  → extrae prefix (primeros 14 chars: smm_ + 10)
  → busca en DB por prefix → obtiene candidatos
  → verifica SHA256 hash contra cada candidato
  → si match → ClaimsPrincipal (tenant_id + role)

→ SummarizeDocumentUseCase
  → parsea documento (PDF/TXT)
  → chunkea texto
  → por cada chunk: llama al LLM via KernelFactory
    → KernelFactory usa API key global del .env (GROQ_API_KEY / GEMINI_API_KEY)
  → reduce summaries si hay múltiples chunks
  → emite tokens via SSE → stream.html
```
