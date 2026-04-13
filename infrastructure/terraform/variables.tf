variable "scw_organization_id" {
  description = "Scaleway organization ID (new coach-os org)."
  type        = string
}

variable "scw_project_id" {
  description = "Scaleway project ID inside the org."
  type        = string
}

variable "scw_region" {
  description = "Scaleway region for regional resources (registry, PG, TEM, secrets)."
  type        = string
  default     = "fr-par"
}

variable "scw_zone" {
  description = "Scaleway zone for zonal resources (instances, IPs)."
  type        = string
  default     = "fr-par-1"
}

variable "domain_name" {
  description = "Root domain managed via Scaleway Domains."
  type        = string
  default     = "coach-os.be"
}

variable "ssh_public_key" {
  description = "Public SSH key authorized on the VPS (the GitHub Actions deploy key)."
  type        = string
}

variable "vps_instance_type" {
  description = "Scaleway instance commercial type. Stardust S1 (STARDUST1-S) is cheapest (~€4/mo, 1 vCPU, 1 GB); upgrade to DEV1-S/PRO2-XXS if needed."
  type        = string
  default     = "STARDUST1-S"
}

variable "vps_image" {
  description = "Scaleway VPS image label."
  type        = string
  default     = "ubuntu_jammy"
}

variable "db_node_type" {
  description = "Managed Postgres node type. DB-DEV-S is the cheapest (~€12/mo)."
  type        = string
  default     = "DB-DEV-S"
}

variable "db_engine" {
  description = "Managed DB engine + version."
  type        = string
  default     = "PostgreSQL-17"
}

variable "db_volume_size_gb" {
  description = "Managed DB storage in GB (min depends on node type)."
  type        = number
  default     = 10
}

variable "registry_namespace" {
  description = "Container registry namespace name (becomes the path in rg.fr-par.scw.cloud/<name>/...)."
  type        = string
  default     = "coach-os"
}

variable "smtp_from_address" {
  description = "SMTP From address; the domain part must match domain_name or a subdomain thereof."
  type        = string
  default     = "noreply@coach-os.be"
}

variable "jwt_issuer" {
  description = "JWT iss claim."
  type        = string
  default     = "CoachOS"
}

variable "jwt_audience" {
  description = "JWT aud claim."
  type        = string
  default     = "CoachOS"
}

variable "jwt_expiry_minutes" {
  description = "JWT lifetime in minutes."
  type        = number
  default     = 60
}
