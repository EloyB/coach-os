# Secrets checklist

Two places to populate before the first deploy: **GitHub Actions secrets**
(used by workflows) and **Scaleway Secret Manager** (used by the running API).

## GitHub Actions secrets

GitHub → coach-os repo → Settings → Secrets and variables → Actions → New repository secret.

| Secret | Source | Notes |
|---|---|---|
| `SCALEWAY_SECRET_KEY` | Scaleway Console → IAM → API Keys (new one for `gha-deploy`) | `IAMReadOnly + RegistryFullAccess + RDBReadOnly + InstanceReadOnly + SecretManagerReadOnly + TransactionalEmailReadOnly` is sufficient |
| `SCALEWAY_ACCESS_KEY` | Same key as above | Access key half |
| `SCALEWAY_DEFAULT_ORGANIZATION_ID` | `tofu output -raw scw_organization_id_for_secrets` or Scaleway → Settings | UUID |
| `SCALEWAY_PROJECT_ID` | `tofu output -raw scw_project_id_for_secrets` | UUID |
| `SCALEWAY_ZONE` | Hardcode | `fr-par-1` |
| `SCALEWAY_INSTANCE_ID` | `tofu output -raw vps_instance_id` | UUID |
| `VPS_SSH_KEY` | Contents of the deploy key generated locally (see Phase 5 step 13 in the master plan) | **Private** half of the ed25519 keypair, `-----BEGIN OPENSSH PRIVATE KEY-----` … |

### How to add (one example)

```powershell
# Get the value
tofu -chdir=infrastructure/terraform output -raw vps_instance_id

# Copy to clipboard, then paste in GitHub UI as new secret SCALEWAY_INSTANCE_ID
```

Or via the gh CLI:
```powershell
gh secret set SCALEWAY_INSTANCE_ID --body "$(tofu -chdir=infrastructure/terraform output -raw vps_instance_id)"
```

## Scaleway Secret Manager

These are auto-created by `terraform/secrets.tf` with values either generated
(JWT key, DB password) or composed from other Terraform outputs (DB connection
string). The two SMTP entries below are **not** auto-created because Scaleway
TEM only issues SMTP credentials after DKIM/SPF DNS verification has propagated.

| Secret name | Auto? | How to populate |
|---|---|---|
| `DatabaseSettings__ConnectionString` | ✓ Terraform | — |
| `Jwt__Key` | ✓ Terraform | Rotate with `tofu apply -replace=random_password.jwt_key` |
| `Jwt__Issuer` | ✓ Terraform | — |
| `Jwt__Audience` | ✓ Terraform | — |
| `Jwt__ExpiryMinutes` | ✓ Terraform | — |
| `Frontend__Origin` | ✓ Terraform | — |
| `App__ConfirmationBaseUrl` | ✓ Terraform | — |
| `Email__SmtpHost` | ✓ Terraform | `smtp.tem.scw.cloud` |
| `Email__SmtpPort` | ✓ Terraform | `587` |
| `Email__FromAddress` | ✓ Terraform | `noreply@coach-os.be` |
| `Email__FromName` | ✓ Terraform | `CoachOS` |
| `Email__Username` | **manual** | After Scaleway TEM verifies the domain, generate API key with `TransactionalEmail` permission, copy to a new secret version |
| `Email__Password` | **manual** | The secret half of the same API key |
| `Mollie__WebhookBaseUrl` | ✓ Terraform | `https://app.<domain_name>` |
| `Mollie__ClientId` | **manual** | From my.mollie.com → Developers → OAuth applications |
| `Mollie__ClientSecret` | **manual** | Same place, shown once on creation |
| `Mollie__RedirectUri` | **manual** | `https://app.<domain_name>/api/oauth/mollie/callback` — must match Mollie dashboard exactly |
| `SuperAdmin__Email` | **manual** | Email of the first system-level super admin (promoted at API startup) |

### Manual TEM creds setup

```powershell
# Wait until `tofu show` reports tem_domain status = "checked" (DKIM verified)
# Then generate an IAM API key with TransactionalEmail permission (Scaleway Console)

scw secret secret create name=Email__Username project-id=$PROJECT_ID
scw secret version create secret-id=<id-from-above> data="<smtp-username>"

scw secret secret create name=Email__Password project-id=$PROJECT_ID
scw secret version create secret-id=<id-from-above> data="<smtp-password>"

# Restart the API container to pick up new secrets
ssh root@$VPS_IP "cd /app && docker compose restart api"
```

## Rotation playbook

| Secret | Rotation cadence | Command |
|---|---|---|
| Scaleway API key | 90 days | New key in Scaleway Console → update `SCALEWAY_*` GitHub secrets → revoke old key |
| `VPS_SSH_KEY` | After every team change | `ssh-keygen` new pair → push pubkey to VPS `~/.ssh/authorized_keys` → swap `VPS_SSH_KEY` GitHub secret → remove old pubkey from VPS |
| `Jwt__Key` | 180 days (forces all users to re-login) | `tofu apply -replace=random_password.jwt_key` → restart api |
| Managed Postgres password | Yearly | `tofu apply -replace=random_password.db` → restart api (connection string secret regenerated automatically) |
| TEM SMTP creds | When compromised | Revoke API key in Scaleway → generate new → update `Email__Username` + `Email__Password` secret versions → restart api |

## Verification

After all secrets are in place:

```powershell
# GitHub side — list configured secret names (values not visible)
gh secret list

# Scaleway side
scw secret secret list project-id=$PROJECT_ID
```

You should see all 7 GitHub secrets and 13 Scaleway secrets (11 from Terraform
+ 2 manual SMTP creds).
