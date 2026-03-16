#!/usr/bin/env dotnet-script

// Generate bcrypt hashes for test passwords
// Run with: dotnet script hash-passwords.cs

#r "nuget: BCrypt.Net-Next, 4.0.3"

using BCrypt.Net;

var passwords = new Dictionary<string, string>
{
    { "altmannvonw@icloud.com", "12345678" },
    { "testuser@example.com", "TestPassword123" }
};

foreach (var (email, password) in passwords)
{
    var hash = BCrypt.Net.BCrypt.HashPassword(password, workFactor: 11);
    Console.WriteLine($"-- {email}");
    Console.WriteLine($"INSERT INTO users.\"Users\" (\"Id\", \"Email\", \"PasswordHash\", \"Nickname\", \"IsBlocked\", \"CreatedAt\", \"UpdatedAt\")");
    Console.WriteLine($"VALUES ('{Guid.NewGuid()}'::uuid, '{email}', '{hash}', 'Test User', false, NOW(), NOW())");
    Console.WriteLine($"ON CONFLICT DO NOTHING;");
    Console.WriteLine();
}
