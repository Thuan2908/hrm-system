# ADR-013: Use Blazor WebAssembly for Frontend

Status: Accepted

## Context
The project is standardizing FE and BE on the .NET ecosystem.

## Decision
Use Blazor WebAssembly + C# for the frontend.

## Consequences

Positive:
- C# on both frontend and backend.
- Shared contracts are easier.
- Good fit for .NET tooling.

Negative:
- Client bundle can be larger than JS SPA alternatives.
- Secrets cannot be trusted in WebAssembly.
- Authentication/token handling must be designed carefully.
