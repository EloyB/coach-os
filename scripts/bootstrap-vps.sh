#!/usr/bin/env bash
#
# One-off bootstrap for a fresh Scaleway VPS (Ubuntu 22.04+) for coach-os.
# Run as root, ONCE, after the VPS is provisioned and DNS A records resolve.
#
#   scp scripts/bootstrap-vps.sh root@<vps-ip>:/root/
#   ssh root@<vps-ip> bash /root/bootstrap-vps.sh
#
# Idempotent: re-running is safe but the certbot step will skip if the cert
# already exists.

set -euo pipefail

DOMAIN="${DOMAIN:-coach-os.be}"
WWW_DOMAIN="${WWW_DOMAIN:-www.coach-os.be}"
ADMIN_EMAIL="${ADMIN_EMAIL:-admin@coach-os.be}"
APP_DIR="/app"

log() { echo -e "\033[1;36m▶\033[0m $*"; }
ok()  { echo -e "\033[1;32m✓\033[0m $*"; }

# ── 1. System packages ──────────────────────────────────────────────────────
log "Updating apt and installing base packages..."
export DEBIAN_FRONTEND=noninteractive
apt-get update -qq
apt-get install -y -qq \
    ca-certificates curl gnupg lsb-release \
    fail2ban ufw certbot
ok "Base packages installed"

# ── 2. Docker (official repo) ───────────────────────────────────────────────
if ! command -v docker >/dev/null 2>&1; then
    log "Installing Docker Engine + Compose plugin..."
    install -m 0755 -d /etc/apt/keyrings
    curl -fsSL https://download.docker.com/linux/ubuntu/gpg \
        | gpg --dearmor -o /etc/apt/keyrings/docker.gpg
    chmod a+r /etc/apt/keyrings/docker.gpg
    echo "deb [arch=$(dpkg --print-architecture) signed-by=/etc/apt/keyrings/docker.gpg] \
https://download.docker.com/linux/ubuntu $(lsb_release -cs) stable" \
        > /etc/apt/sources.list.d/docker.list
    apt-get update -qq
    apt-get install -y -qq docker-ce docker-ce-cli containerd.io \
                            docker-buildx-plugin docker-compose-plugin
    systemctl enable --now docker
    ok "Docker installed"
else
    ok "Docker already present"
fi

# ── 3. Firewall ─────────────────────────────────────────────────────────────
log "Configuring UFW (allow SSH/HTTP/HTTPS, deny everything else inbound)..."
ufw --force reset >/dev/null
ufw default deny incoming
ufw default allow outgoing
ufw allow 22/tcp
ufw allow 80/tcp
ufw allow 443/tcp
ufw --force enable
ok "UFW active"

# ── 4. fail2ban for SSH brute-force protection ──────────────────────────────
log "Enabling fail2ban..."
systemctl enable --now fail2ban
ok "fail2ban running"

# ── 5. App directory ────────────────────────────────────────────────────────
log "Preparing $APP_DIR..."
mkdir -p "$APP_DIR" "$APP_DIR/backups" "$APP_DIR/nginx/conf.d" /var/www/certbot
chmod 755 "$APP_DIR"
ok "$APP_DIR ready"

# ── 6. Let's Encrypt ────────────────────────────────────────────────────────
if [[ -f "/etc/letsencrypt/live/$DOMAIN/fullchain.pem" ]]; then
    ok "Certificate for $DOMAIN already exists, skipping certbot"
else
    log "Obtaining Let's Encrypt certificate for $DOMAIN + $WWW_DOMAIN..."
    # Standalone mode binds port 80 directly; our nginx isn't running yet.
    # If you re-run after nginx is up, switch to --webroot -w /var/www/certbot.
    certbot certonly --standalone --non-interactive --agree-tos \
        -m "$ADMIN_EMAIL" -d "$DOMAIN" -d "$WWW_DOMAIN"
    ok "Certificate obtained"
fi

# ── 7. Cert renewal hook (reload nginx after renewal) ───────────────────────
log "Installing certbot deploy hook for nginx reload..."
mkdir -p /etc/letsencrypt/renewal-hooks/deploy
cat > /etc/letsencrypt/renewal-hooks/deploy/reload-nginx.sh <<'HOOK'
#!/usr/bin/env bash
docker exec coach-os-nginx nginx -s reload || true
HOOK
chmod +x /etc/letsencrypt/renewal-hooks/deploy/reload-nginx.sh
ok "Renewal hook installed"

# ── 8. Scaleway registry login ──────────────────────────────────────────────
if [[ -n "${SCW_SECRET_KEY:-}" ]]; then
    log "Logging in to Scaleway container registry..."
    echo "$SCW_SECRET_KEY" | docker login rg.fr-par.scw.cloud -u nologin --password-stdin
    ok "Registry login OK"
else
    echo
    echo "⚠  SCW_SECRET_KEY not set in environment — skipping registry login."
    echo "    Run this manually before the first 'docker compose pull':"
    echo "    echo \"<scw-secret-key>\" | docker login rg.fr-par.scw.cloud -u nologin --password-stdin"
    echo
fi

echo
ok "VPS bootstrap complete."
echo
echo "Next: push to main to trigger deploy-infra.yml + the build/push workflows."
echo "      The deploy job will scp /app/docker-compose.yml + nginx/ here and"
echo "      'docker compose up -d'."
