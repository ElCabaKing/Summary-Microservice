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
JWT_SECRET=supersecreto-jwt-para-firmar-tokens-2024
AES_KEY=5e884898da28047151d0e56f8dc62927
SQL_CONNECTION_STRING=Server=localhost,1433;Database=SummaryService;User Id=sa;Password=SummarySrv2024!;TrustServerCertificate=True
```

| Variable | Descripción |
|---|---|
| `GROQ_API_KEY` | API key de Groq (fallback global) |
| `GEMINI_API_KEY` | API key de Gemini (fallback global) |
| `JWT_SECRET` | Clave secreta para firmar/validar JWTs |
| `AES_KEY` | Hex string de 32 caracteres (16 bytes) para encriptar API keys en BD |
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

El script SQL está en `src/SummaryService.Infrastructure/Persistence/Scripts/init.sql`.

Podés conectarte con cualquier cliente SQL (Azure Data Studio, SSMS, `sqlcmd`) o directamente con Docker:

```bash
docker exec -i summary-sqlserver /opt/mssql-tools18/bin/sqlcmd \
  -S localhost -U sa -P "SummarySrv2024!" -C \
  -d master \
  -i /scripts/init.sql
```

> El contenedor monta la carpeta `./src/SummaryService.Infrastructure/Persistence/Scripts` en `/scripts/`.

Esto crea la base `SummaryService` y la tabla `TenantProviders`.

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
| `POST` | `/api/v1/summaries/stream` | ✅ JWT | Subir PDF/TXT y recibir resumen via SSE |
| `POST` | `/api/v1/admin/tenants/{tenantId}/providers` | ✅ JWT + admin | Configurar provider IA para un tenant |

---

## 5. Generar un JWT de prueba

El microservicio valida el JWT pero no lo emite. Necesitás generarlo externamente.

### Opción A: Con `jose` (Node.js)

```bash
npx jose new-sign \
  --key "supersecreto-jwt-para-firmar-tokens-2024" \
  --alg HS256 \
  --payload '{"tenant_id":"tenant-alpha","role":"admin","sub":"test-user"}' \
  --exp 24h
```

### Opción B: Con Python

```python
import jwt, datetime

payload = {
    "tenant_id": "tenant-alpha",
    "role": "admin",
    "sub": "test-user",
    "exp": datetime.datetime.utcnow() + datetime.timedelta(hours=24)
}
token = jwt.encode(payload, "supersecreto-jwt-para-firmar-tokens-2024", algorithm="HS256")
print(token)
```

### Opción C: Con jwt.io

1. Andá a [jwt.io](https://jwt.io)
2. En **Payload** pegá:

```json
{
  "tenant_id": "tenant-alpha",
  "role": "admin",
  "sub": "test-user"
}
```

3. En **VERIFY SIGNATURE** seleccioná algoritmo `HS256` y pegá `supersecreto-jwt-para-firmar-tokens-2024` como secret
4. Copiá el token generado

---

## 6. Usar la interfaz web

Abrí `stream.html` en el navegador (no necesita servidor, funciona directo desde el explorador de archivos).

### Pasos:

1. **Pegá el JWT** en el campo de texto y presioná _Guardar Token_
2. Verificá que aparezcan los badges **Tenant** y **Role**
3. _(opcional, solo admin)_ Expandí la sección **Configurar Provider** y guardá una API key para tu tenant
4. **Arrastrá o seleccioná** un PDF/TXT
5. Elegí **Provider**, **Modelo**, **Temperatura** y **Max Tokens**
6. Presioná **Enviar** y observá el resumen en vivo via SSE

> Si el JWT expiró, el botón _Enviar_ se deshabilita. Generá uno nuevo y guardalo.

---

## 7. Probar con curl (sin interfaz web)

### Configurar provider (admin):

```bash
JWT="eyJhbGciOiJIUzI1NiIs..."

curl -X POST http://localhost:5171/api/v1/admin/tenants/tenant-alpha/providers \
  -H "Authorization: Bearer $JWT" \
  -H "Content-Type: application/json" \
  -d '{
    "provider": "groq",
    "model": "llama-3.3-70b-versatile",
    "apiKey": "gsk_tu_key_aqui"
  }'
```

### Resumir un documento:

```bash
curl -X POST http://localhost:5171/api/v1/summaries/stream \
  -H "Authorization: Bearer $JWT" \
  -F "document=@/ruta/al/documento.pdf" \
  -F "provider=groq" \
  -F "model=llama-3.3-70b-versatile"
```

---

## 8. Arquitectura

```
stream.html  (cliente SSE)
     │ POST con JWT en Authorization header
     ▼
SummaryService.Api
  ├── Middleware/ExceptionMiddleware.cs
  ├── Middleware/HttpTenantContext.cs     ← extrae tenant_id del JWT
  ├── Endpoints/SummaryEndpoints.cs      ← /api/v1/summaries/stream
  ├── Endpoints/AdminEndpoints.cs        ← admin/tenants/{id}/providers
  │
  ├── SummaryService.Application
  │   ├── UseCases/
  │   │   ├── SummarizeDocumentUseCase   ← resuelve API key del tenant
  │   │   └── ConfigureTenantProviderUseCase
  │   └── Interfaces/
  │       ├── ITenantContext
  │       ├── ITenantProviderRepository
  │       └── IAesEncryptionService
  │
  ├── SummaryService.Infrastructure
  │   ├── Persistence/TenantProviderRepository.cs  ← Dapper + SQL Server
  │   ├── Encryption/AesEncryptionService.cs
  │   └── Factory/KernelFactory.cs       ← crea Kernel con API key del tenant
  │
  └── SummaryService.Domain
      └── Entities/TenantProvider.cs
```

### Flujo de datos

```
JWT → AddJwtBearer valida firma → HttpTenantContext (tenant_id + role)
→ SummarizeDocumentUseCase
  → ITenantProviderRepository.GetActiveProviderAsync(tenant_id, provider)
    → SQL Server: SELECT ... FROM TenantProviders WHERE ...
  → IAesEncryptionService.Decrypt(encryptedApiKey)
  → KernelFactory.Create(provider, model, decryptedApiKey)
    → Kernel con API key del tenant → LLM → tokens → SSE → stream.html
```
