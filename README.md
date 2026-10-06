<div align="center">

# KnowledgeAssistant

### AI-powered knowledge assistant built with .NET 10, Blazor, SQL Server and RAG

<p>
  <img src="https://img.shields.io/badge/.NET-10-512BD4?style=flat-square&logo=dotnet&logoColor=white" alt=".NET 10">
  <img src="https://img.shields.io/badge/Blazor-Server-512BD4?style=flat-square&logo=blazor&logoColor=white" alt="Blazor">
  <img src="https://img.shields.io/badge/SQL%20Server-CC2927?style=flat-square&logo=microsoftsqlserver&logoColor=white" alt="SQL Server">
  <img src="https://img.shields.io/badge/OpenAI-412991?style=flat-square&logo=openai&logoColor=white" alt="OpenAI">
  <img src="https://img.shields.io/badge/RAG-194A8C?style=flat-square" alt="RAG">
  <img src="https://img.shields.io/badge/xUnit-512BD4?style=flat-square" alt="xUnit">
</p>

<p>
  A practical full-stack .NET application exploring AI integration, document retrieval,
  authentication, asynchronous processing and modern application architecture.
</p>

</div>

---

## Contents

|        | Section                                 |
| :----: | --------------------------------------- |
| **01** | [Overview](#01--overview)               |
| **02** | [Technology Used](#02--technology-used) |
| **03** | [File Structure](#03--file-structure)   |
| **04** | [Screenshots](#04--screenshots)         |
| **05** | [Features](#05--features)               |
| **06** | [Trade Offs](#06--trade-offs)           |

---

## 01 · Overview

KnowledgeAssistant is a full-stack application built to explore how modern .NET applications can integrate AI with a private knowledge base.

Users can upload documents, process them into searchable chunks and ask questions through an AI chat interface. Relevant document content is retrieved and provided to the AI model as context before generating a response.

The project also includes authentication, role-based authorisation, document management, chat history, background processing and automated testing.

The main goal is to demonstrate practical **.NET + AI development** rather than build a large production platform.

### What it demonstrates

| Area           | Implementation                   |
| -------------- | -------------------------------- |
| Application    | ASP.NET Core / Blazor            |
| AI             | OpenAI + Microsoft.Extensions.AI |
| RAG            | Embeddings + similarity search   |
| Database       | SQL Server + EF Core             |
| Messaging      | RabbitMQ                         |
| Authentication | JWT + role-based authorisation   |
| UI             | Blazor + MudBlazor               |
| Testing        | xUnit + bUnit (UI)               |

---

## 02 · Technology Used

### Backend

* **C#**
* **.NET 10**
* **ASP.NET Core REST Web API**
* **Entity Framework Core**
* **SQL Server**
* **JWT Authentication**
* **Role-based Authorisation**

### Frontend

* **Blazor Web App**
* **MudBlazor**
* **HTML / CSS**
* **JavaScript**

### AI

* **Microsoft.Extensions.AI**
* **IChatClient**
* **OpenAI**
* **Chat models**
* **Embedding models**
* **Vector embeddings**
* **Retrieval-Augmented Generation**

### Messaging & Background Processing

* **RabbitMQ**
* **ASP.NET Core Background Services**
* **Queued email processing**
* **Queued notifications processing**
* **Asynchronous notifications**

### Testing & Development

* **xUnit**
* **bUnit**
* **Git**
* **GitHub**
* **Scalar**
* **Visual Studio**
* **GitHub Copilot**
* **Claude**

---

## 03 · File Structure

The solution is currently split into four projects:

```text
KnowledgeAssistant
│
├── KnowledgeAssistant.API
│   ├── Controllers
│   ├── Middleware
│   └── Program.cs
│
├── KnowledgeAssistant.Application
│   ├── BackgroundServices
│   ├── Data
│   │   ├── Configs
│   │   ├── Context
│   │   └── Migrations
│   ├── DTOs
│   ├── Entities
│   ├── Handlers
│   └── Services
│
├── KnowledgeAssistant.Tests
│   ├── Controllers
│   ├── Services
│   └── ...
│
├── KnowledgeAssistant.UI
│   ├── Components
│   │   ├── Layout
│   │   └── Pages
│   ├── Services
│   └── Program.cs
│
├── screenshots
│   ├── dashboard.png
│   ├── chat.png
│   ├── documents.png
│   ├── users.png
│   └── login.png
│
└── README.md
```

<details>
<summary><strong>Project responsibilities</strong></summary>

### API

Provides the HTTP interface for the application.

Controllers handle incoming requests and delegate application operations to the appropriate services.

### Application

Contains the majority of the application's application logic, including:

* Entities
* DTOs
* Database context
* EF Core configurations
* Services
* Background services
* Handlers
* Database migrations

### UI

The user-facing Blazor application.

Responsible for:

* Authentication
* AI chat
* Chat sessions
* Document management
* User management
* Administration
* Navigation

### Tests

Contains automated tests covering important application behaviour across controllers and services.

</details>

---

## 04 · Screenshots

### AI Chat

<div align="center">

<img src="screenshots/chat.png" alt="AI Chat" width="850">

</div>

### Document Management

<div align="center">

<img src="screenshots/documents.png" alt="Document Management" width="850">

</div>

### User Management

<div align="center">

<img src="screenshots/users.png" alt="User Management" width="850">

</div>

### Dashboard

<div align="center">

<img src="screenshots/dashboard.png" alt="Dashboard" width="850">

</div>

### Login

<div align="center">

<img src="screenshots/login.png" alt="Login" width="850">

</div>

---

## 05 · Features

### AI Chat

The application uses `IChatClient` from `Microsoft.Extensions.AI` to provide the AI chat functionality.

Chat sessions and messages are persisted, allowing users to maintain multiple conversations.

```text
User
  ↓
Blazor UI
  ↓
API
  ↓
ChatService
  ↓
RAG Retrieval
  ↓
AI Context
  ↓
IChatClient
  ↓
OpenAI
  ↓
Response
  ↓
Save Message
  ↓
UI
```

### Retrieval-Augmented Generation

Documents are split into smaller chunks and embeddings are generated for those chunks.

When a user asks a question, the question is converted into an embedding and compared against the stored document embeddings.

Relevant chunks are then supplied to the AI model as context.

```text
Document
    ↓
Chunking
    ↓
Embedding Generation
    ↓
SQL Server
    ↓
User Question
    ↓
Question Embedding
    ↓
Similarity Search
    ↓
Relevant Chunks
    ↓
AI Context
    ↓
AI Response
```

This allows the application to answer questions using its own documents rather than relying solely on the model's existing knowledge.

### Document Management

Users can upload and manage documents through the application.

Documents are processed into smaller chunks which can subsequently be used by the RAG pipeline.

### Chat Sessions

Users can create and return to multiple conversations.

Each session contains its own message history and belongs to the authenticated user.

### Authentication & Authorisation

JWT authentication is used alongside role-based authorisation.

The application currently supports roles such as:

* `Admin`
* `User`

Administrative functionality can therefore be restricted using ASP.NET Core's built-in authorisation system.

### User Management

Administrators can manage users and their assigned roles.

The authenticated user's identity is taken from their claims rather than relying on a user ID supplied by the client.

### RabbitMQ

RabbitMQ is used to move background operations away from the main HTTP request.

For example, emails can be added to a queue and processed by a background consumer rather than being sent during the original API request.

The same pattern can be used for other asynchronous operations such as user notifications.

### Email Queue

```text
API Request
     ↓
Create Email
     ↓
RabbitMQ
     ↓
Background Consumer
     ↓
Email Provider
     ↓
Email Sent
```

### Caching

`IMemoryCache` is used for data that does not need to be retrieved from the database on every request.

### Rate Limiting

The API includes rate limiting to prevent excessive requests.

### Output Caching

Selected API responses can use ASP.NET Core output caching where appropriate.

### Automated Testing

An xUnit and bUnit test project covers important application behaviour across controllers, services and user interface components.

The focus is on meaningful tests rather than attempting to achieve 100% code coverage and strict TDD.

---

## 06 · Trade Offs

### SQL Server for Application Data and Embeddings

Document data and embeddings are currently stored alongside the application's other data in SQL Server.

This avoids introducing a separate vector database while the project remains relatively small.

**Benefits**

* One database to maintain
* Simple local development
* Lower infrastructure requirements
* Document metadata and embeddings remain together

**Trade-off**

A dedicated vector solution may provide better indexing and retrieval performance at a much larger scale.

---

### In-Memory Caching

`IMemoryCache` is used instead of introducing Redis.

This keeps the application simple and avoids another infrastructure dependency.

**Trade-off**

In-memory caching is tied to an individual application instance, so a distributed cache would be more appropriate for a horizontally scaled deployment.

---

### RabbitMQ

RabbitMQ adds infrastructure and operational complexity compared with performing operations synchronously.

The benefit is that longer-running work can be moved out of the HTTP request and processed independently.

For this project, the additional complexity is useful because it demonstrates a real-world asynchronous processing pattern.

---

### Application Project Instead of Full Clean Architecture

The current solution is structured as:

```text
API
Application
UI
Tests
```

rather than introducing separate Domain and Infrastructure projects.

For the current size of the application, adding further projects would introduce additional abstraction without providing much practical benefit.

If the application grew significantly, a more formal structure could be introduced:

```text
Domain
Application
Infrastructure
API
UI
Tests
```

---

### OpenAI Dependency

The application currently uses OpenAI for chat and embedding generation.

The use of `Microsoft.Extensions.AI` and `IChatClient` helps keep the application less tightly coupled to a specific AI implementation.

This makes changing AI providers easier in the future.

---

### Similarity Search

The current RAG implementation performs similarity calculations against stored embeddings.

This keeps the implementation straightforward and makes the underlying RAG process easy to understand.

For a significantly larger document collection, a dedicated vector index or SQL Server's vector capabilities would provide a more scalable approach.

---

<div align="center">

### KnowledgeAssistant

Built with .NET 10 · Blazor · SQL Server · OpenAI

[Back to top](#knowledgeassistant)

</div>
