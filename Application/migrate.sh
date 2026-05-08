#!/bin/bash
set -euo pipefail

# Migrate all module databases (each context uses schema-specific __EFMigrationsHistory).
# Users: push tables come from migration 20260504120000_AddPushNotifications.
# If you ever applied the removed empty migration 20260504170002_AddPushService, delete that row first:
#   DELETE FROM users."__EFMigrationsHistory" WHERE "MigrationId" = '20260504170002_AddPushService';
# Then re-run this script. Inspect applied migrations with:
#   SELECT * FROM users."__EFMigrationsHistory" ORDER BY "MigrationId";

dotnet ef database update --context UsersDbContext
dotnet ef database update --context ProductsDbContext
dotnet ef database update --context OrdersDbContext
dotnet ef database update --context CartDbContext
dotnet ef database update --context ReviewsDbContext
dotnet ef database update --context SupportDbContext
