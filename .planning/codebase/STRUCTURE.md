# Codebase Structure

**Analysis Date:** 2026-02-26

## Directory Layout

```
/home/winkler/dev/ProjekteGIT/compose/
├── kausalassist-apps/              # Application services deployed by software team
│   ├── docker-compose.db.yml       # ErrorDB PostgreSQL service
│   ├── docker-compose.fma.yml      # FMA (frontend + backend + dashboard)
│   ├── docker-compose.sds.yml      # SDP + ReBOSA + TimescaleDB
│   ├── docker-compose.kapp.yml     # KausalApp causal graph service
│   ├── docker-compose.semx.yml     # SEMX semantic search service
│   ├── start_kausalassist-apps.sh  # Startup script for all apps
│   ├── clean_stop_kausalassist-apps.sh
│   └── data/                       # Configuration and runtime data
│       ├── db/                     # ErrorDB backups
│       ├── sds/                    # SDP/ReBOSA configs, keystores, DB init scripts
│       ├── kapp/                   # KausalApp SQLite DB, graph models, cache files
│       ├── fma/                    # FMA app settings JSON, Vite configs
│       └── semx/                   # SEMX ML model DB
│
├── central-services-and-infrastructure/  # Shared infrastructure
│   ├── traefik/                    # Reverse proxy, API gateway, SSL termination
│   │   ├── docker-compose.traefik.yml
│   │   ├── certs/                  # SSL certificates
│   │   └── configs/                # Dynamic Traefik configuration files
│   ├── keycloak/                   # OAuth2/OIDC identity management
│   │   └── docker-compose.keycloak.yml
│   ├── elastic/                    # Logging infrastructure
│   │   └── kausal-elk/             # ELK stack (ElasticSearch, Kibana, Filebeats)
│   ├── sentry/                     # Error tracking and exception monitoring
│   │   └── self-hosted-22.4.0/     # Self-hosted Sentry instance
│   ├── centpipe/                   # Central event bus/notification broker
│   │   └── docker-compose.centpipe.yml
│   ├── nextcloud/                  # File storage and backups
│   ├── swagger/                    # OpenAPI documentation aggregator
│   ├── landingpage/                # Home/welcome page
│   ├── logrotate/                  # Log rotation and cleanup
│   ├── start_central-services.sh   # Startup script
│   ├── slim_start_central-services.sh  # Minimal startup (no Elastic/Sentry)
│   └── db_core_start_central-services.sh
│
├── tests/                          # Test suite organized by build phases
│   ├── 0_tests_prebuild/           # Pre-build validation (image tag checks, YAML syntax)
│   ├── 1_tests_build_only_core_required/  # Core services tests
│   │   ├── test_build_succesful.py
│   │   ├── test_openapi_*.py       # API endpoint availability tests
│   │   ├── test_unit_db.py         # ErrorDB unit tests
│   │   └── kausal_helpers.py       # Shared test utilities and API client classes
│   ├── 2_tests_build_full_stack_required/  # Full stack tests (Elastic, Sentry)
│   └── 3_tests_integration_full_stack/     # Integration tests
│       └── test_full_communication_route.py # End-to-end data flow validation
│
├── install/                        # Installation and initialization scripts
│   ├── init_dockerstuff.sh         # Docker login, network creation
│   ├── init_git_submodules.sh      # Git submodule initialization
│   ├── substitute_semx_to_mock.sh  # Test fixture: swap SEMX with mock
│   ├── prune_docker_volumes_24h.sh # Cleanup old Docker artifacts
│   └── init_traefik_elastic_log_mapping.sh  # Configure Filebeats service mapping
│
├── helpers/                        # Operational helper scripts
│   ├── db_maintainer.py            # Database maintenance utilities
│   ├── overview_active_erros_in_db.py
│   ├── container_status_notify.py  # Container health notifications
│   ├── get_new_eh_pkl_file_for_kapp.py
│   └── helpers.py
│
├── docs/                           # Documentation and diagrams
├── demos/                          # Example deployment configurations
└── README.md                       # Main documentation with included services table
```

## Directory Purposes

**kausalassist-apps/**
- Purpose: Contains all application microservices and their Docker orchestration
- Contains: 5 major docker-compose files for independent services
- Key files: `docker-compose.*.yml` files orchestrate each service
- Data persistence: `data/` subdirectories contain configuration files, databases, cache files, and backups

**central-services-and-infrastructure/**
- Purpose: Shared infrastructure services and cross-cutting concerns
- Contains: 8 major infrastructure services including proxy, auth, logging, messaging
- Startup patterns: Multiple start scripts for different deployment scenarios (full, slim, core-only)
- Volumes: All services write logs to `central-log-volume` for centralized collection

**tests/**
- Purpose: Test suite organized by deployment phase and complexity
- Structure: 4 test directories with increasing dependencies and integration scope
- Phase 0: Pre-build static checks (no containers needed)
- Phase 1: Core services only (minimal Docker deployment)
- Phase 2: Full stack (with ElasticSearch and Sentry)
- Phase 3: Integration tests (full end-to-end scenarios)

**install/**
- Purpose: One-time setup scripts for fresh deployments
- Contents: Docker authentication, network creation, Git setup, environment-specific substitutions
- Pattern: Scripts are idempotent and safe to run multiple times

**helpers/**
- Purpose: Operational utilities for maintenance and monitoring
- Contents: Database maintenance, health checks, cache management, notifications

## Key File Locations

**Entry Points:**

| File | Purpose |
|------|---------|
| `/start_KausaLAssist_stack_full.sh` | Complete system startup with all services |
| `/start_KausaLAssist_stack_slim.sh` | Core services only (skip Elastic, Sentry, Filebeats) |
| `central-services-and-infrastructure/start_central-services.sh` | Start only infrastructure |
| `kausalassist-apps/start_kausalassist-apps.sh` | Start only application services |

**Configuration Files:**

| File | Purpose |
|------|---------|
| `kausalassist-apps/data/fma/fma.backend.appsettings.json` | FMA backend configuration (endpoints, auth) |
| `kausalassist-apps/data/sds/sdp/application.yml` | SDP Spring Boot application properties |
| `kausalassist-apps/data/kapp/kapp.env` | KausalApp environment variables |
| `central-services-and-infrastructure/traefik/configs/` | Traefik dynamic routing rules |
| `.gitlab-ci.yml` | CI/CD pipeline (GitLab Runner) |

**Database and State Files:**

| Location | Purpose |
|----------|---------|
| `kausalassist-apps/data/db_backups/` | ErrorDB PostgreSQL dump files |
| `kausalassist-apps/data/kapp/kapp_*.sqlite3` | KausalApp graph database |
| `kausalassist-apps/data/kapp/*eh_cache*.pkl` | KausalApp error history cache |
| `kausalassist-apps/data/sds/db/` | SDP/ReBOSA TimescaleDB initialization |

**Test Locations:**

| File | Purpose |
|------|---------|
| `tests/1_tests_build_only_core_required/kausal_helpers.py` | Shared test utilities (API clients, fixtures) |
| `tests/1_tests_build_only_core_required/test_build_succesful.py` | All services health check |
| `tests/3_tests_integration_full_stack/test_full_communication_route.py` | End-to-end data flow validation with correlation tracking |
| `pytest.ini` | Pytest configuration (ignore deprecation warnings) |

**Core Logic by Service:**

| Service | Key Files | What to Modify |
|---------|-----------|---|
| FMA | `kausalassist-apps/docker-compose.fma.yml` | Backend API endpoints, frontend routes, SSL settings |
| ErrorDB | `kausalassist-apps/docker-compose.db.yml`, `kausalassist-apps/data/db_backups/` | Database schema, backup restoration |
| KausalApp | `kausalassist-apps/docker-compose.kapp.yml`, `kausalassist-apps/data/kapp/` | Graph algorithm, clustering config, cache strategy |
| SDP | `kausalassist-apps/docker-compose.sds.yml`, `kausalassist-apps/data/sds/` | Classification rules, causal scoring |
| SEMX | `kausalassist-apps/docker-compose.semx.yml` | Semantic model, embedding strategy |
| Traefik | `central-services-and-infrastructure/traefik/` | Routing rules, SSL certs, middleware |
| Keycloak | `central-services-and-infrastructure/keycloak/` | Auth realms, roles, user management |

## Naming Conventions

**Files:**
- Docker Compose files: `docker-compose.[service-name].yml`
- Scripts: `[action]_[target].sh` (e.g., `start_KausaLAssist_stack_full.sh`, `clean_stop_stack.sh`)
- Configuration: `[service-name].[config-type].json` or `.yml` (e.g., `fma.backend.appsettings.json`)
- Test files: `test_[feature].py` (e.g., `test_build_succesful.py`, `test_openapi_db.py`)
- Environment files: `.env` or `[service].env` (e.g., `kapp.env`)

**Directories:**
- Service-specific: lowercase with hyphens (e.g., `kausalassist-apps`, `central-services-and-infrastructure`)
- Test categories: numbered prefixes (e.g., `0_tests_prebuild`, `1_tests_build_only_core_required`)
- Data per-service: `data/[service]/` (e.g., `data/kapp/`, `data/sds/`)
- Logs: `central-log-volume` (Docker volume, not filesystem)

**Environment Variables:**
- Service authentication: `OAUTH2_CLIENT_SECRET`, `OAUTH2_ISSUER_URI`
- Database URLs: `DB_URL=jdbc:postgresql://...`, `ERROR_DB_URL=http://...`
- Service endpoints: `[SERVICE]_BASEPATH=http://...` (e.g., `KEYCLOAK_BASEPATH`, `KAPP_BASEPATH`)
- Sensitive paths: `KEYSTORE_PASSWORD` for Java keystores

## Where to Add New Code

**New Backend Microservice:**
1. Create `kausalassist-apps/docker-compose.[service-name].yml` with service definition
2. Add service to `kausalassist-apps/start_kausalassist-apps.sh` startup script
3. Mount configuration file: `./data/[service]/application.yml:/app/config/application.yml`
4. Define labels for Traefik routing: `traefik.http.routers.[service].rule=PathPrefix(...)`
5. Add service to test suite: create test in `tests/1_tests_build_only_core_required/test_openapi_[service].py`
6. Document in main README with service icon and port numbers

**New Infrastructure Service:**
1. Create directory: `central-services-and-infrastructure/[service]/docker-compose.[service].yml`
2. Add to one of the startup scripts: `start_central-services.sh` or `slim_start_central-services.sh`
3. If needs centralized logging: mount `central-log-volume:/app/logs`
4. Integrate with Traefik if HTTP service: add labels for routing
5. Add healthcheck clause for Docker to monitor availability
6. Update install scripts if special initialization needed

**New Integration Feature (e.g., webhook handler):**
1. Add handler code in existing service container image (modify via submodule)
2. Create/update configuration file in `data/[service]/`
3. Add route mapping in `central-services-and-infrastructure/traefik/configs/` if needed
4. Add test case in `tests/3_tests_integration_full_stack/test_full_communication_route.py`
5. Verify correlation ID propagation for distributed tracing

**New Configuration:**
1. Create JSON/YAML in `kausalassist-apps/data/[service]/` or `central-services-and-infrastructure/[service]/`
2. Mount via volume in corresponding docker-compose file
3. Reference in container environment variables if dynamic
4. Document required fields and defaults in README or comments

**New Test:**
1. Unit tests: `tests/1_tests_build_only_core_required/test_unit_[service].py` (minimal dependencies)
2. API tests: `tests/1_tests_build_only_core_required/test_openapi_[service].py` (core service running)
3. Full stack tests: `tests/2_tests_build_full_stack_required/test_[feature].py` (Elastic + Sentry required)
4. Integration tests: `tests/3_tests_integration_full_stack/test_[scenario].py` (end-to-end flows)
5. Use `kausal_helpers.py` for shared API client classes and fixtures
6. Follow pattern: pytest fixtures for service clients, `@pytest.mark.order()` for sequencing if needed

## Special Directories

**central-log-volume (Docker Volume):**
- Purpose: Shared logging volume for all services and Filebeats collector
- Generated: Yes (created by `init_dockerstuff.sh`)
- Committed: No (Docker volume, ephemeral unless persisted)
- Contents: JSON-formatted logs from Traefik, application services, structured entries from Filebeats
- Access: Read via Filebeats container, analyzed in Kibana, queried via ElasticSearch API

**kausalassist-apps/data/** (Configuration and Runtime Data):**
- Purpose: Persistent configuration and cached data for all app services
- Generated: Partially (database backups are generated at runtime)
- Committed: YAML/JSON configs committed, binary files (.sqlite3, .pkl, .psql.bin) version-controlled or backed up separately
- Contents: Database dumps, cache files, configuration files, keystores, PKI certificates
- Mounting: Each service mounts relevant subdirectories as Docker volumes

**central-services-and-infrastructure/traefik/certs/** (SSL Certificates):**
- Purpose: TLS certificates for HTTPS endpoints
- Generated: Yes (via `create_certs.sh` script, not shown in repo)
- Committed: No (certificates are sensitive and generated per-deployment)
- Contents: Private keys (.pem, .key), certificate files, CA roots
- Access: Read-only mount to Traefik and services needing custom CA validation

**.gitlab-ci.yml Pipeline Artifact:**
- Purpose: Defines CI/CD stages and test execution phases
- Generated: No (manually maintained)
- Committed: Yes
- Stages: docker_prune → stack_build_test_slim → integration_tests → stack_build_test_full
- Artifacts: JUnit XML test reports, Docker logs

---

*Structure analysis: 2026-02-26*
