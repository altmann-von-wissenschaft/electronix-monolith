#!/bin/bash

dotnet ef database update --context IdentityDbContext
dotnet ef database update --context CatalogDbContext
dotnet ef database update --context SalesDbContext