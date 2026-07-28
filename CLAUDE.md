# DocBrief API

## Qué hace este proyecto
API REST en .NET que resume PDFs, Word (.docx), texto plano, paginas web y videos de
YouTube (por URL) usando IA via Semantic Kernel, con un frontend en React (wizard de
3 pasos). El usuario elige el tipo de resumen (`SummaryMode`: "basico" o "estudio") e
idioma de salida (es/en/pt, default es). Sin persistencia — cada request genera el resumen y lo
devuelve, sin historial.

Modos de resumen (ver SemanticKernelSummaryService):
- **Básico**: sintesis de 1-2 parrafos, con titulo al inicio.
- **Plan de estudio**: material extenso (chunking, elaboracion, "Terminos clave",
  "Preguntas de repaso"). Incluye siempre un **mapa conceptual** (mermaid mindmap).
- Los prompts piden no inventar grafias plausibles para nombres/terminos mal
  transcriptos (comun en audio/video) — marcarlos como "[verificar: NOMBRE]".

El repo se llama **DocBrief**, el frontend se presenta como **"Te lo resumo"**.

## Arquitectura
Clean Architecture (Domain / Application / Infrastructure / API), dependencias
hacia adentro. Frontend React separado en `web/` (otro ecosistema, Node/npm),
consume la API via HTTP.

Solucion `DocBrief.slnx` en la raiz con todos los proyectos (`src/`, `tests/`,
`web/web.esproj`) para abrir todo junto en Visual Studio.

## Convenciones
- Toda lógica de negocio va en Application/UseCases
- Los handlers usan MediatR (IRequestHandler)
- Las interfaces de servicios externos van en Application/Interfaces
- Nombres en inglés, comentarios en español
- Commits en español, sin Co-Authored-By

## Stack
### Backend
- .NET 10, C# 13, MediatR para CQRS
- Semantic Kernel — proveedor via `AI:Provider` ("Ollama" local con llama3.2,
  "Gemini" con gemini-flash-lite-latest en produccion)
- PdfPig (PDF), DocumentFormat.OpenXml (Word), HtmlAgilityPack (paginas web,
  texto + `<title>`)
- SSRF: `SsrfSafeHttpClientHandler` valida la IP en el `ConnectCallback` (al
  conectar, no antes) — cubre redirects y DNS rebinding
- YouTube: metodo principal es la API de **Supadata** (`Supadata:ApiKey`) —
  YouTube bloquea el scraping directo por IP de datacenter, no arreglable de
  nuestro lado. Sin la key cae a YoutubeExplode (sirve en local). Titulo del
  video via oEmbed
- `app.UseForwardedHeaders()`: necesario para que el rate limiter (por IP) vea
  la IP real del cliente detras del proxy de Render, no la del proxy
  para todos
- Limite de texto pegado: 10.000 caracteres (front y back)
- Rate limiting nativo: 10/min + 10/dia por IP, en memoria
- CORS: cualquier `localhost` en dev, `Cors:AllowedOrigin` exacto en produccion
- Swagger siempre habilitado (tambien en produccion), sin base de datos

### Frontend (web/)
- React + Vite + TypeScript, sin librerías de UI (CSS propio)
- **Wizard de 3 pasos**: step1 origen+opciones → step2 cargar contenido → step3
  procesando/resultado/error (`App.tsx`, `Phase` en `types.ts`). El submit real
  se dispara al confirmar en step2, no al elegir el archivo
- Paleta oklch (indigo/violeta) + Nunito/Inter, tokens en `index.css`
- **Mapa conceptual**: se renderiza con `mermaid` (`ConceptMapSection.tsx`); si
  viene vacio o invalido, no se muestra nada (ni el titulo), no un error
- PDF (`utils/download.ts`, jsPDF): el mapa conceptual se rasteriza a PNG via
  canvas usando un data URI (blob URL rompe por los `<foreignObject>` de
  mermaid) y va en pagina propia
- `sourceTitle` (del backend) reemplaza la URL cruda en pantalla y nombra el PDF
- Dark/light mode (localStorage), animaciones CSS sutiles (respetan
  `prefers-reduced-motion`), responsive mobile-first

## Estructura
```
DocBrief.slnx
src/
  DocBrief.Domain/           → Vacío (sin entidades persistentes)
  DocBrief.Application/      → Interfaces, UseCases
  DocBrief.Infrastructure/   → Parsers, Web (fetchers), IA (Semantic Kernel)
  DocBrief.API/               → Controllers, Program.cs, Swagger
tests/
  DocBrief.TestConsole/           → Consola manual (parsers)
  DocBrief.Application.Tests/     → xUnit + Moq (SummarizeDocumentHandler)
  DocBrief.Infrastructure.Tests/  → xUnit (SSRF, deteccion de URLs de YouTube)
web/
  src/components/            → WizardHeader, StepSource, StepContent,
                                ProcessingView, DoneView, ErrorView,
                                SummaryContent, ConceptMapSection, etc.
  src/services/api.ts        → Llamadas HTTP
  src/utils/                 → markdown.ts, download.ts, mermaidRender.ts
```

## Comandos útiles
```
dotnet build DocBrief.slnx                # compilar todo
dotnet run --project src/DocBrief.API     # levantar la API (Swagger en /swagger)
dotnet test DocBrief.slnx                 # correr los tests

cd web && npm run dev                     # frontend en localhost:5173
```

## Ollama (desarrollo local)
```
ollama serve
ollama list   # verificar que llama3.2 este disponible
```

## Deploy (produccion)
- **Backend**: Render (Docker) — https://te-lo-resumo.onrender.com
  - Variables: `AI__Provider=Gemini`, `Gemini__ApiKey`, `Cors__AllowedOrigin`,
    `Supadata__ApiKey`
  - Free tier: se duerme tras inactividad, primera request ~30-50s
- **Frontend**: Vercel — https://te-lo-resumo.vercel.app
  - Root Directory: `web`, variable `VITE_API_URL`

## Limitaciones conocidas
- YouTube: Supadata resuelve la mayoria de los casos pero no el 100% (depende
  de que YouTube exponga subtitulos). Sin esa key, YoutubeExplode puede fallar
  incluso desde IP residencial para algunos videos puntuales.
