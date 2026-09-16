# ADR-014: Standardize the Monorepo on .NET Tooling

Status: Accepted

Supersedes: the pnpm workspace and Turborepo tooling decision in ADR-001. The monorepo decision itself remains accepted.

## Context

ADR-001 selected pnpm workspace and Turborepo when the repository expected a JavaScript frontend. ADR-013 later standardized the frontend on Blazor WebAssembly and the backend is ASP.NET Core, so the product is now fully based on the .NET ecosystem.

Keeping JavaScript monorepo tooling would add a second build system without a product requirement and would conflict with the current Foundation tasks.

## Decision

Use a .NET monorepo with:

- a root `.slnx` solution;
- `global.json` for SDK selection;
- `Directory.Build.props` for shared build settings;
- `Directory.Packages.props` for Central Package Management;
- a local .NET tool manifest;
- `dotnet restore`, `dotnet format`, `dotnet build` and `dotnet test` as baseline quality commands.

pnpm and Turborepo are not required unless a future accepted ADR introduces a JavaScript workspace with a concrete product need.

## Consequences

- Frontend, backend, shared contracts and tests use one SDK and build toolchain.
- Package versions and compiler rules are consistent across the monorepo.
- CI does not require Node.js for the baseline Blazor WebAssembly application.
- Any future non-.NET workspace must document its build and dependency boundaries in a new ADR.
