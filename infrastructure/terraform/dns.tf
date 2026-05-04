# DNS records on Scaleway Domains for coach-os.be.
# The domain itself must be registered/transferred at Scaleway before these apply.
#
# SPF/DKIM/DMARC records for TEM are auto-created by scaleway_tem_domain.coach_os
# (autoconfig = true). When we add Google Workspace SPF/DKIM/DMARC, we'll need to
# disable autoconfig and manage all auth records here so Google + TEM can coexist.

# Apex: coach-os.be → VPS public IP
resource "scaleway_domain_record" "apex" {
  dns_zone = var.domain_name
  name     = ""
  type     = "A"
  data     = scaleway_instance_ip.vps.address
  ttl      = 300
}

# www: www.coach-os.be → VPS public IP
resource "scaleway_domain_record" "www" {
  dns_zone = var.domain_name
  name     = "www"
  type     = "A"
  data     = scaleway_instance_ip.vps.address
  ttl      = 300
}

# ── Google Workspace MX record ───────────────────────────────────────────────
# Mail for *@coach-os.be is delivered to Google Workspace mailboxes.
# Modern Google setup uses a single MX (smtp.google.com) instead of the legacy
# 5-record ASPMX.L.GOOGLE.COM setup; both are supported by Google.
# TEM is send-only and does not need MX records, so no conflict with TEM.

resource "scaleway_domain_record" "mx_google" {
  dns_zone = var.domain_name
  name     = ""
  type     = "MX"
  data     = "SMTP.GOOGLE.COM."
  priority = 1
  ttl      = 3600

  lifecycle {
    prevent_destroy = true
  }
}
