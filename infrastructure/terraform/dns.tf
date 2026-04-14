# DNS records on Scaleway Domains for coach-os.be.
# The domain itself must be registered/transferred at Scaleway before these apply.
#
# SPF/DKIM/DMARC records are NOT managed here — they're auto-created by
# scaleway_tem_domain.coach_os (autoconfig = true). See main.tf.

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
