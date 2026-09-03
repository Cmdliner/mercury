# Mercury

A merchant operating platform for Nigerian pharmacy chains — payments, inventory, and settlement in one system, so an owner who isn't physically present at any store can still trust what the numbers say.

## The problem

A pharmacy chain owner in Nigeria runs multiple store locations. Each store takes payments through several channels (bank transfer, POS card, cash), tracks its own inventory, and reports up to an owner who isn't on-site day to day. Today that owner is stitching together a POS app, Excel, and WhatsApp — with no single, trustworthy view of what's actually happening across the business, and no reliable way to catch a missing settlement or a mismatched transaction until it's already cost money.

Mercury is a platform that gives that owner one system: accept payments across every store, keep inventory accurate in real time, and get correctly reconciled financial data without manually cross-checking three different tools.

**This is a portfolio/learning project**, not a live product — the goal is to build something with genuine fintech-grade engineering depth (a real double-entry ledger, correct settlement and reconciliation logic, production-shaped architecture) as proof of backend engineering capability for the Nigerian fintech job market.

## Architecture

Two languages, split by what each is good at — not split for its own sake:

- **C# / ASP.NET Core (`core/`)** — the business brain. Ledger, merchants, auth, payments, and (later) fraud rules, settlement, and reporting all live here.
- **Go (`pos-gateway/`)** — device-facing, high-concurrency work: POS terminal connections, heartbeats, offline transaction sync. Not yet started (planned for Milestone 5).

```
merchant-platform/
├── core/                          # .NET solution — the business brain
│   ├── Mercury.sln
│   ├── Mercury.Api/                # HTTP layer: controllers, auth, DbContext (AppDbContext)
│   ├── Mercury.Ledger/             # domain: double-entry ledger, no HTTP/DB-provider awareness
│   ├── Mercury.Merchants/          # domain: Merchant, Store, Staff
│   └── Mercury.Tests/              # xUnit, SQLite in-memory
├── pos-gateway/                    # Go service — not yet started
└── docker-compose.yml              # Postgres + Redis (local dev)
```

**A hard rule enforced by project references, not just convention:** `Mercury.Ledger` and `Mercury.Merchants` never reference `Mercury.Api`. Domain logic doesn't know HTTP or EF Core's specific provider exists — `Mercury.Api` depends on the domain projects, never the reverse. This is what keeps the ledger testable in isolation and reusable outside the web API if needed later.

## Design principles this codebase follows

- **The ledger is append-only.** No `JournalEntry` is ever updated or deleted. Mistakes are corrected with a compensating reversal entry; refunds are their own event type. Both preserve full history — nothing is ever silently erased.
- **Every journal entry is balanced by construction.** `JournalEntry.Create(...)` is the only way to construct one, and it throws if debits ≠ credits across its lines — an unbalanced entry is not a state the type system allows to exist.
- **Account balances are sign-normalized by account type.** `Asset`/`Expense` accounts read as `debits − credits`; `Liability`/`Equity`/`Revenue` accounts read as `credits − debits` — callers get an intuitive positive number regardless of which side an account normally grows on.
- **Enums are stored as strings in Postgres, never as native DB enums or raw integers** — adding a new value later never requires a schema migration or risks reinterpreting existing rows.
- **No repository layer.** Services (`LedgerService`, `AuthService`) call `AppDbContext`/EF Core directly. A repository abstraction isn't earning its cost here — EF Core isn't getting swapped out, and SQLite in-memory already gives fast, realistic tests.
- **Secrets never live in committed files.** Local dev uses `dotnet user-secrets`; production uses environment variables via an untracked `.env`. `appsettings*.json` files are safe to commit precisely because they hold no secrets.

## Current status

### ✅ Milestone 0 — Foundations
Product definition, ledger account model (on paper), repo/solution scaffolding, dependency direction locked in.

### ✅ Milestone 1 — Ledger core
- `Account`, `JournalEntry`, `JournalLine` entities (`Mercury.Ledger`), EF Core + Postgres, snake_case naming convention, string-stored enums, `decimal(18,2)` precision.
- `LedgerService`: `PostSaleAsync`, `PostRefundAsync`, `PostReversalAsync`, `GetAccountBalanceAsync`, `GetAccountBalancesAsync` (batched).
- Sign-aware balance calculation via `AccountType.NormalBalanceSide()`.
- xUnit test suite against SQLite in-memory covering: balanced-entry enforcement, multi-line (fee-split) entries, sale → refund correctness, sale → reversal correctness.

### 🔄 Milestone 2 — Merchants, auth, and one real payment (in progress)
- `Merchant`, `Store`, `Staff` entities (`Mercury.Merchants`) — `Staff` scoped to a `Merchant` always, to a `Store` only for Manager/Cashier roles (Owner is unscoped), enforced via a `Staff.Create()` factory.
- ASP.NET Core Identity (`IdentityUser<Guid>`, `IdentityRole<Guid>`) merged into `AppDbContext`; Identity handles authentication only — authorization is driven entirely by the domain's own `StaffRole`, not Identity's role system.
- JWT bearer auth: `TokenService` issues tokens carrying `role`, `merchant_id`, and (when applicable) `store_id` as claims.
- `AuthController` (thin) → `AuthService` (registration + login logic) → `UserManager`/`AppDbContext`.
- **Not yet done:** `LoginAsync` implementation, Paystack sandbox integration, Redis-backed idempotency on payment webhooks, `[Authorize]`-protected endpoints, minimal Next.js dashboard.

### ⬜ Milestone 3 — Inventory + stores
### ⬜ Milestone 4 — Settlement engine
### ⬜ Milestone 5 — POS Gateway (Go)
### ⬜ Milestone 6 — Async messaging + notifications (RabbitMQ, Go notification service)
### ⬜ Milestone 7 — Reconciliation
### ⬜ Milestone 8 — Observability (structured logging, Prometheus/Grafana, OpenTelemetry)
### ⬜ Milestone 9 — Deployment + hardening (Docker Compose on a single VPS, CI/CD, load testing)

## Deliberately deferred / scoped down

To keep this buildable by one person in a reasonable timeframe, some commonly-suggested "enterprise" technologies are intentionally **not** part of the core plan:

- **Kubernetes** — Docker Compose on one VPS is the actual deployment target. K8s would be a separate exercise, not a dependency.
- **Kafka** — RabbitMQ covers the same async-messaging learning (queues, retries, dead-letter) with far less operational overhead for a solo project.
- **CQRS / event sourcing everywhere** — applied to the ledger only, where the rigor is actually earned. Everything else is plain CRUD with clean service boundaries.
- **A dedicated secret manager** (Key Vault, Vault) — an untracked `.env` file is the right-sized solution at this scale; a secret manager is a legitimate later add-on, not a current requirement.

## Getting started

```bash
git clone <repo>
cd merchant-platform
docker compose up -d          # Postgres + Redis

cd core/Mercury.Api
dotnet user-secrets set "ConnectionStrings:AppDb" "Host=localhost;Port=5432;Database=mercury;Username=mercury;Password=mercury_dev"
dotnet user-secrets set "Jwt:SigningKey" "<your own local dev key, 32+ chars>"
dotnet ef database update
dotnet run
```

## Next steps

1. Finish `AuthService.LoginAsync` and confirm the full register → login → JWT round trip.
2. Add `[Authorize]` to a protected endpoint and confirm role/merchant/store claims are readable from `HttpContext.User`.
3. Integrate Paystack sandbox: initiate payment → webhook → signature verification → `LedgerService.PostSaleAsync`.
4. Add Redis-backed idempotency so a retried webhook can't double-post a sale.
5. Minimal Next.js dashboard: login, view today's transactions — proves the chain works end to end, closing out Milestone 2.