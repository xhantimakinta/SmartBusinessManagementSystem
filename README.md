# SmartBusiness Management System

SmartBusiness Management System is a full-stack business application for managing customers, products, orders, reporting, and operational analytics. The solution includes a .NET API backend and a React + Vite frontend, with a local SQLite default configuration for rapid development and a SQL Server option for production-style deployment.

## Overview

This project delivers a compact business management experience with:

- customer and product records
- category management
- order creation and status tracking
- dashboard summaries and reporting
- authentication and role-based access
- REST API documentation through Swagger
- local-first development support without requiring Docker

## Technology Stack

- Backend: ASP.NET Core 10, C#, Entity Framework Core
- Frontend: React 19, TypeScript, Vite
- Data: SQLite for local development, SQL Server ready for deployment
- Testing: xUnit, Vitest
- CI: GitHub Actions

## Repository Structure

- backend/SmartBusiness.Api - API project and HTTP entry point
- backend/SmartBusiness.Application - application layer and business logic
- backend/SmartBusiness.Domain - domain entities and business rules
- backend/SmartBusiness.Infrastructure - persistence and infrastructure services
- backend/SmartBusiness.Api.Tests - backend integration and service tests
- frontend/ - React UI application
- .github/workflows - CI automation

## Prerequisites

- .NET SDK 10
- Node.js 22+
- npm

## Local Development

1. Clone the repository.
2. Copy `.env.example` to `.env` if you want to use the provided environment defaults.
3. Restore backend packages:

   dotnet restore SmartBusiness.slnx

4. Install frontend dependencies:

   cd frontend
   npm install

5. Start the API from the repository root:

   dotnet run --project backend/SmartBusiness.Api/SmartBusiness.Api.csproj --urls http://localhost:5017

6. Start the frontend in a second terminal:

   cd frontend
   npm run dev -- --host 0.0.0.0 --port 5173

7. Open the application at http://localhost:5173

## Default Local Configuration

The project is configured to run locally with SQLite by default so it can be started without Docker. The API uses the development app settings and automatically falls back to a local SQLite database file if SQL Server is not configured.

If you want to use the SQL Server container setup, copy the example environment file and run:

   docker compose -f docker-compose.dev.yml up --build

## Testing

Run the backend tests:

   dotnet test SmartBusiness.slnx --nologo

Run the frontend tests:

   cd frontend
   npm test -- --run

Run frontend linting:

   cd frontend
   npm run lint

## CI

The repository includes a GitHub Actions workflow for backend and frontend verification. The workflow checks formatting, restore, build, test, lint, and Docker image build health.

## License

This project is licensed under the MIT License. See the LICENSE file for details.
