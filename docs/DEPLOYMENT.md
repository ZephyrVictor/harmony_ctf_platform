# GZCTF with Flag Scoring Module — Deployment Guide

## Production Deployment (docker-compose)

### Quick Start

```bash
# 1. Configure environment
cp .env.example .env
# Edit .env and set strong passwords

# 2. Build and start
docker compose up -d

# 3. Verify
curl http://localhost:8080/healthz
```

### Services

| Service | Port | Description |
|---------|------|-------------|
| gzctf | 8080 | Application server (ASP.NET Core) |
| db | 5432 (internal) | PostgreSQL 17 |
| redis | 6379 (internal) | Redis 7 cache |

### Database Migration

EF Core migrations run automatically on startup (`PrelaunchHelper.MigrateAsync()`).

### Environment Variables

| Variable | Description | Default |
|----------|-------------|---------|
| `DB_PASSWORD` | PostgreSQL password | `gzctf_pass` |
| `XOR_KEY` | Encryption key for sensitive data | `gzctf-xor-key` |

### Verify New Tables

After startup, connect to the database to verify the new tables were created:

```bash
docker compose exec db psql -U gzctf -d gzctf -c "\dt"
```

Expected new tables: `ChallengeFlags`, `ChallengePenalty`, `SubmissionRecords`, `Tickets`, `BigScreenEvents`, `CompetitionSettings`.

---

## Development Environment Setup

### Prerequisites

- .NET SDK 10.0
- Node.js 20+ with pnpm
- Docker (for PostgreSQL, Redis)

### 1. Start Infrastructure

```bash
docker run -d \
  --name gzctf-postgres \
  -e POSTGRES_USER=gzctf \
  -e POSTGRES_PASSWORD=gzctf_pass \
  -e POSTGRES_DB=gzctf \
  -p 5432:5432 \
  postgres:17-alpine

docker run -d \
  --name gzctf-redis \
  -p 6379:6379 \
  redis:7-alpine
```

### 2. Configure Connection

Create `src/GZCTF/appsettings.Development.json`:

```json
{
  "ConnectionStrings": {
    "Database": "Host=localhost;Port=5432;Database=gzctf;Username=gzctf;Password=gzctf_pass"
  }
}
```

### 3. Apply Migrations

```bash
export PATH="$HOME/.dotnet:$HOME/.dotnet/tools:$PATH"
cd src/GZCTF
dotnet ef database update
```

### 4. Run Backend

```bash
cd src/GZCTF
dotnet run
```

Backend: `http://localhost:8080`

### 5. Run Frontend (Development)

```bash
cd src/GZCTF/ClientApp
pnpm install
pnpm dev
```

Frontend dev server: `http://localhost:5173`

### 6. Production Build

```bash
cd src/GZCTF
dotnet publish -c Release -o out
```

---

## Competition Settings (Admin UI)

After login as admin, navigate to **Admin > Scoring** to configure:

- **Scoring.MinScore** — Minimum score (default 0)
- **Scoring.DefaultPenaltyScore** — Default wrong answer penalty per challenge
- **Scoring.MaxPenaltyPerSubmission** — Max penalty per single submission
- **Ticket.EnableAutoRefund** — Auto-refund penalty on approval
- **Ticket.DefaultRefundRatio** — Default refund ratio (1.0 = full refund)
- **Ticket.MaxTicketsPerSubmission** — Max tickets per submission
- **BigScreen.EnableBonusEvents** — Enable bonus event broadcasting

Per-challenge flag and penalty configuration: **Admin > Games > Challenges > Edit**.
