# Outputs consumed by the deploy workflow + manual GitHub Secrets setup.
# Run `tofu output -json` for a machine-readable dump.

output "vps_ip" {
  description = "Public IP of the VPS — point coach-os.be A records here (already done if dns.tf is enabled)."
  value       = scaleway_instance_ip.vps.address
}

output "vps_instance_id" {
  description = "Scaleway instance ID — needed for the GitHub Actions deploy IP-lookup step."
  value       = scaleway_instance_server.vps.id
}

output "registry_endpoint" {
  description = "Container registry endpoint to push to."
  value       = scaleway_registry_namespace.coach_os.endpoint
}

output "db_host" {
  description = "Managed Postgres host."
  value       = scaleway_rdb_instance.coach_os.load_balancer.0.ip
  sensitive   = true
}

output "db_port" {
  description = "Managed Postgres port."
  value       = scaleway_rdb_instance.coach_os.load_balancer.0.port
}

output "db_name" {
  description = "Managed Postgres database name."
  value       = scaleway_rdb_database.coach_os.name
}

output "tem_dkim_record" {
  description = "DKIM record value from Scaleway TEM (for sanity-checking DNS)."
  value       = scaleway_tem_domain.coach_os.dkim_config
}

# These two outputs are intentionally not sensitive so they show up in
# `tofu output` for copy-paste into GitHub Secrets:
output "scw_organization_id_for_secrets" {
  value = var.scw_organization_id
}

output "scw_project_id_for_secrets" {
  value = var.scw_project_id
}
