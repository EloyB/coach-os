#!/bin/bash
set -e

# Usage: ./reset-db.sh [--no-frontend]
#   --no-frontend   Don't start the frontend container (useful when the FE
#                   dev runs it locally via bun dev).

SKIP_FRONTEND=false
for arg in "$@"; do
    case "$arg" in
        --no-frontend|--no-fe)
            SKIP_FRONTEND=true
            ;;
    esac
done

COMPOSE_FILE="$(git rev-parse --show-toplevel)/docker-compose.yml"

echo "Stopping containers and removing database volume..."
docker-compose -f "$COMPOSE_FILE" down -v

# --build is verplicht: zonder rebuild draait de container de vorige image en test
# de reset stale code. De reset is pas een echte "definitieve done-check" als de
# backend-image uit de huidige source herbouwd wordt.
if [ "$SKIP_FRONTEND" = true ]; then
    echo "Starting fresh (without frontend, rebuilding images)..."
    SERVICES=$(docker-compose -f "$COMPOSE_FILE" config --services | grep -v '^frontend$' | tr '\n' ' ')
    docker-compose -f "$COMPOSE_FILE" up -d --build $SERVICES
else
    echo "Starting fresh (rebuilding images)..."
    docker-compose -f "$COMPOSE_FILE" up -d --build
fi

echo ""
echo "Database volume wiped and images rebuilt. The API will auto-migrate on startup."
