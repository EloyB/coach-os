# DNS records on Scaleway Domains for coach-os.be.
# The domain itself must be registered/transferred at Scaleway before these apply.
#
# Mail authentication records (SPF, DKIM, DMARC) were originally created by
# scaleway_tem_domain.coach_os with autoconfig = true. Autoconfig is now off
# (see main.tf) so SPF can include both TEM and Google Workspace senders.

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

# ── SPF: combined Scaleway TEM + Google Workspace ────────────────────────────
# include:_spf.tem.scaleway.com  → authorizes TEM for transactional email
# include:_spf.google.com        → authorizes Gmail for outbound from @coach-os.be
# ~all                           → softfail (Google-recommended for combined senders)

resource "scaleway_domain_record" "spf" {
  dns_zone = var.domain_name
  name     = ""
  type     = "TXT"
  data     = "v=spf1 include:_spf.tem.scaleway.com include:_spf.google.com ~all"
  ttl      = 3600
}

# ── TEM DKIM ─────────────────────────────────────────────────────────────────
# Selector UUID is assigned by Scaleway TEM when the domain is created.
# The public key value below is what TEM auto-published; treat it as opaque.
# If TEM ever rotates the key, look up the new value via the Scaleway API
# and update both the name (selector) and data (public key) here.

resource "scaleway_domain_record" "tem_dkim" {
  dns_zone = var.domain_name
  name     = "9d341648-bcbf-496f-820b-968265d8394f._domainkey"
  type     = "TXT"
  data     = "v=DKIM1; h=sha256; k=rsa; p=MIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEAsFduwgevWYvpdVXuN9ivdc/URB8d2SRn62ErXTFIqG3sHUAwtVrby6LGeOtWK63wVE/PhgMh3xBk934++jHFwfs0AryTVaQYEtcPd4QESpuN6bif7o3hhwjB9XsxC7l56DjuqKexlTewA18S5/OGuKpFY7wCMSbnOUSQlAqqy9xXa24Scw0F/IJYxqnSwdKcc7kWoMX1ZBiIdsX/XRsaSlJGbaU8bFOKjTcNF7u6p5RRX43JyMitagww6fhPNxBkwo6G4AEY1YDorbc/Ijd/Z3t6GhjdD5pUbdS/LJsDVW/Ig97Q5dRnCCGNTYF2ih4aKjsInTCReHguCfB57VZGMwIDAQAB"
  ttl      = 3600
}

# ── DMARC: monitoring-only policy ────────────────────────────────────────────
# p=none means receivers don't quarantine/reject on SPF/DKIM failure — they
# just report. Tighten to p=quarantine or p=reject once we add a rua= mailbox
# and verify reports look healthy.

resource "scaleway_domain_record" "dmarc" {
  dns_zone = var.domain_name
  name     = "_dmarc"
  type     = "TXT"
  data     = "v=DMARC1; p=none"
  ttl      = 3600
}
