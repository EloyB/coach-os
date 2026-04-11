#!/bin/bash
set -e

echo "Stopping containers and removing database volume..."
docker-compose -f "$(git rev-parse --show-toplevel)/docker-compose.yml" down -v
echo "Starting fresh..."
docker-compose -f "$(git rev-parse --show-toplevel)/docker-compose.yml" up -d
echo ""
echo "Database volume wiped. The API will auto-migrate on startup."
