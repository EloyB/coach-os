#!/bin/bash
set -e

echo "Applying migrations..."
dotnet ef database update --project CoachOS.Infrastructure --startup-project CoachOS.API
echo "Database updated."
