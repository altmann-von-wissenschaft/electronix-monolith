#!/bin/bash

# Generate SQL INSERT statements with correct bcrypt hashes
# This script creates the SQL needed to seed users with proper password hashes

cd /home/altmann/Desktop/electronix/electronix-monolith

# Create a temp project to generate hashes
cat > /tmp/generate_hashes.csproj << 'EOF'
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="BCrypt.Net-Next" Version="4.0.3" />
  </ItemGroup>
</Project>
EOF

cat > /tmp/generate_hashes.cs << 'EOF'
using BCrypt.Net;

// Generate hashes with same work factor as application
var password1 = "12345678";
var hash1 = BCrypt.HashPassword(password1, workFactor: 11);
Console.WriteLine($"'{hash1}'");

var password2 = "TestPassword123";
var hash2 = BCrypt.HashPassword(password2, workFactor: 11);
Console.WriteLine($"'{hash2}'");
EOF

cd /tmp
dotnet run --project generate_hashes.csproj 2>/dev/null
