# State is stored in a Scaleway Object Storage bucket in the coach-os project.
# The bucket must exist before `tofu init` — bootstrap it once with:
#   scw object bucket create coach-os-tf-state region=fr-par
# See terraform/README.md.

terraform {
  backend "s3" {
    bucket                      = "coach-os-tf-state"
    key                         = "coach-os/prod/terraform.tfstate"
    region                      = "fr-par"
    endpoints                   = { s3 = "https://s3.fr-par.scw.cloud" }
    skip_credentials_validation = true
    skip_region_validation      = true
    skip_requesting_account_id  = true
    skip_metadata_api_check     = true
    skip_s3_checksum            = true

    # Credentials come from SCW_ACCESS_KEY and SCW_SECRET_KEY env vars
    # (the same ones you use for `scw` and for the scaleway provider itself).
  }
}
