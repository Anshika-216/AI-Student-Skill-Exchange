# Repository Structure

## Project

AI-Powered Student Skill Exchange and Peer Learning Platform

## 1. Overview

The repository is organized to separate controllers, data transfer objects, database models, services, views, documentation, and configuration files.

This structure helps maintain separation of responsibilities and makes the application easier to develop, test, and maintain.

## 2. Current Repository Structure

```text
AI-Student-Skill-Exchange/
│
├── Controllers/
│   ├── LearningController.cs
│   └── Other Controllers
│
├── DTOs/
│   ├── LearningDtos.cs
│   └── Other DTOs
│
├── Models/
│   ├── ApplicationUser.cs
│   ├── Skill.cs
│   ├── StudentSkill.cs
│   ├── LearningRequest.cs
│   ├── LearningSession.cs
│   ├── Feedback.cs
│   └── ViewModels/
│
├── Data/
│   └── ApplicationDbContext.cs
│
├── Migrations/
│   └── Database Migration Files
│
├── Services/
│   └── Application Services
│
├── Views/
│   └── Application Views
│
├── wwwroot/
│   ├── css/
│   ├── js/
│   └── other static resources
│
├── docs/
│   ├── system-design/
│   │   ├── system-architecture.md
│   │   ├── module-design.md
│   │   └── repository-structure.md
│   │
│   └── database/
│       └── database-design.md
│
├── Properties/
│   └── launchSettings.json
│
├── Program.cs
├── appsettings.json
├── appsettings.Development.json
└── AIStudentSkillExchange.csproj
```
##3 Directory Responsibilities
Controllers

Controllers handle HTTP requests and coordinate communication between the user interface, application logic, and data layer.

DTOs

DTOs (Data Transfer Objects) define the data transferred between different parts of the application and help control the information exposed through APIs.

Models

Models represent the main entities of the application and their relationships.

Examples include users, skills, student skills, learning requests, learning sessions, and feedback.

Data

The Data directory contains the database context and database-related configuration.

Migrations

The Migrations directory contains Entity Framework Core migration files used to create and update the database schema.

Services

The Services directory contains application-level business logic and reusable functionality.

Views

Views contain the user interface pages used to display application information.

wwwroot

The wwwroot directory contains static resources such as CSS, JavaScript, images, and other client-side files.

docs

The docs directory contains project documentation including system architecture, module design, database design, and repository documentation.

4. Development Organization

The project follows a modular structure where each major feature is developed independently and integrated into the main application.

Feature branches are used for individual modules and improvements.

Examples:

skill-management
feat/peer-discovery-skill-matching
feat/ai-recommendation-module
feat/system-design-architecture

Changes are integrated into the main branch through pull requests whenever possible.

5. Documentation Organization

Project documentation is maintained inside the docs directory.

The documentation includes:

System Architecture
Module Design
Database Design
Repository Structure

Additional documentation can be added as new project requirements are completed.

6. Future Repository Expansion

As development progresses, additional directories or files may be added for:

Authentication
AI services
Peer matching services
Testing
API documentation
Deployment configuration

The repository structure will be updated as the project grows.
