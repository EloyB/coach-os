# DNS records on Scaleway Domains for coach-os.be.
# The domain itself must be registered/transferred at Scaleway before these apply.

locals {
  tem_spf_record   = "v=spf1 include:_spf.tem.scw.cloud -all"
  tem_dmarc_record = "v=DMARC1; p=quarantine; rua=mailto:postmaster@${var.domain_name}"
}

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

# Transactional Email SPF
resource "scaleway_domain_record" "spf" {
  dns_zone = var.domain_name
  name     = ""
  type     = "TXT"
  data     = local.tem_spf_record
  ttl      = 3600
}

# Transactional Email DKIM (Scaleway generates the key; attach via the TEM resource output)
resource "scaleway_domain_record" "dkim" {
  dns_zone = var.domain_name
  name     = scaleway_tem_domain.coach_os.dkim_config
  type     = "TXT"
  data     = scaleway_tem_domain.coach_os.dkim_config
  ttl      = 3600
  # Scaleway's provider sometimes exposes dkim_config as a record name; if your
  # provider version returns a key=value pair instead, split and use
  # dkim_key_name / dkim_key_value outputs.
}

# DMARC
resource "scaleway_domain_record" "dmarc" {
  dns_zone = var.domain_name
  name     = "_dmarc"
  type     = "TXT"
  data     = local.tem_dmarc_record
  ttl      = 3600
}
