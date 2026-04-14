provider "scaleway" {
  organization_id = var.scw_organization_id
  project_id      = var.scw_project_id
  region          = var.scw_region
  zone            = var.scw_zone
}

# ── SSH key ────────────────────────────────────────────────────────────────
# The "gha-deploy" SSH key is registered manually in the Scaleway console
# (Project → SSH Keys) because the IAM application lacks SSH-key write
# permission. Scaleway automatically attaches all project SSH keys to any
# instance booted in the project, so no terraform reference is needed.

# ── Reserved public IP (stable across VPS rebuilds) ──────────────────────────

resource "scaleway_instance_ip" "vps" {
  project_id = var.scw_project_id
  type       = "routed_ipv4"
}

# ── Security group (22/80/443 inbound; all outbound) ─────────────────────────

resource "scaleway_instance_security_group" "vps" {
  name                    = "coach-os-vps"
  project_id              = var.scw_project_id
  description             = "VPS firewall for coach-os: SSH, HTTP, HTTPS inbound only"
  inbound_default_policy  = "drop"
  outbound_default_policy = "accept"

  inbound_rule {
    action   = "accept"
    port     = 22
    protocol = "TCP"
  }

  inbound_rule {
    action   = "accept"
    port     = 80
    protocol = "TCP"
  }

  inbound_rule {
    action   = "accept"
    port     = 443
    protocol = "TCP"
  }
}

# ── VPS ──────────────────────────────────────────────────────────────────────

resource "scaleway_instance_server" "vps" {
  project_id        = var.scw_project_id
  name              = "coach-os-vps"
  type              = var.vps_instance_type
  image             = var.vps_image
  ip_id             = scaleway_instance_ip.vps.id
  security_group_id = scaleway_instance_security_group.vps.id
  tags              = ["coach-os", "prod"]
}

# ── Container Registry ───────────────────────────────────────────────────────

resource "scaleway_registry_namespace" "coach_os" {
  project_id  = var.scw_project_id
  region      = var.scw_region
  name        = var.registry_namespace
  description = "Container images for coach-os (api, frontend)"
  is_public   = false
}

# ── Managed Postgres ─────────────────────────────────────────────────────────

resource "random_password" "db" {
  length           = 32
  special          = true
  override_special = "!#%*-_=+" # Avoids chars that break shell escaping or PG connection strings
  min_lower        = 1
  min_upper        = 1
  min_numeric      = 1
  min_special      = 1
}

resource "scaleway_rdb_instance" "coach_os" {
  project_id        = var.scw_project_id
  region            = var.scw_region
  name              = "coach-os-db"
  node_type         = var.db_node_type
  engine            = var.db_engine
  user_name         = "coachos"
  password          = random_password.db.result
  volume_type       = "sbs_5k"
  volume_size_in_gb = var.db_volume_size_gb
  is_ha_cluster     = false
  disable_backup    = false
  tags              = ["coach-os", "prod"]
}

resource "scaleway_rdb_database" "coach_os" {
  instance_id = scaleway_rdb_instance.coach_os.id
  name        = "coachos"
}

# Grant the instance user full access to the coachos database.
# Scaleway RDB doesn't auto-grant when DB is created separately from the user.
resource "scaleway_rdb_privilege" "coach_os" {
  instance_id   = scaleway_rdb_instance.coach_os.id
  user_name     = scaleway_rdb_instance.coach_os.user_name
  database_name = scaleway_rdb_database.coach_os.name
  permission    = "all"
}

# ── Transactional Email domain ───────────────────────────────────────────────
# autoconfig = true lets Scaleway create and maintain the SPF/DKIM/DMARC
# records directly in Scaleway Domains. We previously managed them manually
# in dns.tf but the DKIM value drifted out of sync with TEM's expectations,
# blocking domain verification.

resource "scaleway_tem_domain" "coach_os" {
  project_id = var.scw_project_id
  region     = var.scw_region
  name       = var.domain_name
  accept_tos = true
  autoconfig = true
}
