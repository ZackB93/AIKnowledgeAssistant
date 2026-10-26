<div align="center">

# KnowledgeAssistant

### AI-powered knowledge assistant built with .NET 10, Blazor, SQL Server and RAG. Hosted on Microsoft Azure.

<p>
  <img src="https://img.shields.io/badge/.NET-10-512BD4?style=flat-square&logo=dotnet&logoColor=white" alt=".NET 10">
  <img src="https://img.shields.io/badge/Blazor-Server-512BD4?style=flat-square&logo=blazor&logoColor=white" alt="Blazor">
  <img src="https://img.shields.io/badge/SQL%20Server-CC2927?style=flat-square&logo=microsoftsqlserver&logoColor=white" alt="SQL Server">
  <img src="https://img.shields.io/badge/OpenAI-412991?style=flat-square&logo=openai&logoColor=white" alt="OpenAI">
  <img src="https://img.shields.io/badge/Azure-0078D4?style=flat-square&logo=microsoftazure&logoColor=white" alt="Azure">
  <img src="https://img.shields.io/badge/RAG-194A8C?style=flat-square" alt="RAG">
  <img src="https://img.shields.io/badge/xUnit-512BD4?style=flat-square&logo=xunit&logoColor=white" alt="xUnit">
</p>

<p>
  A practical full-stack .NET application exploring AI integration, document retrieval,
  authentication, asynchronous processing and modern application architecture hosted on Microsoft Azure.
</p>

</div>

---

## Contents

|        | Section                                 |
| :----: | --------------------------------------- |
| **01** | [Overview](#01--overview)               |
| **02** | [Azure](#02--azure)                     |
| **03** | [Technology Used](#03--technology-used) |
| **04** | [File Structure](#04--file-structure)   |
| **05** | [Screenshots](#05--screenshots)         |
| **06** | [Features](#06--features)               |
| **07** | [Trade Offs](#07--trade-offs)           |
| **08** | [To Do](#08--to-do)                     |

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
| RAG            | Embeddings + Similarity search   |
| Database       | SQL Server + EF Core             |
| Messaging      | RabbitMQ                         |
| Authentication | JWT + Role-based authorisation   |
| UI             | Blazor + MudBlazor + SignalR     |
| Testing        | xUnit + bUnit (UI)               |
| Cloud          | Microsoft Azure + Azure DevOps   |

---

## 02 · Microsoft Azure

The application is hosted in Microsoft Azure using Azure's **Free Tier** for this project.

### Azure Services

| Service | Purpose |
| ------- | ------- |
| **Azure DevOps** | CI/CD pipelines for building, testing and deploying the application. |
| **Azure App Service** | Hosts the Blazor UI and ASP.NET Core API. |
| **Azure SQL** | Hosts the SQL database. |
| **Azure Application Insights** | Monitoring, diagnostics and logging for the UI and API. |
| **Azure Key Vault** | Secure storage for application secrets. |

### Live Application

| Application | URL |
| ----------- | --- |
| **Blazor UI** | [Open KnowledgeAssistant](https://knowledgeassistant-ui-e2hwbcexacddeabz.ukwest-01.azurewebsites.net) |
| **API** | [Open API](https://knowledgeassistant-api-g8gsadbuf2gueghr.ukwest-01.azurewebsites.net) |

The application is currently running on the **Azure Free Tier**, keeping hosting costs low while providing hands-on experience with cloud deployment, CI/CD and application monitoring.

### Login Details

You can sign in using the following details:
| Email | Password |
| ----------- | --- |
| zack.bucci@example.com | test |

There is no sensitive or personal information within the database.

---

## 03 · Technology Used

### Backend

* **C#**
* **.NET 10**
* **ASP.NET Core REST Web API**
* **Entity Framework Core**
* **SQL Server**
* **JWT Authentication**
* **Role-based Authorisation**
* **Serilog Logging**
* **Scalar**

### Frontend

* **Blazor Web App**
* **MudBlazor**
* **SignalR**
* **SAPBlazorAnimate**
* **Blazor-ApexCharts**
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
* **LLM Function / Tool Calling**

### Messaging & Background Processing

* **RabbitMQ**
* **ASP.NET Core Background Services**
* **Queued email processing**
* **Queued notification batch processing**

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

## 04 · File Structure

The solution is currently split into six projects and follows clean architecture layers.

```text
KnowledgeAssistant
│
├── KnowledgeAssistant.API
│   ├── Controllers
│   ├── Hubs
│   ├── Middleware
│   └── Program.cs
│
├── KnowledgeAssistant.Application
│   ├── DTOs
│   ├── Handlers
│   ├── Interfaces
│   │   ├── Hubs
│   │   └── Repositories
│   ├── Services
│   └── Tools
│       └── Knowledge
│
├── KnowledgeAssistant.Domain
│   ├── Entities
│   └── Enums
│
├── KnowledgeAssistant.Infrastructure
│   ├── BackgroundServices
│   ├── Data
│   │   ├── Configs
│   │   ├── Context
│   │   └── Migrations
│   └── Repositories
│
├── KnowledgeAssistant.Tests
│   ├── API
│   ├── Application
│   └── UI
│
├── KnowledgeAssistant.UI
│   ├── Components
│   │   ├── Authentication
│   │   ├── Common
│   │   ├── Layout
│   │   ├── User
│   │   ├── App.razor
│   │   └── Routes.razor
│   ├── Helpers
│   ├── Pages
│   ├── _Imports.razor
│   ├── appsettings.json
│   └── Program.cs
│
├── screenshots
│
└── README.md
```

<details>
<summary><strong>Project responsibilities</strong></summary>

### API

Provides the HTTP interface for the application.

Controllers handle incoming requests and delegate operations to the appropriate application services and handlers.

Responsible for:

* HTTP endpoints and request handling
* Authentication and authorisation configuration
* Middleware
* API-specific configuration

### Application

Contains the application's business use cases and application logic.

Responsible for:

* DTOs
* Application interfaces
* Repository interfaces
* Services
* Handlers
* Application-specific business workflows
* Orchestrating operations between the API, domain and infrastructure layers

The Application layer contains abstractions for external concerns such as repositories, allowing the application logic to remain independent of the underlying implementation.

### Domain

Contains the core business model and domain rules of the application.

Responsible for:

* Domain entities
* Enums
* Core business concepts
* Domain-level rules and behaviour

The Domain layer has no dependency on the API, UI, database or infrastructure implementations.

### Infrastructure

Contains implementations for external and persistence-related concerns.

Responsible for:

* Entity Framework Core
* Database context
* EF Core configurations
* Database migrations
* Repository implementations
* Background services
* Integration with external infrastructure and services

Infrastructure implements the interfaces defined by the Application layer and handles communication with external systems such as the database and messaging infrastructure.

### UI

The user-facing Blazor application.

Responsible for:

* Authentication
* AI chat
* Chat sessions
* Document management
* User management
* Notifications
* Administration
* Navigation
* User interface components and pages

### Tests

Contains automated tests covering important application behaviour across controllers, services and UI components.
Strict Test Driven Development has not been followed in this project.

</details>

---

## 05 · Screenshots

### Login

<div align="center">

<img src="screenshots/sign_in.png" alt="Login" width="850">

</div>

### Dashboard

<div align="center">

<p>User Dashboard</p>
<img src="screenshots/user_dashboard.png" alt="User Dashboard" width="850">

<p>Admin Dashboard</p>
<img src="screenshots/admin_dashboard.png" alt="Admin Dashboard" width="850">

</div>

### AI Chat

<div align="center">

<img src="screenshots/chat_general_convo.png" alt="AI Chat" width="850">
<img src="screenshots/chat_general_convo_memory.png" alt="AI Chat" width="850">
<img src="screenshots/chat_general_convo_llm.png" alt="AI Chat" width="850">
<img src="screenshots/chat_attachment_convo1.png" alt="AI Chat" width="850">
<img src="screenshots/chat_attachment_convo2.png" alt="AI Chat" width="850">
<img src="screenshots/chat_attachment_convo3.png" alt="AI Chat" width="850">

</div>

### Document Management

<div align="center">

<img src="screenshots/admin_documents.png" alt="Documents Management" width="850">
<img src="screenshots/admin_documents_upload.png" alt="Document Upload" width="850">

</div>

### User Management

<div align="center">

<img src="screenshots/admin_users.png" alt="User Management" width="850">
<img src="screenshots/admin_users_search.png" alt="User Management" width="850">
<img src="screenshots/admin_users_edit.png" alt="User Management" width="850">

</div>

### Role Management

<div align="center">

<img src="screenshots/admin_roles.png" alt="Roles Management" width="850">

</div>

### Email Management

<div align="center">

<img src="screenshots/admin_emails.png" alt="Emails Management" width="850">

</div>

### Notifications

<div align="center">

<p>Admin</p>
<img src="screenshots/admin_notifications.png" alt="Notifications" width="850">
<img src="screenshots/admin_notification_details.png" alt="Notifications" width="850">
<img src="screenshots/admin_notification_recipients.png" alt="Notifications" width="850">
<img src="screenshots/admin_notification_send1.png" alt="Notifications" width="850">
<img src="screenshots/admin_notification_send2.png" alt="Notifications" width="850">

<p>User</p>
<img src="screenshots/user_notifications.png" alt="Notifications" width="850">
<img src="screenshots/user_notification_read.png" alt="Notifications" width="850">

</div>

---

## 06 · Features

### AI Chat with memory and LLM tool calling

The application uses `IChatClient` from `Microsoft.Extensions.AI` to provide AI-powered chat functionality through OpenAI.

Chat sessions and messages are persisted, allowing users to maintain multiple conversations and revisit previous chats. The system also stores user-specific facts, preferences, goals and project details in SQL Server to support personalised responses and continuity across conversations.

LLM tool calling allows the model to request specific operations exposed by the application when a question requires live application data. For example, a user could ask how many active users are registered. The model can request the relevant tool, the application executes the operation using its own services and repositories, and the result is returned to the model to help formulate the answer. The model does not receive unrestricted database access; only explicitly registered tools are available to it.

```text
User
  ↓
Blazor UI
  ↓
API
  ↓
ChatService
  ↓
Security Checks & Prompt Injection Safeguards
  ↓
RAG Retrieval & Memory
  ↓
AI Context
  ↓
IChatClient / OpenAI
  ↕
LLM Tool Calling (when required)
  ↓
Output Sanitisation
  ↓
Save Message
  ↓
UI
```

#### AI security considerations

AI input, retrieved document content and tool results must be treated as untrusted data. Prompt-injection safeguards help reduce the risk of instructions embedded in user messages or uploaded documents influencing the model in unintended ways, but they cannot guarantee that every attack will be detected.

Tools should expose narrow, purpose-specific operations rather than general database access. Authorisation must be enforced by the application before sensitive operations run; it must not depend on the model's judgement or on instructions in the prompt. For user-specific information, the authenticated identity should be obtained from trusted server-side claims rather than a user ID supplied by the model or client. Responses returned to users should be sanitised, and tool access and failures should be handled by the application.

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

Admins can upload and manage documents through the application.

Documents are processed into smaller chunks which can subsequently be used by the RAG pipeline.

Document processing can be queued asynchronously using RabbitMQ, allowing the work to be handled by a background service rather than blocking the original HTTP request.

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

### Notification Management

Administrators can create, send and view user notifications and it's recipients on the system. 

SignalR on the UI is used with a Hub for a constant and immediate connection and update of the user's unread notification count.

Notifications are queued, processed and sent in batches to support a large active user system.

### Role Management

Administrators can manage system roles that can be assigned to users.

### Logging & Auditing

The system uses Serilog for normal information and also error logging. It is implemented within the Middleware.

The system has its own Audit table and Audit service so creations, updates, deletes are always audited.

### RabbitMQ

RabbitMQ is used to move background operations away from the main HTTP request.

For example, emails can be added to a queue and processed by a background consumer rather than being sent during the original API request.

The same pattern will be used for future asynchronous operations such as user notifications and document processing.

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

### Automated Testing

An xUnit and bUnit test project covers important application behaviour across controllers, services and user interface components.

The focus is on meaningful tests rather than attempting to achieve 100% code coverage and strict TDD.

---

## 07 · Trade Offs

### SQL Server for Application Data and Embeddings

Document data and embeddings are currently stored alongside the application's other data in SQL Server.

This avoids introducing a separate vector database while the project remains relatively small.

**Benefits**

* One database to maintain
* Simple local development
* Lower infrastructure requirements
* Document metadata and embeddings remain together

**Trade-off**

A dedicated vector solution will provide better indexing and retrieval performance at a much larger scale.

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

### Entity Framework Core

Microsoft's full featured ORM. It works with C# entities and LINQ rather than writing SQL.

**Advantages**

* LINQ queries and strongly typed entities.
* Change tracking and automatic updates.
* Relationships, migrations and navigation properties.
* Excellent integration with dependency injection and transactions.

**Disadvantages**

* Change tracking and entity materialisation add overhead.
* Complex LINQ can generate inefficient SQL.
* Less direct control over the SQL that executes.
* Can be excessive for simple reporting queries.
* A much smaller and more lightweight solution would be Dapper.

---

### Notification Batch Processing

Notifications are processed in batches, rather than having a message for each user.
This helps with scalability and as the user table grows.

The disadvantage is that on a unlikely occassion that if something happens to the system (an issue, blip in network connectivity) only part of the batch will be processed.
The other part of the batch will not have been processed, a retry area within the admin section could be added in case.

---

## 08 · To Do

### 1. Separate Vector Database (PostgreSQL + pgvector)

Introduce a dedicated PostgreSQL database using the pgvector extension to store and query document embeddings independently of the existing SQL Server database.

This will separate vector search from the application's relational data, providing a more specialised foundation for similarity search and future improvements to the RAG pipeline.

This will initially include:

- Storing document embeddings in PostgreSQL using pgvector.
- Implementing vector similarity search using PostgreSQL's vector operators and indexing capabilities.
- Integrating the vector database into the existing document ingestion and retrieval workflows.
- Keeping application data, users, chat sessions and messages in SQL Server.

This will allow the application to use purpose-built vector search alongside its existing relational data architecture.

---

<div align="center">

### KnowledgeAssistant

Built with .NET 10 · Blazor · SQL Server · OpenAI

[Back to top](#knowledgeassistant)

</div>
