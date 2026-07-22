# Talentio AI Powered Platform

AI-powered recruitment and talent management platform for intelligent hiring, candidate tracking, workforce insights, and end-to-end talent lifecycle management.

## Repository purpose

This repository contains different development snapshots of the Talentio platform. 

## High-level architecture (from `v1`/`v2`)

- **Backend (`Backend/`)**
  - ASP.NET Core Web API + Entity Framework Core (SQLite)
- JWT authentication (signed JWTs)
  - Swagger/OpenAPI
  - Controller-based API design
  - In `v1`, extra services for AI, calendar integration, and storage

- **Frontend (`Frontend/`)**
  - React + Vite application
  - Authentication context and role-aware views
  - Build/lint scripts managed in `package.json`

- **Desktop host integration (Backend `Program.cs`)**
  - Windows desktop shell (WPF + WebView2) that launches the backend and displays the frontend in an embedded browser.

## Key technologies

### Backend
- .NET 8 (`net8.0-windows`)
- ASP.NET Core
- Entity Framework Core (SQLite)
- JWT authentication (signed JWTs)
- Swashbuckle (Swagger)
- BCrypt (password hashing)
- MailKit (email)
- Google Calendar API client

### Frontend
- React 19
- Vite
- React Router DOM
- OXlint


> Note: backend targets `net8.0-windows` and uses WPF/WebView2 in `Program.cs`, so full desktop-host behavior is Windows-oriented.
