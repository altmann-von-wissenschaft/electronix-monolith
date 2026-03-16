#!/bin/bash

# Seed test data into the test database
# This script creates test users needed for integration tests

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$SCRIPT_DIR"

echo "=== Seeding Test Database ==="

# Colors
GREEN='\033[0;32m'
NC='\033[0m'

# Run psql inside the postgres container
docker compose -f "$PROJECT_ROOT/docker-compose.test.yml" exec -T postgres-test psql -U postgres -d electronix_test << 'EOF'

-- Seed roles if they don't exist
INSERT INTO users."Roles" ("Code", "Name", "Hierarchy")
VALUES ('ADMINISTRATOR', 'Administrator', 1) ON CONFLICT DO NOTHING;

INSERT INTO users."Roles" ("Code", "Name", "Hierarchy")
VALUES ('USER', 'User', 10) ON CONFLICT DO NOTHING;

-- Seed test users if they don't exist
INSERT INTO users."Users" (
    "Id", 
    "Email", 
    "PasswordHash", 
    "Nickname", 
    "IsBlocked", 
    "CreatedAt", 
    "UpdatedAt"
)
VALUES (
    '9734c85d-20c5-47a1-8c97-6eda77a04735'::uuid,
    'altmannvonw@icloud.com',
    '$2b$11$qgZt1qj0cbmlCCn5ZG0KOuV2CAVstqHhykUFIS2iPhwHdpJtlvhJ.',  -- Password: 12345678
    'Altmann von W.',
    false,
    NOW(),
    NOW()
) ON CONFLICT DO NOTHING;

INSERT INTO users."Users" (
    "Id",
    "Email", 
    "PasswordHash",
    "Nickname",
    "IsBlocked",
    "CreatedAt",
    "UpdatedAt"
)
VALUES (
    'a1234567-20c5-47a1-8c97-6eda77a04736'::uuid,
    'testuser@example.com',
    '$2b$11$GGs.B3VvmMoIXF.djkCfeuHXth/tR4lzRtGQk/.JPNR/NDa.aZvq.',  -- Password: TestPassword123
    'Test User',
    false,
    NOW(),
    NOW()
) ON CONFLICT DO NOTHING;

-- Assign ADMINISTRATOR role to admin user
INSERT INTO users."UserRoles" ("UserId", "RoleId", "AssignedAt")
SELECT 
    u."Id",
    r."Id",
    NOW()
FROM users."Users" u
CROSS JOIN users."Roles" r
WHERE u."Email" = 'altmannvonw@icloud.com' 
  AND r."Code" = 'ADMINISTRATOR'
  AND NOT EXISTS (
    SELECT 1 FROM users."UserRoles" ur 
    WHERE ur."UserId" = u."Id" AND ur."RoleId" = r."Id"
  );

-- Assign USER role to test user  
INSERT INTO users."UserRoles" ("UserId", "RoleId", "AssignedAt")
SELECT 
    u."Id",
    r."Id",
    NOW()
FROM users."Users" u
CROSS JOIN users."Roles" r
WHERE u."Email" = 'testuser@example.com'
  AND r."Code" = 'USER'
  AND NOT EXISTS (
    SELECT 1 FROM users."UserRoles" ur 
    WHERE ur."UserId" = u."Id" AND ur."RoleId" = r."Id"
  );

-- Seed test category for products
INSERT INTO products."Categories" (
    "Id",
    "Name",
    "Description",
    "DisplayOrder"
)
VALUES (
    '00000000-0000-0000-0000-000000000010'::uuid,
    'Test Category',
    'A category for test products',
    1
) ON CONFLICT DO NOTHING;

-- Seed test products
INSERT INTO products."Products" (
    "Id",
    "Name",
    "Description",
    "Price",
    "Stock",
    "IsHidden",
    "CategoryId",
    "CreatedAt",
    "UpdatedAt"
)
VALUES (
    '00000000-0000-0000-0000-000000000001'::uuid,
    'Test Product 1',
    'A test product for unit tests',
    99.99,
    50,
    false,
    '00000000-0000-0000-0000-000000000010'::uuid,
    NOW(),
    NOW()
) ON CONFLICT DO NOTHING;

INSERT INTO products."Products" (
    "Id",
    "Name",
    "Description",
    "Price",
    "Stock",
    "IsHidden",
    "CategoryId",
    "CreatedAt",
    "UpdatedAt"
)
VALUES (
    '00000000-0000-0000-0000-000000000002'::uuid,
    'Test Product 2',
    'Another test product',
    49.99,
    100,
    false,
    '00000000-0000-0000-0000-000000000010'::uuid,
    NOW(),
    NOW()
) ON CONFLICT DO NOTHING;

EOF

echo -e "${GREEN}Test database seeded successfully!${NC}"
