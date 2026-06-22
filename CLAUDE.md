# DocBrief API

## Qué hace este proyecto
API REST en .NET que resume PDFs y texto plano usando Gemini via Semantic Kernel.
Guarda historial de resúmenes en PostgreSQL.

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
- Semantic Kernel para IA
- PdfPig para parsear PDFs
- EF Core + PostgreSQL
- MediatR para CQRS

## Estructura
```
src/
  DocBrief.Domain/          → Entidades (Document, Summary)
  DocBrief.Application/     → Interfaces, UseCases, Commands/Handlers
  DocBrief.Infrastructure/  → Parsers (PdfPig), IA (Semantic Kernel), DB (EF Core)
  DocBrief.API/             → Controllers, Program.cs
tests/
  DocBrief.TestConsole/     → Test manual de parsers
```

## Comandos útiles
```
dotnet build                              # compilar
dotnet run --project src/DocBrief.API     # levantar la API
dotnet ef migrations add NombreMigracion  # crear migracion
dotnet test                               # tests
```
