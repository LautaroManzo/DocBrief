# DocBrief API

## Qué hace este proyecto
API REST en .NET que resume PDFs y texto plano usando IA via Semantic Kernel.
No persiste nada — cada request genera el resumen y lo devuelve, sin historial.

## Arquitectura
Clean Architecture: Domain / Application / Infrastructure / API.
Las dependencias apuntan hacia adentro. Infrastructure nunca en Domain.

## Convenciones
- Toda lógica de negocio va en Application/UseCases
- Los handlers usan MediatR (IRequestHandler)
- Las interfaces de servicios externos van en Application/Interfaces
- Nombres en inglés, comentarios en español
- Commits en español, sin Co-Authored-By

## Stack
- .NET 10, C# 13
- Semantic Kernel para IA (Ollama local en desarrollo, Gemini en producción)
- PdfPig para parsear PDFs
- MediatR para CQRS
- Sin base de datos

## Estructura
```
src/
  DocBrief.Domain/          → Vacío por ahora (sin entidades persistentes)
  DocBrief.Application/     → Interfaces, UseCases, Commands/Handlers
  DocBrief.Infrastructure/  → Parsers (PdfPig), IA (Semantic Kernel)
  DocBrief.API/             → Controllers, Program.cs
tests/
  DocBrief.TestConsole/     → Test manual de parsers
```

## Comandos útiles
```
dotnet build                              # compilar
dotnet run --project src/DocBrief.API     # levantar la API
dotnet test                               # tests
```

## Ollama (desarrollo local)
```
ollama serve          # si no esta corriendo como servicio
ollama list            # verificar que llama3.2 este disponible
```
