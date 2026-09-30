# Mercury

A high-integrity merchant operating platform engineered for multi-location Nigerian retail and pharmacy chains. Mercury unifies omnichannel payment collection, store-level double-entry ledger bookkeeping, role-based staff provisioning, and automated reconciliation into a single verifiable system.

---

## 1. Executive Summary & Problem Context

Operating multi-location pharmacy and retail businesses across Nigeria introduces acute operational and financial friction:
- **Disparate Payment Silos**: Transactions arrive across fragmented channels (POS terminal card payments, dynamic bank transfers, and cash tills) without consolidated verification.
- **Settlement Lag & Reconciliation Gaps**: Bank and gateway settlements are detached from point-of-sale events. Business owners who are not physically on-site struggle to audit real cash flow against reported sales.
- **Auditability Vulnerabilities**: Traditional systems overwrite database rows or calculate balances with mutable single-entry tallies, leaving books susceptible to untracked adjustments or staff fraud.

### Core Value Proposition
Mercury replaces fragmented tooling (POS terminals, spreadsheets, chat channels) with an **immutable, double-entry financial ledger** and **multi-tenant payment pipeline**. Every transaction is guaranteed to be balanced, auditable, and traceable from initial payment collection to final ledger posting.

---

## 2. System Architecture & Topology

Mercury is architected around clean domain separation, explicit dependency direction, and technology specialization:

```
merchant-platform/
├── core/                                 # .NET 10 Solution — Core Business Brain
│   ├── Mercury.slnx                      # Modern Solution definition
│   ├── Mercury.Api/                      # HTTP/REST host, Auth, DbContext, Webhooks, DI
│   ├── Mercury.Ledger/                   # Domain: Double-entry ledger, accounts, posting logic
│   ├── Mercury.Merchants/                # Domain: Merchants, Stores, Staff hierarchy & RBAC
│   ├── Mercury.Payments/                 # Domain & Adapters: Multi-provider payment gateway
│   └── Mercury.Tests/                    # xUnit unit & integration test suite (SQLite in-memory)
├── pos-gateway/                          # Go Service (Planned) — High-concurrency POS terminal sync
└── docker-compose.yml                    # Infrastructure: PostgreSQL 16 + Redis 7
```

### Architectural Dependency Invariant

```
               ┌───────────────────────────────┐
               │          Mercury.Api          │
               │   (ASP.NET Core 10 HTTP Host) │
               └───────┬───────────────┬───────┘
                       │               │
        ┌──────────────┴──────┐ ┌──────┴───────────────┐
        ▼                     ▼ ▼                      ▼
┌───────────────┐     ┌─────────────────┐     ┌────────────────┐
│Mercury.Ledger │     │Mercury.Merchants│     │Mercury.Payments│
│(Pure Domain)  │     │  (Pure Domain)  │     │(Provider Abstr)│
└───────────────┘     └─────────────────┘     └────────────────┘
```

> **Strict Dependency Rule**: `Mercury.Ledger`, `Mercury.Merchants`, and `Mercury.Payments` never reference `Mercury.Api`. Core domain entities and accounting logic are completely decoupled from HTTP frameworks, controller models, and infrastructure providers.

---

## 3. Architecture Decision Records (ADRs)

### ADR-001: Immutable Double-Entry Ledger Engine
- **Decision**: Financial state is never stored as a mutable balance. All financial state transitions are recorded as append-only `JournalEntry` and `JournalLine` pairs.
- **Invariant**: `JournalEntry.Create(...)` enforces mathematical balance (`∑ Debits == ∑ Credits`) at construction time. It is impossible to instantiate or persist an unbalanced entry in the domain.
- **Auditability**: Records are never updated or deleted. Corrections are applied exclusively through compensating reversal entries (`PostReversalAsync`) or explicit refund events (`PostRefundAsync`).

### ADR-002: Sign-Normalized Account Balances
- **Decision**: Normal account balances vary based on classification. To eliminate caller ambiguity and prevent sign errors in UI/reporting layers, account balances are dynamically normalized using `AccountType.NormalBalanceSide()`:
  - **Asset / Expense**: $\text{Balance} = \sum \text{Debits} - \sum \text{Credits}$
  - **Liability / Equity / Revenue**: $\text{Balance} = \sum \text{Credits} - \sum \text{Debits}$
- **Outcome**: Returns intuitive, positive financial figures for standard account states across all account categories.

### ADR-003: Decoupled Multi-Tenant Identity & Domain RBAC
- **Decision**: ASP.NET Core Identity (`IdentityUser<Guid>`, `IdentityRole<Guid>`) is utilized strictly for credential management and password hashing, while authorization logic is owned entirely by the domain's `StaffRole` hierarchy:
  - `Owner`: Tenant-wide scope; manages merchant entities, stores, and registers staff.
  - `Manager`: Scoped to a specific physical `Store`; oversees branch transactions and cash tills.
  - `Cashier`: Scoped to a specific physical `Store`; initiates point-of-sale payment requests.
- **Token Claims**: JWT tokens encode `merchant_id`, `role`, and `store_id` (when applicable), allowing authorization gates to evaluate multi-tenant boundaries without redundant database queries.

### ADR-004: Store Account Isolation & Dynamic Provisioning
- **Decision**: Every physical store provisions dedicated ledger accounts (`STORE-{storeId}-PENDING`, `STORE-{storeId}-REVENUE`, `STORE-{storeId}-REFUNDS`) via `AccountProvisioningService`.
- **Benefit**: Prevents cross-store ledger contamination and provides real-time per-store balance sheets and P&L auditing.

### ADR-005: Multi-Provider Payment Gateway Architecture
- **Decision**: Payment providers (Paystack, Nomba) are encapsulated behind a unified `IPaymentCollector` interface dispatched via `PaymentCollectorFactory`.
- **Resilience**: Upstream provider rejections are normalized into `PaymentProviderException` carrying provider status codes and raw downstream error messages.

### ADR-006: Two-Tier Idempotency Architecture
- **Decision**: Distributed financial systems must guarantee strictly once-only execution for payments and webhooks.
  1. **API Tier (Checkout Initialization)**: `PaymentRequest.IdempotencyKey` backed by a unique database constraint ensures multiple client submissions with the same key return the existing checkout session without duplicate charges.
  2. **Webhook Tier (Event Processing)**: Distributed Redis key `webhook:{providerReference}` with a 24-hour TTL and `When.NotExists` concurrency locking prevents double-posting to the ledger upon duplicate webhook retries.

### ADR-007: Constant-Time HMAC Signature Verification
- **Decision**: Webhook security uses HMAC-SHA512 verification executed via `CryptographicOperations.FixedTimeEquals`.
- **Security Rationale**: Standard string comparison fails fast on mismatched characters, leaking timing information that can be exploited in timing attacks. Constant-time byte comparisons close this vulnerability.

### ADR-008: Centralized RFC 7807 Problem Details Error Handling
- **Decision**: Implement ASP.NET Core `IExceptionHandler` (`GlobalExceptionHandler`) to translate unhandled exceptions into RFC 7807 `ProblemDetails` payloads containing correlation `traceId`, standard HTTP status codes, and sanitised developer diagnostics.

### ADR-009: Direct Entity Framework Core Interaction (No Repository Pattern)
- **Decision**: Domain services (`LedgerService`, `AuthService`, `AccountProvisioningService`) interact directly with `AppDbContext` and `DbSet<T>`.
- **Rationale**: EF Core is already a comprehensive unit-of-work and repository abstraction. Additional generic repository layers add friction, obscure query optimizations, and offer zero utility for a single-database architecture.

---

## 4. End-to-End Request & Payment Flows

### Flow A: Merchant Onboarding & Store Provisioning
1. `POST /api/v1/auth/register/merchant` → Creates `IdentityUser`, seeds `Merchant`, creates tenant `Owner` staff member, and returns signed JWT.
2. `POST /api/v1/merchant/stores` (Authenticated Owner) → Creates `Store` linked to `MerchantId`.
3. `POST /api/v1/auth/register/staff` (Authenticated Owner) → Registers `Manager` or `Cashier` assigned to the target `StoreId`.

### Flow B: Store Payment Initialization & Upstream Collection
```
[ Cashier Client ] 
        │
        │ 1. POST /api/v1/payments/initialize (Amount, Provider, IdempotencyKey)
        ▼
[ PaymentController ]
        │
        │ 2. Validate JWT claims (merchant_id, store_id)
        │ 3. Check IdempotencyKey (return existing if present)
        │ 4. Persist PaymentRequest (Status: Pending)
        ▼
[ PaymentCollectorFactory ] ──► [ PaystackCollector / NombaCollector ]
                                            │
                                            │ 5. POST upstream initialize
                                            ▼
                                  [ Payment Gateway ]
```

### Flow C: Webhook Verification & Immutable Ledger Posting
```
[ Payment Gateway ] 
        │
        │ 1. POST /api/v1/webhooks/paystack (Raw Payload + HMAC Signature)
        ▼
[ WebhookController ]
        │
        │ 2. Constant-time HMAC signature verification (Reject 401 if invalid)
        │ 3. Redis distributed lock: SETNX "webhook:{providerReference}" (TTL: 24h)
        │    └─► If already processed, return 200 OK immediately
        │ 4. Retrieve PaymentRequest & Resolve Store Accounts (-PENDING, -REVENUE)
        ▼
[ LedgerService.PostSaleAsync ]
        │
        │ 5. Create balanced JournalEntry:
        │    - DEBIT:  Store Pending Settlement (Asset)
        │    - CREDIT: Store Sales Revenue (Revenue)
        │ 6. Mark PaymentRequest as Successful
        ▼
[ PostgreSQL Database (ACID Commit) ]
```

---

## 5. Ledger Double-Entry Rules

| Event Type | Debit Account | Credit Account | Domain Purpose |
|---|---|---|---|
| **Sale (Card / POS / Transfer)** | `STORE-{id}-PENDING` *(Asset)* | `STORE-{id}-REVENUE` *(Revenue)* | Recognizes revenue; establishes pending receivable from payment provider. |
| **Cash Sale** | `STORE-{id}-CASH-TILL` *(Asset)* | `STORE-{id}-REVENUE` *(Revenue)* | Recognizes revenue; establishes immediate physical cash in till. |
| **Refund** | `STORE-{id}-REFUNDS` *(Expense)* | `STORE-{id}-PENDING` *(Asset)* | Records refund expense; reduces pending settlement asset. |
| **Reversal (Correction)** | Mirror original Credit account | Mirror original Debit account | Full compensating entry neutralizing erroneous transaction. |

---

## 6. Milestone Delivery Matrix

| Milestone | Scope & Deliverables | Status |
|---|---|---|
| **Milestone 0: Foundations** | Solution scaffolding, clean project dependency boundaries, EF Core PostgreSQL configuration. | ✅ Complete |
| **Milestone 1: Core Double-Entry Ledger** | Append-only `JournalEntry` / `JournalLine`, sign-normalized balances, SQLite in-memory test suite. | ✅ Complete |
| **Milestone 2: Merchant Hierarchy, Auth & Payments** | Merchant/Store/Staff domain model, JWT claims-based RBAC, Paystack integration, Redis-backed webhook idempotency, Global exception handling. | ✅ Complete |
| **Milestone 3: Store Inventory & Stock Control** | SKU catalog, multi-store stock levels, batch tracking, low-stock alerts. | ⬜ Planned |
| **Milestone 4: Settlement & Payout Engine** | Provider settlement ingestion, net payout calculations, fee deductions, bank transfer dispatch. | ⬜ Planned |
| **Milestone 5: POS Terminal Gateway (Go)** | Ultra-low latency TCP/TLS connection handler for physical Android/Linux POS terminals. | ⬜ Planned |
| **Milestone 6: Asynchronous Messaging & Events** | RabbitMQ broker, dead-letter queues, event-driven receipt and notification delivery. | ⬜ Planned |
| **Milestone 7: Financial Reconciliation Engine** | Automated 3-way matching (POS transactions vs. Gateway settlements vs. Bank statements). | ⬜ Planned |
| **Milestone 8: Observability & Telemetry** | OpenTelemetry tracing, Prometheus metrics, structured Serilog logging, Grafana dashboards. | ⬜ Planned |
| **Milestone 9: Production Hardening & CI/CD** | Multi-stage Docker builds, Compose production topology, GitHub Actions pipeline, load tests. | ⬜ Planned |

---

## 7. Local Development & Setup Guide

### Prerequisites
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Docker Engine & Compose](https://www.docker.com/)

### 1. Start Infrastructure Dependencies
```bash
# Launch PostgreSQL and Redis containers
docker compose up -d
```

### 2. Configure Local Secrets
```bash
cd core/Mercury.Api

# Set database connection string
dotnet user-secrets set "ConnectionStrings:Mercury" "Host=localhost;Port=5432;Database=mercury;Username=mercury;Password=mercury_dev"

# Set JWT authentication secrets
dotnet user-secrets set "Jwt:SigningKey" "a_very_secure_development_signing_key_32_characters_long_min!"
dotnet user-secrets set "Jwt:Issuer" "MercuryApi"
dotnet user-secrets set "Jwt:Audience" "MercuryClients"

# Configure Payment Provider Sandboxes
dotnet user-secrets set "Paystack:SecretKey" "sk_test_your_paystack_secret_key"
dotnet user-secrets set "Paystack:BaseUrl" "https://api.paystack.co"
dotnet user-secrets set "Nomba:ClientId" "your_nomba_client_id"
dotnet user-secrets set "Nomba:ClientSecret" "your_nomba_client_secret"
dotnet user-secrets set "Nomba:AccountId" "your_nomba_account_id"
dotnet user-secrets set "Nomba:BaseUrl" "https://api.nomba.com"
```

### 3. Run Database Migrations
```bash
dotnet ef database update
```

### 4. Build & Run Test Suite
```bash
cd ../../core
dotnet test
```

### 5. Launch Application
```bash
cd Mercury.Api
dotnet run
```
The API OpenAPI documentation will be available at `https://localhost:5001/openapi/v1.json` or through Swagger UI in development mode.
