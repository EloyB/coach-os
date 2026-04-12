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

if [ "$SKIP_FRONTEND" = true ]; then
    echo "Starting fresh (without frontend)..."
    SERVICES=$(docker-compose -f "$COMPOSE_FILE" config --services | grep -v '^frontend$' | tr '\n' ' ')
    docker-compose -f "$COMPOSE_FILE" up -d $SERVICES
else
    echo "Starting fresh..."
    docker-compose -f "$COMPOSE_FILE" up -d
fi

echo ""
echo "Database volume wiped. The API will auto-migrate on startup."
