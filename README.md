# 📄 Te lo resumo

Resúmenes de documentos con IA. Subís un PDF, un Word, pegás texto, un link o un video de YouTube, y devuelve un resumen claro.

[Ver sitio web](https://te-lo-resumo.vercel.app) · [Documentación de la API](https://te-lo-resumo.onrender.com/swagger)

---

## 🚀 Tecnologías utilizadas

![.NET](https://img.shields.io/badge/.NET-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)
![C%23](https://img.shields.io/badge/C%23-239120?style=for-the-badge&logo=csharp&logoColor=white)
![React](https://img.shields.io/badge/react-%2320232a.svg?style=for-the-badge&logo=react&logoColor=%2361DAFB)
![TypeScript](https://img.shields.io/badge/typescript-%23007ACC.svg?style=for-the-badge&logo=typescript&logoColor=white)
![Vite](https://img.shields.io/badge/vite-%23646CFF.svg?style=for-the-badge&logo=vite&logoColor=white)
![Gemini](https://img.shields.io/badge/gemini-8E75B2?style=for-the-badge&logo=googlegemini&logoColor=white)
![Render](https://img.shields.io/badge/render-%2346E3B7.svg?style=for-the-badge&logo=render&logoColor=white)
![Vercel](https://img.shields.io/badge/vercel-%23000000.svg?style=for-the-badge&logo=vercel&logoColor=white)

### 🛠️ Otras dependencias

- **Semantic Kernel** — orquesta la IA (Gemini en producción, Ollama/llama3.2 en desarrollo local)
- **MediatR** — patrón CQRS en el backend
- **PdfPig** / **DocumentFormat.OpenXml** — extracción de texto de PDF y Word
- **HtmlAgilityPack** — extracción de texto y título de páginas web
- **mermaid** — renderizado del mapa conceptual (diagrama tipo mindmap)
- **jsPDF** — exportación del resumen a PDF, incluyendo el mapa conceptual
- **xUnit + Moq** — tests unitarios del backend

---

## 🧠 Modos de resumen

- **Básico** — síntesis en 1-2 párrafos.
- **Plan de estudio** — chunking, elaboración con ejemplos, glosario de términos clave, preguntas de repaso y mapa conceptual (mermaid).

---

## 🔒 Seguridad

- Protección contra SSRF (evita que un link malicioso obligue al servidor a acceder a IPs internas propias, en vez de a una página web real)
- Rate limiting: 10 requests/minuto + 10/día por IP

---

## 📡 APIs utilizadas

| API | Uso | Sitio |
|-----|-----|-------|
| **Google Gemini** | Generación de los resúmenes | [ai.google.dev](https://ai.google.dev) |
| **Supadata** | Transcripción de videos de YouTube (evita el bloqueo anti-bot de IPs de datacenter) | [supadata.ai](https://supadata.ai) |
| **YouTube oEmbed** | Título del video para mostrar en la UI y nombrar el PDF | [oembed.com](https://oembed.com) |

---

## 🏗️ Arquitectura

Clean Architecture (Domain / Application / Infrastructure / API).

```
src/
  DocBrief.Domain/          → Entidades (vacío)
  DocBrief.Application/     → Casos de uso, interfaces (MediatR)
  DocBrief.Infrastructure/  → Parsers, fetchers, integración con Semantic Kernel
  DocBrief.API/              → Controllers, Swagger
web/                        → Frontend React (wizard de 3 pasos)
```

---

## 🌐 Despliegue

Backend en **Render** (Docker) y frontend en **Vercel**.
