#!/bin/bash
#
# Post docker-compose setup script for CoachOS.
# Applies database migrations and seeds demo data if needed.
#
# Usage:
#   docker compose up -d && ./setup.sh
#

set -e

API_BASE="http://localhost:5142/api"
CONNECTION_STRING="Host=localhost;Port=5432;Database=coachos_dev;Username=coachos;Password=coachos"
MAX_WAIT=60

echo ""
echo "=== CoachOS Setup ==="
echo ""

# 0. Ensure dotnet-ef tool is installed
echo "Checking dotnet-ef tool..."
if ! dotnet ef --version &>/dev/null; then
    echo "  dotnet-ef not found. Installing..."
    dotnet tool install --global dotnet-ef
    export PATH="$PATH:$HOME/.dotnet/tools"
    echo "  dotnet-ef installed."
else
    echo "  dotnet-ef found: $(dotnet ef --version)"
fi

# 1. Wait for API to be ready
echo "Waiting for API to be ready..."
elapsed=0
while [ $elapsed -lt $MAX_WAIT ]; do
    if curl -s -o /dev/null -w "%{http_code}" "${API_BASE}/trainers" 2>/dev/null | grep -qE "^(200|401|403)$"; then
        echo "  API is ready."
        break
    fi
    sleep 2
    elapsed=$((elapsed + 2))
    printf "  %ds...\r" $elapsed
done

if [ $elapsed -ge $MAX_WAIT ]; then
    echo "  API did not become ready within ${MAX_WAIT}s. Exiting."
    exit 1
fi

# 2. Apply database migrations
echo ""
echo "Applying database migrations..."
cd backend
dotnet ef database update \
    --project CoachOS.Infrastructure \
    --startup-project CoachOS.API \
    --connection "$CONNECTION_STRING" \
    2>&1 | tail -5
echo "  Migrations applied."
cd ..

# 3. Check if seed data already exists
echo ""
echo "Checking if demo data exists..."
login_response=$(curl -s -X POST "${API_BASE}/auth/login" \
    -H "Content-Type: application/json" \
    -d '{"email": "jan@deaces.be", "password": "Demo1234!"}')

token=$(echo "$login_response" | python3 -c "import sys,json; print(json.load(sys.stdin).get('token',''))" 2>/dev/null || echo "")

if [ -n "$token" ] && [ "$token" != "" ]; then
    echo "  Demo data already exists (jan@deaces.be can log in). Skipping seed."
else
    echo "  No demo data found. Running seed script..."
    echo ""
    bash backend/Scripts/seed-demo-data.sh "$API_BASE"
fi

echo ""
echo "=== Setup Complete ==="
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
