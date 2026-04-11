#!/bin/bash
#
# Tears down everything, rebuilds, and seeds fresh demo data.
# Run from the repo root after a git pull.
#
# Usage:
#   ./reset.sh
#

set -e

API_BASE="http://localhost:5142/api"
MAX_WAIT=120

echo ""
echo "=== CoachOS Reset ==="
echo ""

# 1. Tear down containers + volumes
echo "1. Stopping containers and removing volumes..."
docker compose down -v
echo "   Done."

# 2. Rebuild and start
echo ""
echo "2. Building and starting containers..."
docker compose up -d --build
echo "   Done."

# 3. Wait for API
echo ""
echo "3. Waiting for API to be ready..."
elapsed=0
until curl -sf "${API_BASE}/auth/login" -X POST -H "Content-Type: application/json" -d '{}' > /dev/null 2>&1 || [ $elapsed -ge $MAX_WAIT ]; do
    sleep 2
    elapsed=$((elapsed + 2))
    printf "   %ds...\r" "$elapsed"
done

if [ $elapsed -ge $MAX_WAIT ]; then
    echo "   API did not start within ${MAX_WAIT}s. Check: docker compose logs backend"
    exit 1
fi
echo "   API is ready."

# 4. Seed demo data
echo ""
echo "4. Seeding demo data..."
bash ./backend/Scripts/seed-demo-data.sh "$API_BASE"

echo ""
echo "=== Reset Complete ==="
echo ""
echo "Login credentials:"
echo "  Email:    jan@deaces.be"
echo "  Password: Demo1234!"
echo ""
echo "URLs:"
echo "  Frontend:  http://localhost:5317"
echo "  API:       http://localhost:5142/swagger"
echo "  Email:     http://localhost:3001"
echo ""
