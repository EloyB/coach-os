# ── Off-site backup (restic) ─────────────────────────────────────────────────
# Versleutelde nachtelijke backup van de DataProtection-keyring en de database
# (scripts/backup/offsite-backup.sh) naar een bucket in een APARTE Scaleway-
# project en een andere regio dan de database.
#
# Waarom een apart project: IAM-rechten op Object Storage gelden per project,
# niet per bucket. In het hoofdproject zou de backup-key ook de Terraform-state
# (coach-os-tf-state) kunnen lezen en overschrijven. In een eigen project kan
# de key enkel aan deze bucket.
#
# De VPS haalt de drie secrets hieronder op via
# .github/workflows/offsite-backup.yml → infrastructure/backup/install-offsite-backup.sh.
# Ze staan in het hoofdproject (daar kan de API-key van de VPS lezen), onder
# het pad /offsite-backup en zonder "__" in de naam, zodat de API ze niet als
# configuratie gebruikt.
#
# Vereist voor de CI-key van Terraform: ProjectManager (projecten aanmaken) en
# IAMManager. Object Storage-rechten in het nieuwe project geeft Terraform zichzelf
# hieronder (scaleway_iam_policy.terraform_ci_offsite_backup), want de handmatige
# policy van de CI-key geldt enkel voor het hoofdproject.

resource "scaleway_account_project" "offsite_backup" {
  name        = "coach-os-offsite-backup"
  description = "Enkel de versleutelde off-site backup (restic) van coach-os"

  lifecycle {
    prevent_destroy = true
  }
}

# De CI-key van Terraform moet de bucket in het nieuwe project kunnen aanmaken
# en beheren. Zijn handmatige policy (coach-os-terraform-ci-policy) dekt enkel
# het hoofdproject, dus deze extra policy geeft hem Object Storage in het
# backupproject. IAM-wijzigingen hebben even nodig, vandaar de korte wachttijd.
data "scaleway_iam_application" "terraform_ci" {
  name = var.terraform_ci_application_name
}

resource "scaleway_iam_policy" "terraform_ci_offsite_backup" {
  name           = "coach-os-terraform-ci-offsite-backup"
  description    = "Terraform CI: Object Storage in het off-site backupproject"
  application_id = data.scaleway_iam_application.terraform_ci.id

  rule {
    project_ids          = [scaleway_account_project.offsite_backup.id]
    permission_set_names = ["ObjectStorageFullAccess"]
  }
}

resource "time_sleep" "terraform_ci_offsite_backup_propagation" {
  create_duration = "30s"
  triggers = {
    policy_id = scaleway_iam_policy.terraform_ci_offsite_backup.id
  }
}

resource "scaleway_object_bucket" "offsite_backup" {
  depends_on = [time_sleep.terraform_ci_offsite_backup_propagation]

  name       = var.offsite_backup_bucket_name
  project_id = scaleway_account_project.offsite_backup.id
  region     = var.offsite_backup_region
  tags = {
    app     = "coach-os"
    purpose = "offsite-backup"
  }

  lifecycle {
    prevent_destroy = true
  }
}

resource "scaleway_iam_application" "offsite_backup" {
  name        = "coach-os-offsite-backup"
  description = "restic op de VPS: lezen/schrijven/opruimen in de off-site backupbucket"
}

resource "scaleway_iam_policy" "offsite_backup" {
  name           = "coach-os-offsite-backup"
  description    = "Enkel Object Storage in het backupproject"
  application_id = scaleway_iam_application.offsite_backup.id

  rule {
    project_ids = [scaleway_account_project.offsite_backup.id]
    permission_set_names = [
      "ObjectStorageBucketsRead",
      "ObjectStorageObjectsRead",
      "ObjectStorageObjectsWrite",
      "ObjectStorageObjectsDelete", # nodig voor `restic forget --prune`
    ]
  }
}

resource "scaleway_iam_api_key" "offsite_backup" {
  application_id     = scaleway_iam_application.offsite_backup.id
  description        = "restic off-site backup op de VPS — beheerd door Terraform"
  default_project_id = scaleway_account_project.offsite_backup.id
}

# Het restic-wachtwoord versleutelt de hele repository. Kwijt = backup waardeloos,
# dus nooit vervangen: bewaar daarnaast een kopie BUITEN Scaleway (wachtwoordkluis).
resource "random_password" "restic" {
  length  = 48
  special = false

  lifecycle {
    prevent_destroy = true
  }
}

locals {
  offsite_backup_pg_major = element(split("-", var.db_engine), 1)
  offsite_backup_db_host  = scaleway_rdb_instance.coach_os.load_balancer.0.ip
  offsite_backup_db_port  = scaleway_rdb_instance.coach_os.load_balancer.0.port

  # Wordt /etc/coachos-backup/offsite-backup.env op de VPS (systemd EnvironmentFile).
  offsite_backup_env = <<-EOT
    # Beheerd door Terraform (infrastructure/terraform/offsite-backup.tf). Niet met de hand aanpassen.
    RESTIC_REPOSITORY=s3:https://s3.${var.offsite_backup_region}.scw.cloud/${scaleway_object_bucket.offsite_backup.name}/restic
    RESTIC_PASSWORD_FILE=/etc/coachos-backup/restic-password
    AWS_ACCESS_KEY_ID=${scaleway_iam_api_key.offsite_backup.access_key}
    AWS_SECRET_ACCESS_KEY=${scaleway_iam_api_key.offsite_backup.secret_key}
    AWS_DEFAULT_REGION=${var.offsite_backup_region}
    KEYRING_DIR=/var/lib/docker/volumes/app_data_protection_keys/_data
    PGHOST=${local.offsite_backup_db_host}
    PGPORT=${local.offsite_backup_db_port}
    PGDATABASE=${scaleway_rdb_database.coach_os.name}
    PGUSER=${scaleway_rdb_instance.coach_os.user_name}
    PGPASSFILE=/etc/coachos-backup/pgpass
    PGSSLMODE=require
    PG_MAJOR=${local.offsite_backup_pg_major}
    PG_DUMP_BIN=/usr/lib/postgresql/${local.offsite_backup_pg_major}/bin/pg_dump
  EOT

  offsite_backup_secrets = {
    "offsite-backup-env"             = local.offsite_backup_env
    "offsite-backup-restic-password" = random_password.restic.result
    # libpq .pgpass-formaat: host:port:database:user:password
    "offsite-backup-pgpass" = join(":", [
      local.offsite_backup_db_host,
      tostring(local.offsite_backup_db_port),
      scaleway_rdb_database.coach_os.name,
      scaleway_rdb_instance.coach_os.user_name,
      random_password.db.result,
    ])
  }
}

resource "scaleway_secret" "offsite_backup" {
  for_each    = local.offsite_backup_secrets
  project_id  = var.scw_project_id
  region      = var.scw_region
  name        = each.key
  path        = "/offsite-backup"
  description = "Off-site backup op de VPS — beheerd door Terraform"
  tags        = ["coach-os", "offsite-backup"]
}

resource "scaleway_secret_version" "offsite_backup" {
  for_each  = local.offsite_backup_secrets
  secret_id = scaleway_secret.offsite_backup[each.key].id
  data      = each.value
}
