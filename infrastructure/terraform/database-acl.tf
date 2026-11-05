# ROLLOUT GATE (#260): do not apply until the GitHub-hosted migrate-db job
# and deploy's dependency on it have been removed by an authorized owner.
# The proposed workflow diff is in the PR BODY ONLY, not applied here.
# This resource REPLACES ALL instance ACL entries. Review existing operator
# access and verify the VPS's actual egress address before any approved apply.
resource "scaleway_rdb_acl" "coach_os" {
  instance_id = scaleway_rdb_instance.coach_os.id

  acl_rules {
    ip          = "${scaleway_instance_ip.vps.address}/32"
    description = "CoachOS VPS only; API performs startup migrations"
  }
}
