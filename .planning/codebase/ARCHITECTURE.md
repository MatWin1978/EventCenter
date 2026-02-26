# Architecture

**Analysis Date:** 2026-02-26

## Pattern Overview

**Overall:** Microservices event-driven architecture with Docker Compose orchestration and API gateway routing

**Key Characteristics:**
- Service-oriented distributed system with dedicated applications for error handling, causal analysis, semantic search, and manufacturing workflows
- Event-driven synchronization through HTTP webhooks with pub/sub pattern via central pipeline broker
- Layered access through Traefik reverse proxy providing unified REST API gateway
- Database-centric event notification: changes in ErrorDB trigger cascading updates across services
- Correlation tracking through request headers and logs for distributed request tracing

## Layers

**API Gateway & Routing:**
- Purpose: Unified HTTP entry point, SSL/TLS termination, request routing, and path-based service discovery
- Location: `central-services-and-infrastructure/traefik/`
- Contains: Traefik reverse proxy configuration, certificate management, dynamic routing rules
- Depends on: Docker socket for service discovery, SSL certificates
- Used by: All external clients and inter-service communication

**Frontend Applications:**
- Purpose: User interfaces for error analysis, manufacturing dashboards, and semantic search
- Location: `kausalassist-apps/docker-compose.fma.yml` (FMA application with 3 frontend components)
- Contains: Vue/Vite-based frontend apps, dashboard, application components
- Depends on: FMA backend API, Keycloak for authentication
- Used by: End users via browser

**Backend Services:**

**FMA Backend (Queo):**
- Purpose: Application data management, error workflow, user interactions
- Location: `kausalassist-apps/docker-compose.fma.yml`
- Contains: .NET/C# REST API for application features
- Depends on: Keycloak authentication, Traefik routing
- Used by: FMA frontend, CentPipe notifications

**ErrorDB (Solution and Detect):**
- Purpose: Central error and solution repository, event source for entire system
- Location: `kausalassist-apps/docker-compose.db.yml`
- Contains: REST API (port 8000), PostgreSQL database with backup management
- Depends on: PostgreSQL database (TimescaleDB)
- Used by: All other services for error/environment/solution queries and notifications

**SDP (Semantic Data Pipeline):**
- Purpose: Error classification and causal analysis with ReBOSA recommendation engine
- Location: `kausalassist-apps/docker-compose.sds.yml`
- Contains: Java Spring Boot services (SDP + ReBOSA), TimescaleDB for historical data
- Depends on: ErrorDB API, Keycloak authentication
- Used by: CentPipe for semantic analysis and causal graph generation

**KausalApp:**
- Purpose: Causal graph analysis, clustering, event processing with Python Jupyter notebook interface
- Location: `kausalassist-apps/docker-compose.kapp.yml`
- Contains: Python Flask REST API (port 5300), SQLite graph database, event history cache
- Depends on: ErrorDB API, Keycloak authentication, SDP notifications
- Used by: CentPipe for graph analysis and error relationship discovery

**SEMX (Semantic Explorer):**
- Purpose: Semantic search and ML-based error document analysis
- Location: `kausalassist-apps/docker-compose.semx.yml`
- Contains: Python ML service for semantic embeddings and search
- Depends on: ErrorDB API, CentPipe notifications
- Used by: CentPipe for semantic search capabilities

**Central Infrastructure:**

**CentPipe (Central Pipeline):**
- Purpose: Event broker and notification hub that orchestrates inter-service communication
- Location: `central-services-and-infrastructure/centpipe/`
- Contains: Python Flask service managing pub/sub event distribution
- Depends on: ErrorDB for change events, Keycloak for authentication
- Used by: ErrorDB (publishes changes), all services consume notifications

**Keycloak:**
- Purpose: OAuth2/OIDC identity and access management
- Location: `central-services-and-infrastructure/keycloak/`
- Contains: Java/Quarkus IAM service with realm/role management
- Depends on: PostgreSQL (separate), external DNS for issuer consistency
- Used by: All backend services and frontend apps for authentication

**ElasticSearch + Kibana + Filebeats:**
- Purpose: Centralized logging, indexing, and visualization
- Location: `central-services-and-infrastructure/elastic/kausal-elk/`
- Contains: ElasticSearch cluster, Kibana UI, Filebeats log collectors
- Depends on: All services (collect logs), Docker volumes
- Used by: Operations for debugging and monitoring

**Nextcloud:**
- Purpose: File storage and document management for artifacts and backups
- Location: `central-services-and-infrastructure/nextcloud/`
- Contains: PHP/Nextcloud instance with database
- Depends on: Apache web server, PostgreSQL
- Used by: Data backup storage and file sharing

**Sentry:**
- Purpose: Error tracking and exception monitoring for backend services
- Location: `central-services-and-infrastructure/sentry/`
- Contains: Self-hosted Sentry instance with Kafka, Redis, PostgreSQL
- Depends on: Multiple infrastructure services
- Used by: Backend services for error reporting

## Data Flow

**Error Creation and Synchronization Flow:**

1. OPC/Manufacturing event arrives at SDP (Solution and Detect) via external source or SDP directly creates error
2. SDP calls ErrorDB POST `/db/api/v2/Errors` to create new error record
3. ErrorDB saves error to PostgreSQL and immediately notifies CentPipe via POST `/cent-pipe/api/v1/notify/databaseChange`
4. CentPipe receives change notification and broadcasts to all subscribed services:
   - Broadcasts to FMA backend: POST `/fma/api/v1/cp/notify`
   - Broadcasts to KausalApp: POST `/kapp/api/v1/notify/databaseChange`
   - Broadcasts to SEMX: POST `/semx/api/v1/semantic_search/db_update`
5. KausalApp receives notification, updates internal causal graph, patches ErrorDB with cluster ID: PATCH `/db/api/v2/ErrorHistory/{id}`
6. ErrorDB notifies CentPipe again about the cluster ID patch
7. FMA frontend displays updated error to user

**Request Tracing:**
- All requests include `X-Correlation-Id` header for distributed tracing
- Traefik logs all request/response pairs with correlation ID
- Filebeats collects logs from central-log-volume
- ElasticSearch indexes logs for cross-service flow analysis

**State Management:**
- ErrorDB is authoritative source of truth (PostgreSQL)
- KausalApp maintains in-memory graph cache (SQLite + pickle dumps for persistence)
- Services subscribe to notifications rather than polling
- Each service maintains independent state copies synchronized via change events

## Key Abstractions

**Error and ErrorHistory:**
- Purpose: Represents detected faults with classification metadata and resolution tracking
- Examples: `docker-compose.db.yml`, `tests/3_tests_integration_full_stack/test_full_communication_route.py`
- Pattern: Domain entity with mutable state (CRUD operations), notification events on change

**Environment:**
- Purpose: Represents manufacturing workstation, assembly line, or production cell
- Examples: ErrorDB schema, referenced in test fixtures
- Pattern: Parent-child relationship with errors, Solution mappings

**Service-to-Service APIs:**
- Location: Each service exposes REST API under `/servicename/api/v1/...`
- Pattern: HTTP POST for notifications, GET/PATCH/PUT/DELETE for CRUD operations
- Error handling: Services retry on connection failures via docker restart policies

**Notification Events:**
- Location: ErrorDB publishes to CentPipe via webhooks
- Pattern: JSON payload with event type, resource ID, timestamp
- Processing: CentPipe transforms single notification into multiple outbound notifications

**Keycloak OAuth Integration:**
- Location: `central-services-and-infrastructure/keycloak/docker-compose.keycloak.yml`
- Pattern: Services validate JWT tokens against Keycloak issuer, use OAUTH2_ISSUER_URI environment variable
- Critical: Issuer URI must match between frontend (via Traefik) and backend (direct Docker DNS)

## Entry Points

**HTTP REST APIs:**
- FMA Backend: `http://localhost:8080/fma/api/v1/...` (Traefik routes to backend container)
- KausalApp: `http://localhost:8080/kapp/api/v1/...`
- ErrorDB: `http://localhost:8080/db/api/v2/...`
- SDP: `http://localhost:8080/sdp/...`
- SEMX: `http://localhost:8080/semx/api/v1/...`
- CentPipe: `http://localhost:8080/cent-pipe/api/v1/...`

**Frontend Web UIs:**
- FMA Application: `https://localhost:8443/fma/app/`
- FMA Dashboard: `https://localhost:8443/fma/dashboard/`
- SDP UI: `http://localhost:8080/sdp/`
- Kibana: `http://localhost:5601/`
- ReBOSA UI: `http://localhost:8445/`

**Stack Initialization:**
- Location: `/start_KausaLAssist_stack_full.sh` orchestrates multi-phase startup
- Responsibilities:
  1. Creates Docker networks and volume mounts
  2. Pulls images from container registry
  3. Starts central services (Traefik, Keycloak, ELK)
  4. Starts application services (DB, FMA, KausalApp, SDP, SEMX)
  5. Configures ElasticSearch log mappings
  6. Starts Filebeats collectors

## Error Handling

**Strategy:** Layered approach with service-level health checks, database transactionality, and retry mechanisms

**Patterns:**

**Service Health Checks:**
- Location: Docker Compose `healthcheck` clauses in all docker-compose files
- Example: ErrorDB `pg_isready` check, KausalApp curl to `/api/v1/health`
- Behavior: Container restart on failure with exponential backoff

**Database Constraints:**
- Location: PostgreSQL schemas in ErrorDB and TimescaleDB (SDP)
- Pattern: ACID transactions ensure error/solution consistency
- Error flow: Constraint violations returned via HTTP 400/422 response codes

**Notification Retry:**
- Location: CentPipe subscribes to ErrorDB change events
- Pattern: Services listen on POST webhooks, CentPipe broadcasts to multiple subscribers
- Failure handling: Services implement retry logic, optional restart policies (`restart: on-failure:15`)

**Request Correlation:**
- Pattern: X-Correlation-Id header propagated through entire call chain
- Benefits: Enables distributed debugging via log aggregation in ElasticSearch

**API Error Responses:**
- Standard HTTP status codes (200, 201, 204, 400, 404, 500)
- JSON error bodies with message and details
- Documented via OpenAPI/Swagger endpoints at each service

## Cross-Cutting Concerns

**Logging:**
- Framework: JSON-based structured logging via Filebeats
- Configuration: `central-services-and-infrastructure/elastic/kausal-elk/filebeats/.env` maps service names to IP addresses
- Centralized indexing: ElasticSearch stores all logs with automatic parsing
- Query: Kibana UI for visualization and debugging via correlation ID

**Validation:**
- Framework: Service-specific (FMA uses .NET validation, Python services use Flask validators)
- Location: Request body validation in each API handler
- Pattern: Return 400 Bad Request with validation error details

**Authentication:**
- Framework: OAuth2 via Keycloak
- Implementation: Backend services validate JWT tokens against Keycloak `/realms/kausal` endpoint
- Issuer consistency: Must match across frontend (external) and backend (Docker DNS) paths
- Configuration: OAUTH2_ISSUER_URI environment variable in all services

**Authorization:**
- Pattern: Role-based access control (RBAC) via Keycloak realm roles
- Implementation: Backend API handlers check roles in JWT claims
- Example: Different UI screens for admin vs. operator roles

**SSL/TLS:**
- Termination: Traefik handles HTTPS on port 8443
- Certificates: Mounted from `traefik/certs/out/` directory
- Custom CA: Individual services import `kausalRootCA.pem` for internal certificate validation
- Entrypoint configuration: FMA frontend redirects HTTP to HTTPS

---

*Architecture analysis: 2026-02-26*
