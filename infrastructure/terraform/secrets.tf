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

    # Platform-billing (clubs betalen hun CoachOS-abonnement aan ons): webhooks van
    # ons eigen Mollie-account komen op /api/webhooks/mollie-billing. De API-key
    # zelf is een manuele secret (zie mollie_billing_api_key hieronder).
    "MollieBilling__WebhookBaseUrl" = "https://app.${var.domain_name}"
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

# Mollie platform-billing API-key — de statische key van het EIGEN Mollie-account
# van Studio Swyft (eerst test_..., later live_...), los van de Connect-credentials.
# Zolang deze secret leeg is, draait de API met een fake-client (geen echte betalingen).
# Op dat Mollie-account moeten SEPA-domiciliëring en kaart voor recurring actief zijn.
# Vul in via:
#   scw secret secret-version create <secret_id> data=<test_of_live_key>
resource "scaleway_secret" "mollie_billing_api_key" {
  project_id  = var.scw_project_id
  region      = var.scw_region
  name        = "MollieBilling__ApiKey"
  description = "Mollie API-key van het eigen platformaccount (abonnementen) — manueel ingevuld"
  tags        = ["coach-os", "manual", "mollie"]
}

# Billit — facturatie van CoachOS-abonnementen. In tegenstelling tot de manuele secrets
# hierboven maakt Terraform hier meteen een eerste versie aan met de placeholder "NOT_SET":
# een secret zonder versie laat de API crashen bij het opstarten (zie SECRETS.md). De API
# behandelt "NOT_SET" als leeg, dus tot de echte waarde ingevuld is draait de fake-facturatie
# en blijft self-service afrekenen uit. Vul de echte waarde in als NIEUWE versie:
#   scw secret version create secret-id=<secret_id> data=<waarde>
# Terraform negeert latere wijzigingen (ignore_changes), dus `tofu apply` overschrijft niets.
locals {
  billing_placeholder_secrets = {
    "Billit__ApiKey"  = "API-key uit MyBillit (Mijn profiel) van het facturerende bedrijf"
    "Billit__PartyId" = "Id van het facturerende bedrijf in Billit (URL van Mijn bedrijf)"
  }
}

resource "scaleway_secret" "billing_placeholder" {
  for_each    = local.billing_placeholder_secrets
  project_id  = var.scw_project_id
  region      = var.scw_region
  name        = each.key
  description = "${each.value} — manueel ingevuld (start als NOT_SET)"
  tags        = ["coach-os", "manual", "billit"]
}

resource "scaleway_secret_version" "billing_placeholder" {
  for_each  = local.billing_placeholder_secrets
  secret_id = scaleway_secret.billing_placeholder[each.key].id
  data      = "NOT_SET"

  lifecycle {
    ignore_changes = [data]
  }
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

# Mollie__RedirectUri — overschrijft de runtime-afgeleide redirect URI
# (HttpContext.Request.Scheme + Host). Nodig achter de nginx-proxy omdat
# ASP.NET anders `http://` ziet ipv `https://` en Mollie de redirect exact
# matcht. Vul in via Scaleway console of:
#   scw secret secret-version create <secret_id> \
#     data="https://app.<domain_name>/api/oauth/mollie/callback"
# De waarde MOET letterlijk overeenkomen met wat in het Mollie dashboard
# als redirect URI staat geregistreerd.
resource "scaleway_secret" "mollie_redirect_uri" {
  project_id  = var.scw_project_id
  region      = var.scw_region
  name        = "Mollie__RedirectUri"
  description = "Mollie Connect OAuth redirect URI — manueel ingevuld, moet exact matchen met Mollie dashboard"
  tags        = ["coach-os", "manual", "mollie"]
}
