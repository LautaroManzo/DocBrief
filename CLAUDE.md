# DocBrief API

## Qué hace este proyecto
API REST en .NET que resume PDFs, Word (.docx), texto plano y paginas web (por URL)
usando IA via Semantic Kernel, con un frontend en React. El usuario elige el tipo de
resumen (`SummaryMode`: "basico" o "estudio") e idioma de salida (es/en). No persiste
nada — cada request genera el resumen y lo devuelve, sin historial.

Modos de resumen (ver SemanticKernelSummaryService, dos prompts distintos):
- **Básico**: sintesis clara y concisa en 1-2 parrafos.
- **Plan de estudio**: material de estudio extenso que aplica tecnicas de aprendizaje
  (chunking en secciones, elaboracion con ejemplos, glosario de "Terminos clave" y
  active recall con "Preguntas de repaso" + respuestas).

El repo/proyecto backend se llama **DocBrief**, pero el frontend se presenta al
usuario como **"Te lo resumo"** (titulo de la pestana y branding visible).

## Arquitectura
Backend: Clean Architecture (Domain / Application / Infrastructure / API).
Las dependencias apuntan hacia adentro. Infrastructure nunca en Domain.
Frontend: proyecto React separado en `web/`, fuera de `src/` porque es otro
ecosistema (Node/npm) — consume la API via HTTP.

## Convenciones
- Toda lógica de negocio va en Application/UseCases
- Los handlers usan MediatR (IRequestHandler)
- Las interfaces de servicios externos van en Application/Interfaces
- Los parsers de documentos se resuelven por extensión via IDocumentParserResolver
- Nombres en inglés, comentarios en español
- Commits en español, sin Co-Authored-By

## Stack
### Backend
- .NET 10, C# 13
- Semantic Kernel para IA — proveedor configurable via `AI:Provider` en appsettings
  ("Ollama" para desarrollo local con llama3.2, "Gemini" con gemini-flash-lite-latest)
- PdfPig para parsear PDFs, DocumentFormat.OpenXml para Word
- HtmlAgilityPack para extraer texto de paginas web (IUrlContentFetcher).
  Proteccion SSRF real: SsrfSafeHttpClientHandler valida la IP en el
  ConnectCallback (momento exacto de conectar), no antes — asi cubre tanto
  redirects (3xx a IPs internas) como DNS rebinding, no solo la URL inicial
- Limite de texto pegado: 10.000 caracteres (validado en front y back)
- Errores de parseo (PDF/DOCX corruptos) devuelven 400 con mensaje claro,
  no 500 con stack trace
- MediatR para CQRS
- Swashbuckle (Swagger) para documentación interactiva — habilitado siempre,
  incluso en producción (link "API docs" visible en el frontend)
- Rate limiting nativo de .NET: 10 requests/minuto + 10/dia por IP (en memoria,
  se resetea si la API reinicia)
- CORS: en desarrollo acepta cualquier origen `localhost` (Vite cambia de puerto);
  en produccion exige `Cors:AllowedOrigin` configurado
- Sin base de datos

### Frontend (web/)
- React + Vite + TypeScript
- Sin librerías de UI — componentes y CSS propios (paleta oklch, fuentes Nunito/Inter)
- jsPDF para exportar el resumen a PDF con formato (títulos, listas, negritas)
- Dark/light mode con toggle propio (localStorage)
- Layout responsive (mobile-first en breakpoints clave) — pendiente pulir
  detalles visuales en mobile, reportados como poco prolijos

## Estructura
```
src/
  DocBrief.Domain/          → Vacío por ahora (sin entidades persistentes)
  DocBrief.Application/     → Interfaces, UseCases, Commands/Handlers
  DocBrief.Infrastructure/  → Parsers (PdfPig, OpenXml), Web (UrlContentFetcher),
                               IA (Semantic Kernel)
  DocBrief.API/              → Controllers, Program.cs, Swagger
tests/
  DocBrief.TestConsole/     → Test manual de parsers
web/
  src/components/           → IdleView, ProcessingView, DoneView, ErrorView
                               + Select, SummaryContent, ThemeToggle, ApiDocsLink
  src/services/api.ts       → Llamadas HTTP a la API
  src/utils/                → markdown.ts (parser compartido), download.ts (jsPDF)
```

## Comandos útiles
### Backend
```
dotnet build                              # compilar
dotnet run --project src/DocBrief.API     # levantar la API (Swagger en /swagger)
dotnet test                               # tests
```

### Frontend
```
cd web
npm run dev                               # levantar en localhost:5173
```

## Ollama (desarrollo local)
```
ollama serve          # si no esta corriendo como servicio
ollama list            # verificar que llama3.2 este disponible
```

## Deploy (produccion)
- **Backend**: Render (Docker) — https://te-lo-resumo.onrender.com
  - `Dockerfile` en la raiz del repo, build multi-stage
  - Escucha en el puerto de la variable `PORT` si esta seteada
  - Variables de entorno en Render: `AI__Provider=Gemini`,
    `Gemini__ApiKey=<key>`, `Cors__AllowedOrigin=https://te-lo-resumo.vercel.app`
  - Free tier: el servicio "duerme" tras inactividad, la primera
    request tras un rato inactivo tarda ~30-50s en responder
- **Frontend**: Vercel — https://te-lo-resumo.vercel.app
  - Root Directory: `web`
  - Variable de entorno: `VITE_API_URL=https://te-lo-resumo.onrender.com`
