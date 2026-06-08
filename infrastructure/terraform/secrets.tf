# Production secrets stored in Scaleway Secret Manager.
# The running API reads these via LodeKennes.Extensions.Scaleway.SecretManager
# (see ConfigurationExtensions.ConfigureAppConfiguration).

# ── Generated secrets ────────────────────────────────────────────────────────

resource "random_password" "jwt_key" {
  length  = 64
  special = false # Easier to round-trip through shells; 64 hex chars = 256 bits of entropy
}

# ── DB connection string (composed from the managed PG outputs) ──────────────

locals {
  db_connection_string = join(";", [
    "Host=${scaleway_rdb_instance.coach_os.load_balancer.0.ip}",
    "Port=${scaleway_rdb_instance.coach_os.load_balancer.0.port}",
    "Database=${scaleway_rdb_database.coach_os.name}",
    "Username=${scaleway_rdb_instance.coach_os.user_name}",
    "Password=${random_password.db.result}",
    "SslMode=Require",
  ])

  # Each entry becomes a Scaleway secret. Key format matches ASP.NET Core
  # hierarchical config (double underscore between sections) so the app can
  # read them unchanged from any provider (env var, secret manager, etc).
  secrets = {
    "DatabaseSettings__ConnectionString" = local.db_connection_string
    "Jwt__Key"                           = random_password.jwt_key.result
    "Jwt__Issuer"                        = var.jwt_issuer
    "Jwt__Audience"                      = var.jwt_audience
    "Jwt__ExpiryMinutes"                 = tostring(var.jwt_expiry_minutes)
    # The CoachOS app is served on app.<domain_name> (the apex hosts the
    # marketing website). CORS origin and confirmation redirect URL must
    # therefore both target the app subdomain.
    "Frontend__Origin"         = "https://app.${var.domain_name}"
    "App__ConfirmationBaseUrl" = "https://app.${var.domain_name}/confirmation"
    "Email__SmtpHost"          = "smtp.tem.scw.cloud"
    "Email__SmtpPort"          = "587"
    "Email__FromAddress"       = var.smtp_from_address
    "Email__FromName"          = "CoachOS"
    # Email__Username and Email__Password come from Scaleway TEM after domain
    # verification. Set them manually via `scw secret secret-version create`
    # or re-run `tofu apply` after filling in the `tem_smtp_*` variables.

    # Mollie Connect — base URL where Mollie sends webhooks back to the API.
    # The actual webhook path is /api/webhooks/mollie (handled by the API).
    # ClientId and ClientSecret are manually filled (see resources below).
    "Mollie__WebhookBaseUrl" = "https://app.${var.domain_name}"
  }
}

resource "scaleway_secret" "app" {
  for_each    = local.secrets
  project_id  = var.scw_project_id
  region      = var.scw_region
  name        = each.key
  description = "CoachOS runtime configuration — managed by Terraform"
  tags        = ["coach-os"]
}

resource "scaleway_secret_version" "app" {
  for_each  = local.secrets
  secret_id = scaleway_secret.app[each.key].id
  data      = each.value
}

# ── Manually-filled secrets ──────────────────────────────────────────────────
# These secrets are created by Terraform but their value MUST be filled in
# manually via the Scaleway console (or `scw secret secret-version create`).
# Reason: they reference identities (e-mail, person) that should never live in
# version control.

# SuperAdmin__Email — promoot bij API-startup de bijbehorende user tot system-level
# super admin (zie SuperAdminBootstrapHostedService). Vul deze waarde manueel in
# zodat de e-mail van de eerste super admin niet in git terechtkomt.
resource "scaleway_secret" "super_admin_email" {
  project_id  = var.scw_project_id
  region      = var.scw_region
  name        = "SuperAdmin__Email"
  description = "E-mail van de eerste system-level super admin — manueel ingevuld"
  tags        = ["coach-os", "manual"]
}

# Mollie Connect OAuth credentials — manueel ingevuld na het aanmaken van het
# Mollie partner-account en het activeren van Mollie Connect in het Mollie
# dashboard. De redirect URI die in het Mollie dashboard moet worden ingesteld
# is `https://app.<domain_name>/api/oauth/mollie/callback`.
# Vul de waarden in via:
#   scw secret secret-version create <secret_id> data=<value>
# of via de Scaleway console.
resource "scaleway_secret" "mollie_client_id" {
  project_id  = var.scw_project_id
  region      = var.scw_region
  name        = "Mollie__ClientId"
  description = "Mollie Connect OAuth client id — manueel ingevuld na Mollie onboarding"
  tags        = ["coach-os", "manual", "mollie"]
}

resource "scaleway_secret" "mollie_client_secret" {
  project_id  = var.scw_project_id
  region      = var.scw_region
  name        = "Mollie__ClientSecret"
  description = "Mollie Connect OAuth client secret — manueel ingevuld na Mollie onboarding"
  tags        = ["coach-os", "manual", "mollie"]
}
