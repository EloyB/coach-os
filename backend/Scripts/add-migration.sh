#!/bin/bash
set -e

if [ -z "$1" ]; then
    echo "Usage: ./Scripts/add-migration.sh <MigrationName>"
    exit 1
fi

echo "Creating migration '$1'..."
dotnet ef migrations add "$1" --project CoachOS.Infrastructure --startup-project CoachOS.API
echo "Migration '$1' created."
