provider "scaleway" {
  organization_id = var.scw_organization_id
  project_id      = var.scw_project_id
  region          = var.scw_region
  zone            = var.scw_zone
}

# ── SSH key (registers the GitHub Actions deploy key on Scaleway) ────────────

resource "scaleway_iam_ssh_key" "gha_deploy" {
  name       = "gha-deploy"
  public_key = trimspace(var.ssh_public_key)
}

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

  # The SSH key is delivered to the VPS via the project-level IAM SSH keys;
  # the explicit reference here forces a dependency so the key exists before
  # the instance tries to boot with it.
  depends_on = [scaleway_iam_ssh_key.gha_deploy]
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
  length  = 32
  special = false # Scaleway RDB rejects some special chars; alphanumerics only is safest
}

resource "scaleway_rdb_instance" "coach_os" {
  project_id     = var.scw_project_id
  region         = var.scw_region
  name           = "coach-os-db"
  node_type      = var.db_node_type
  engine         = var.db_engine
  user_name      = "coachos"
  password       = random_password.db.result
  volume_type    = "bssd"
  volume_size_in_gb = var.db_volume_size_gb
  is_ha_cluster  = false
  disable_backup = false
  tags           = ["coach-os", "prod"]
}

resource "scaleway_rdb_database" "coach_os" {
  instance_id = scaleway_rdb_instance.coach_os.id
  name        = "coachos"
}

# ── Transactional Email domain ───────────────────────────────────────────────
# The domain must be DNS-verified before it can send; the records for that are
# created in dns.tf to avoid a circular dependency.

resource "scaleway_tem_domain" "coach_os" {
  project_id = var.scw_project_id
  region     = var.scw_region
  name       = var.domain_name
  accept_tos = true
}
