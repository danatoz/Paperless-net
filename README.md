# Paperless (.NET)

> Port of [paperless-ngx](https://docs.paperless-ngx.com/) — an open-source document management system — from Python/Django to C# (.NET 10).

## CI Status

[![.NET Build & Test](https://github.com/danatoz/Paperless-net/actions/workflows/dotnet-build-test.yml/badge.svg)](https://github.com/danatoz/Paperless-net/actions/workflows/dotnet-build-test.yml)

## Solution Structure

```
Paperless.slnx
├── src/
│   ├── Paperless.Shared/         # Common abstractions, Result/Error pattern, event contracts
│   ├── Paperless.Core/           # Domain entities, interfaces, business logic (pure C#)
│   ├── Paperless.Infrastructure/ # EF Core, Lucene.NET, Hangfire, file storage
│   ├── Paperless.Api/            # ASP.NET Core Web API + SignalR
│   ├── Paperless.Mail/           # IMAP mail fetching (MailKit)
│   └── Paperless.AI/             # AI classification, RAG chat, embeddings
└── tests/
    ├── Paperless.Core.Tests/
    ├── Paperless.Infrastructure.Tests/
    ├── Paperless.Api.Tests/
    ├── Paperless.Mail.Tests/
    └── Paperless.AI.Tests/
```

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- (Optional) Docker for infrastructure services (PostgreSQL, Redis, Gotenberg, Tika)

## Build & Test

```bash
dotnet restore Paperless.slnx
dotnet build Paperless.slnx -c Release
dotnet test Paperless.slnx -c Release
```
