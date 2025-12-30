# Trello Clone

[![Lint](https://github.com/jakmate/TrelloClone/workflows/Lint/badge.svg)](https://github.com/jakmate/TrelloClone/actions/workflows/lint.yml)
[![codecov](https://codecov.io/gh/jakmate/TrelloClone/branch/main/graph/badge.svg)](https://codecov.io/gh/jakmate/TrelloClone)
[![Quality Gate Status](https://sonarcloud.io/api/project_badges/measure?project=jakmate_TrelloClone&metric=alert_status)](https://sonarcloud.io/summary/overall?id=jakmate_TrelloClone&branch=main)
[![Security Rating](https://sonarcloud.io/api/project_badges/measure?project=jakmate_TrelloClone&metric=security_rating)](https://sonarcloud.io/summary/overall?id=jakmate_TrelloClone&branch=main)

A full-featured Trello-like task management application built with Blazor WebAssembly and ASP.NET Core.

## To Do

- Async drag and drop visible to other users in real time
- Async add board to boards after accepting invitation

## Overview

- **TrelloClone.Server** — ASP.NET Core backend: domain entities, data persistence (EF Core), API controllers, SignalR hubs, business logic.
- **TrelloClone.Client** — Blazor frontend: UI components, pages, Razor components, services for HTTP and SignalR, CSS, client‑side logic.
- **TrelloClone.Shared** — Shared DTOs and types used by both Server and Client (e.g. data transfer objects, enums).

This architecture cleanly separates frontend, backend, and shared models.

## Tech Stack

Here are the main technologies and frameworks used in **TrelloClone**:

- **Blazor WebAssembly** — client‑side UI framework, allowing you to build rich interactive web front‑ends with C# instead of JavaScript.
- **ASP.NET Core** — backend framework powering the API, real‑time hubs, and web server logic.
- **Entity Framework Core (EF Core)** — ORM for data persistence and database access, used in the server project.
- **SignalR** — real‑time communication library enabling live updates (e.g. for boards, tasks, notifications) between client and server.
- **JWT (JSON Web Tokens)** — used for secure authentication / authorization (login, register, protected API endpoints).
- **Shared DTO / model project** — shared data contracts between server and client to avoid duplication and ensure consistency.
- **.NET 9** — full‑stack C#/.NET environment, unifying backend and frontend codebase.

## Features

- **Board Management**: Create, edit, delete, and organize boards
- **Columns & Tasks**: Drag-and-drop columns and tasks with real-time updates
- **Real-time Collaboration**: Live updates using SignalR
- **User Authentication**: Secure login/registration with JWT tokens
- **Board Invitations**: Invite other users to collaborate on boards
- **Task Assignments**: Assign tasks to team members
- **Task Prioritization**: Multiple priority levels for tasks
- **Responsive Design**: Works on desktop and mobile devices
- **Notifications**: Real-time notifications for board activities
- **User Profiles**: Manage user information and preferences
- **Templates**: Pre-built board templates for quick setup

## Project Structure

    TrelloClone/
    ├── TrelloClone.Client/ # Blazor WebAssembly frontend
    │ ├── Components/ # Reusable UI components
    │ ├── Layouts/ # Layout components
    │ ├── Pages/ # Application pages
    │ └── Services/ # Client-side services
    │
    ├── TrelloClone.Server/ # ASP.NET Core backend
    │ ├── Application/ # Business logic and services
    │ ├── Controllers/ # API endpoints
    │ ├── Domain/ # Entities and interfaces
    │ ├── Infrastructure/ # Data access and external services
    │ └── Migrations/ # Database migrations
    │
    ├── TrelloClone.Shared/ # Shared models and DTOs
    │ └── DTOs/ # Data transfer objects
    │
    └── TrelloClone.sln # Solution file

## Getting Started

### Prerequisites

- [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [Visual Studio 2022](https://visualstudio.microsoft.com/) or [VS Code](https://code.visualstudio.com/)

### Running locally

1. Clone the repository:

   ```bash
   git clone https://github.com/jakmate/TrelloClone.git
   cd TrelloClone
   ```

2. Build and run server + client:

   ```bash
   # from solution root
   dotnet build
   dotnet run --project TrelloClone.Server
   dotnet run --project TrelloClone.Client
   ```

3. Then open a browser to the client (e.g. <http://localhost:5069/>) — depending on your setup.

## Test Coverage

This project uses Coverlet for code coverage analysis. Coverage reports are automatically generated and uploaded to Codecov on every push and pull request.

### Running tests with coverage locally

```bash
# Run all tests with coverage
dotnet test --settings:coverage.runsettings --collect:"XPlat Code Coverage" --results-directory ./TestResults

# Generate HTML coverage report
reportgenerator -reports:"TestResults/**/coverage.cobertura.xml" -targetdir:"coverage-report" -reporttypes:Html

# View it in browser
start coverage-report/index.html
```
