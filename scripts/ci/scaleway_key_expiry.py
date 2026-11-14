#!/usr/bin/env python3
"""Zoekt Scaleway API-keys van CoachOS die binnenkort vervallen.

Scaleway geeft elke API-key een vervaldatum (standaard 1 jaar na aanmaak). Na die datum
falen deploys, Terraform, mail of de API zelf. Dit script vraagt alle keys van de
organisatie op en print per key die binnen REMIND_DAYS dagen vervalt één JSON-regel
{"title": ..., "body": ...}. De workflow scaleway-key-expiry-reminder.yml maakt daar issues van.

Enkel keys van CoachOS: met als standaardproject SCW_PROJECT_ID, of van een IAM-applicatie
waarvan de naam met "coach-os-" begint. De repo is publiek: er komt nooit een volledige
access key of secret in de uitvoer.

Omgeving:
  SCW_SECRET_KEY       key met minstens IAMReadOnly op organisatieniveau
  SCW_ORGANIZATION_ID  organisatie-id
  SCW_PROJECT_ID       id van het CoachOS-hoofdproject
  REMIND_DAYS          drempel in dagen (standaard 45)
  TODAY                optioneel, YYYY-MM-DD (voor tests)
"""

import json
import os
import sys
import urllib.parse
import urllib.request
from datetime import date, datetime

API = "https://api.scaleway.com/iam/v1alpha1"

# Wat elke CoachOS-applicatie doet en waar de key gebruikt wordt.
KNOWN_APPS = {
    "coach-os-gha-deploy": (
        "Deploys (registry, VPS) én de draaiende API, die er Secret Manager mee leest.",
        [
            "Maak in Scaleway → IAM → Applications → `coach-os-gha-deploy` een nieuwe API-key aan.",
            "Zet in GitHub de secrets `SCALEWAY_ACCESS_KEY` en `SCALEWAY_SECRET_KEY` op de nieuwe waarden.",
            "Start `deploy-infra.yml` manueel: die schrijft de nieuwe key naar `/app/.env` op de VPS en herstart de containers.",
            "Controleer dat de API gezond opstart en dat een deploy slaagt.",
            "Verwijder pas daarna de oude key.",
        ],
    ),
    "coach-os-terraform-ci": (
        "Terraform plan en apply in GitHub Actions (`terraform.yml`).",
        [
            "Maak in Scaleway → IAM → Applications → `coach-os-terraform-ci` een nieuwe API-key aan.",
            "Zet in GitHub de secrets `SCALEWAY_TF_ACCESS_KEY` en `SCALEWAY_TF_SECRET_KEY` op de nieuwe waarden.",
            "Start `terraform.yml` manueel (plan) en controleer dat die slaagt.",
            "Verwijder pas daarna de oude key.",
        ],
    ),
    "coach-os-tofu": (
        "Lokaal `tofu` draaien.",
        [
            "Maak in Scaleway → IAM → Applications → `coach-os-tofu` een nieuwe API-key aan.",
            "Vervang de key in je lokale tofu-/scw-configuratie.",
            "Verwijder daarna de oude key.",
        ],
    ),
    "coach-os-offsite-backup": (
        "Nachtelijke restic off-site backup op de VPS. Beheerd door Terraform.",
        [
            "Zet in `infrastructure/terraform/offsite-backup.tf` bij `scaleway_iam_api_key.offsite_backup` een nieuwe `expires_at`, maximaal 1 jaar vooruit.",
            "Open een PR: de `Tofu plan` moet de key en de secret `offsite-backup-env` vervangen, verder niets.",
            "Merge naar main. Daarna zet `offsite-backup.yml` de nieuwe key automatisch op de VPS.",
            "Start `offsite-backup.yml` manueel met **run_now** en controleer dat de backup slaagt.",
        ],
    ),
}

GENERIC_STEPS = [
    "Zoek uit waar deze key gebruikt wordt (GitHub-secrets, Scaleway Secret Manager, `/app/.env` op de VPS, lokale configuratie).",
    "Maak een nieuwe key aan met dezelfde rechten en hetzelfde standaardproject.",
    "Vervang de key overal waar hij gebruikt wordt en controleer dat alles werkt.",
    "Verwijder pas daarna de oude key.",
]


def get(path: str, params: dict, token: str) -> dict:
    url = f"{API}/{path}?{urllib.parse.urlencode(params)}"
    request = urllib.request.Request(url, headers={"X-Auth-Token": token})
    with urllib.request.urlopen(request, timeout=30) as response:
        return json.load(response)


def list_all(path: str, key: str, organization_id: str, token: str) -> list:
    items, page = [], 1
    while True:
        data = get(path, {"organization_id": organization_id, "page": page, "page_size": 100}, token)
        items.extend(data.get(key, []))
        if len(items) >= data.get("total_count", 0) or not data.get(key):
            return items
        page += 1


def parse_date(value: str) -> date:
    return datetime.fromisoformat(value.replace("Z", "+00:00")).date()


def main() -> int:
    token = os.environ["SCW_SECRET_KEY"]
    organization_id = os.environ["SCW_ORGANIZATION_ID"]
    project_id = os.environ["SCW_PROJECT_ID"]
    remind_days = int(os.environ.get("REMIND_DAYS", "45"))
    today = date.fromisoformat(os.environ["TODAY"]) if os.environ.get("TODAY") else date.today()

    applications = {
        app["id"]: app["name"]
        for app in list_all("applications", "applications", organization_id, token)
    }
    keys = list_all("api-keys", "api_keys", organization_id, token)

    checked = 0
    for key in keys:
        app_name = applications.get(key.get("application_id") or "", "")
        is_coachos = key.get("default_project_id") == project_id or app_name.startswith("coach-os-")
        if not is_coachos or not key.get("expires_at"):
            continue

        checked += 1
        expires = parse_date(key["expires_at"])
        days_left = (expires - today).days
        name = app_name or key.get("description") or "zonder beschrijving"
        suffix = key["access_key"][-4:]
        print(f"{name} (…{suffix}): vervalt {expires:%d/%m/%Y}, nog {days_left} dagen", file=sys.stderr)

        if days_left > remind_days:
            continue

        usage, steps = KNOWN_APPS.get(app_name, ("Onbekend: zie stap 1.", GENERIC_STEPS))
        owner = f"applicatie `{app_name}`" if app_name else "persoonlijke key van een gebruiker"
        status = "is **vervallen**" if days_left < 0 else f"vervalt over **{days_left} dagen**"
        body = "\n".join(
            [
                f"De Scaleway API-key **{name}** (eindigt op `…{suffix}`, {owner}) {status}, op **{expires:%d/%m/%Y}**.",
                "",
                f"**Gebruikt voor:** {usage}",
                "",
                "## Vervangen",
                *[f"{i}. {step}" for i, step in enumerate(steps, start=1)],
                "",
                "_Automatisch geopend door `scaleway-key-expiry-reminder.yml`. Sluit dit issue als de key vervangen is._",
            ]
        )
        title = f"chore(infra): Scaleway API-key {name} (…{suffix}) vervalt op {expires:%d/%m/%Y}"
        print(json.dumps({"title": title, "body": body}, ensure_ascii=False))

    print(f"{checked} CoachOS-keys gecontroleerd.", file=sys.stderr)
    return 0


if __name__ == "__main__":
    sys.exit(main())
