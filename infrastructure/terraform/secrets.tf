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
    "Host=${scaleway_rdb_instance.coach_os.endpoint_ip}",
    "Port=${scaleway_rdb_instance.coach_os.endpoint_port}",
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
