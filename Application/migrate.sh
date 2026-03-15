#!/bin/bash

# Migrate all module databases
dotnet ef database update --context UsersDbContext
dotnet ef database update --context ProductsDbContext
dotnet ef database update --context OrdersDbContext
dotnet ef database update --context CartDbContext
dotnet ef database update --context ReviewsDbContext
dotnet ef database update --context SupportDbContext
