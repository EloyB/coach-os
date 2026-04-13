# coach-os — Infrastructure

Production infrastructure-as-code for [coach-os.be](https://coach-os.be),
deployed on a Scaleway VPS in a dedicated Scaleway organization.

## Layout

```
infrastructure/
├── terraform/           # OpenTofu — provisions VPS, IP, registry, PG, TEM, secrets, DNS
├── docker-compose.yml   # Production compose deployed to /app on the VPS
├── nginx/
│   ├── nginx.conf       # Global config (gzip, rate-limit zones, log format)
│   └── conf.d/
│       └── coach-os.conf  # Vhost: HTTP→HTTPS, /api/* → api:8080, /* → frontend:3000
├── README.md            # this file
└── SECRETS.md           # checklist of secrets to add to GitHub + Scaleway

../scripts/bootstrap-vps.sh  # one-off VPS setup (Docker, ufw, fail2ban, certbot)

../.github/workflows/
├── backend-build-push.yml   # test → build → push → migrate → ssh+restart
├── frontend-build-push.yml  # build → push → ssh+restart
└── deploy-infra.yml         # ssh+sync compose + nginx
```

Application code is at `../backend/` and `../frontend/`. The local-dev compose
at the repo root is unrelated to this folder.

## Architecture

```
Internet
  │
  └─ coach-os.be ──→ Scaleway VPS (single instance)
                      │
                      └─ nginx:alpine (host :80, :443) — Let's Encrypt SSL
                           ├─ /api/*   → coach-os-api      (internal :8080)
                           └─ /*       → coach-os-frontend (internal :3000)

Scaleway Managed Postgres (separate service) ◄─── api connects via private endpoint
Scaleway Container Registry (rg.fr-par.scw.cloud/coach-os/{api,frontend})
Scaleway Secret Manager (DB string, JWT key, SMTP creds — read by api at startup)
Scaleway Transactional Email (SMTP for outbound mail)
```

All app containers communicate via a Docker bridge network (`coach-os-network`).
Only nginx exposes ports on the host.

## Deploy flow

| Trigger | Workflow | What happens |
|---|---|---|
| Push to `main` ∩ `infrastructure/docker-compose.yml` or `infrastructure/nginx/**` | `deploy-infra.yml` | SSH to VPS, backup, sync compose+nginx, validate, restart |
| Push to `main` ∩ `backend/**` (excl. Scripts/, *.md) | `backend-build-push.yml` | Test → build → push → migrate-db → SSH pull-and-restart |
| Push to `main` ∩ `frontend/**` (excl. e2e, *.md, dev Dockerfile) | `frontend-build-push.yml` | Build (Dockerfile.prod) → push → SSH pull-and-restart |

All workflows are also `workflow_dispatch`-able from the GitHub Actions UI.

## First-time setup

1. Provision Scaleway resources via OpenTofu — see `terraform/README.md`
2. Bootstrap the VPS — `bash scripts/bootstrap-vps.sh` on the new instance
3. Add GitHub + Scaleway Secret Manager values — see `SECRETS.md`
4. Push to `main` — first deploy fires, all three workflows run

## Day-2 ops

```bash
# SSH to VPS
ssh root@$(cd infrastructure/terraform && tofu output -raw vps_ip)

# Inspect
cd /app
docker compose ps
docker compose logs -f api
docker compose logs -f frontend

# Manual rollback to last backup
ls backups/
cp backups/docker-compose.yml.<TIMESTAMP> docker-compose.yml
cp -r backups/nginx.<TIMESTAMP>/* nginx/
docker compose up -d

# Restart a single service after secret rotation
docker compose restart api

# Check certbot
certbot certificates
certbot renew --dry-run
```

## Rollback strategy

- **Compose / nginx config:** automatic rollback inside the deploy job if validation fails; manual rollback from `/app/backups/`
- **API image:** `docker compose pull api && docker compose up -d --no-deps api` after pinning a known-good tag in `docker-compose.yml` (replace `:latest` with `:main-<sha>`)
- **DB migration:** `dotnet ef migrations remove` locally, push the revert, the next deploy re-runs migrations downward. **Risky** for destructive migrations — take a `pg_dump` first.
- **Full infra reset:** `cd terraform && tofu destroy && tofu apply` (loses the DB; use only with a fresh PG dump on hand)

## Cost estimate (€/mo, prod)

| Item | Cost |
|---|---|
| VPS (Stardust S1, 1 vCPU / 1 GB RAM) | ~€4 |
| Reserved IP | ~€1 |
| Managed Postgres (DB-DEV-S, 10 GB) | ~€12 |
| Container Registry (storage-billed) | ~€0–2 |
| Transactional Email (~1k mails/mo) | ~€0–1 |
| Secret Manager (~10 secrets) | ~€0 |
| Object Storage (TF state, <1 GB) | ~€0 |
| **Total** | **~€18–20** |

Bump VPS to DEV1-S (~€11) once you're past ~10 active organizations.
