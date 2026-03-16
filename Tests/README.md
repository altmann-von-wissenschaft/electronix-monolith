# Unit Tests and Integration Tests

This project contains comprehensive unit and integration tests for the Electronix application.

## Project Structure

```
Tests/
├── Fixtures/
│   ├── TestApplicationFixture.cs        # Shared test context and HTTP client setup
│   └── TestDataFactory.cs               # Test data factories for creating test objects
├── IntegrationTests/
│   ├── AuthenticationTests.cs           # Tests for login and user endpoints
│   ├── ProductsTests.cs                 # Tests for product CRUD operations
│   ├── CartTests.cs                     # Tests for shopping cart operations
│   └── OrdersTests.cs                   # Tests for order management and sales reports
├── UnitTests/
│   ├── OrderDomainTests.cs              # Unit tests for Order domain models
│   ├── DtoMappingTests.cs               # Unit tests for DTO mapping
│   └── ProductDomainTests.cs            # Unit tests for Product domain models
└── Tests.csproj                         # Project file with test dependencies
```

## Test Environment Setup

A separate Docker environment is configured for testing:

- **docker-compose.test.yml**: Defines isolated services for testing
  - `application-test`: Application running on port 8080
  - `postgres-test`: PostgreSQL on port 5433 with database `electronix_test`
  - `minio-test`: MinIO on ports 9010/9011 for image storage

## Prerequisites

- Docker and Docker Compose
- .NET 10.0 SDK
- xUnit testing framework
- FluentAssertions for readable test assertions
- Moq for mocking (ready for future use)

## Running Tests

### Start Test Environment
```bash
cd /path/to/electronix-monolith
docker-compose -f docker-compose.test.yml up -d
sleep 20  # Wait for services to be ready
```

### Apply Database Migrations for Test Environment
```bash
cd Application
dotnet ef database update --context UsersDbContext -- --environment Testing
dotnet ef database update --context ProductsDbContext -- --environment Testing
dotnet ef database update --context OrdersDbContext -- --environment Testing
dotnet ef database update --context CartDbContext -- --environment Testing
dotnet ef database update --context ReviewsDbContext -- --environment Testing
dotnet ef database update --context SupportDbContext -- --environment Testing
# Or use migrate script if available
```

### Run All Tests
```bash
cd Tests
dotnet test
```

### Run Specific Test Class
```bash
dotnet test --filter "Tests.IntegrationTests.AuthenticationTests"
```

### Run Tests with Coverage
```bash
dotnet test /p:CollectCoverage=true /p:CoverageFormat=opencover
```

### Stop Test Environment
```bash
docker-compose -f docker-compose.test.yml down
```

## Test Categories

### Integration Tests
Located in `IntegrationTests/` folder. These tests:
- Connect to the application via HTTP endpoints
- Verify end-to-end functionality
- Test authentication, authorization, and business logic
- Use `TestApplicationFixture` for shared setup

**Test Classes:**
- **AuthenticationTests**: Login, token validation, user authentication
- **ProductsTests**: Get, create, update, delete products
- **CartTests**: Cart operations, item management
- **OrdersTests**: Order creation, status updates, sales reports

### Unit Tests
Located in `UnitTests/` folder. These tests:
- Test individual domain models and entities
- Verify DTO mapping and validation
- Test enum values and domain logic
- Don't require external services

**Test Classes:**
- **OrderDomainTests**: Order entity, status tracking, items
- **DtoMappingTests**: DTO initialization, mapping correctness
- **ProductDomainTests**: Product entity, attributes, images

### Test Fixtures
Located in `Fixtures/` folder:
- **TestApplicationFixture**: Manages HTTP client, authentication, application readiness
- **TestDataFactory**: Creates test request objects and test data

## Test Data

Default test accounts:
- **Admin**: altmannvonw@icloud.com / 12345678
- **User**: testuser@example.com / TestPassword123

Valid GUIDs for testing:
- Product ID: 00000000-0000-0000-0000-000000000001
- Category ID: 00000000-0000-0000-0000-000000000010
- User ID: 9734c85d-20c5-47a1-8c97-6eda77a04735

## Testing Guidelines

1. **Isolation**: Each test is independent and can run in any order
2. **Cleanup**: Test environment is managed via Docker, data is reset between test runs
3. **Assertions**: Use FluentAssertions for readable test assertions
4. **Naming**: Follow AAA pattern (Arrange, Act, Assert) with clear test names
5. **Coverage**: Aim for critical business logic and API endpoints

## CI/CD Integration

These tests can be integrated into CI/CD pipelines:

```bash
# Build and test
docker-compose -f docker-compose.test.yml up -d
cd Application && bash migrate.sh
cd ../Tests
dotnet test --logger "trx;LogFileName=test-results.trx"
docker-compose -f docker-compose.test.yml down
```

## Troubleshooting

### Application not starting
- Ensure ports 8080, 5433, 9010, 9011 are available
- Check Docker logs: `docker-compose -f docker-compose.test.yml logs application-test`

### Database connection errors
- Verify postgres-test container is healthy: `docker-compose -f docker-compose.test.yml ps`
- Check migration logs in Application folder

### Test failures
- Review test output for specific assertions
- Check application logs for error details
- Verify test environment is properly initialized

## Future Enhancements

- Add performance/load tests
- Add UI/E2E tests with Selenium
- Add security/penetration tests
- Expand mock objects for isolated unit testing
- Add API contract testing
