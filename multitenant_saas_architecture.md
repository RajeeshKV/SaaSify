# Multi-Tenant SaaS Backend (.NET Clean Architecture)

## Overview
A multi-tenant SaaS backend with Clean Architecture, CQRS-lite, JWT auth, logging, caching, and microservices.

## Core Features
- Multi-tenancy (Tenant isolation)
- JWT Authentication
- CQRS-lite
- UnitOfWork (generic)
- Logging + CorrelationId
- Caching (Memory/Redis)
- Centralized Error Handling
- Microservice (Order Processing)

## Architecture
WebAPI → Application → Domain → Infrastructure

## Folder Structure
(Refer to earlier message structure)

## Flow
Request → Middleware → Controller → Handler → UoW → DB

## Middleware
- TenantMiddleware
- CorrelationMiddleware
- ExceptionMiddleware

## Logging
- Structured logging (Serilog)
- Includes CorrelationId, TenantId

## Caching
- ICacheService abstraction
- MemoryCache → Redis later

## Microservice Flow
API → Event → Queue → Order Service → Processing

## Roadmap
1. Base setup
2. JWT + Query Filters
3. Logging + Error Handling
4. Caching
5. Microservices

## Next Step
Implement Global Query Filters
