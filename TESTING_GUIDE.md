# Comprehensive Testing Guide - Electronix Application

## Overview

The Electronix application uses a comprehensive testing strategy with separate unit and integration tests, all managed through an isolated Docker-based test environment. This guide explains the complete testing infrastructure.

## Architecture

### Test Environment Docker Compose

The test environment (`docker-compose.test.yml`) provides:

- **application-test**: ASP.NET Core application running on port 8080
- **postgres-test**: PostgreSQL 15 on port 5433 with `electronix_test` database
- **minio-test**: MinIO object storage on ports 9010/9011

This ensures tests run in complete isolation from the production environment.

### Test Project Structure

```
Tests/
├── Fixtures/
│   ├── TestApplicationFixture.cs       # IAsyncLifetime fixture managing HTTP client
│   └── TestDataFactory.cs              # Factory methods for test data
├── IntegrationTests/
│   ├── AuthenticationTests.cs          # Auth endpoint tests (7 tests)
│   ├── ProductsTests.cs                # Products CRUD tests (6 tests)
│   ├── CartTests.cs                    # Shopping cart tests (6 tests)
│   └── OrdersTests.cs                  # Orders and reports tests (10 tests)
├── UnitTests/
│   ├── OrderDomainTests.cs             # Order model tests (6 tests)
│   ├── DtoMappingTests.cs              # DTO mapping tests (7 tests)
│   ├── ProductDomainTests.cs           # Product model tests (9 tests)
│   ├── ValidationTests.cs              # Validation tests (12 tests)
│   └── UnitTest1.cs                    # Placeholder (can be removed)
├── Tests.csproj                        # Project file with dependencies
├── README.md                           # Quick reference guide
└── [This file]                         # Detailed testing guide
```

**Total Tests: 63+ test cases**

## Test Types

### 1. Integration Tests

Integration tests verify end-to-end functionality by making actual HTTP requests to the application.

**Location**: `IntegrationTests/` folder

**Key Classes**:

#### AuthenticationTests
- **Purpose**: Verify login, authentication, and user endpoints
- **Test Cases**: 6
- **Coverage**:
  - Login with valid credentials → Returns token
  - Login with invalid credentials → Returns 401
  - Get user info with valid token → Returns user data
  - Get user info without token → Returns 401
  - Get user info with invalid token → Returns 401

#### ProductsTests
- **Purpose**: Test product CRUD operations
- **Test Cases**: 6
- **Coverage**:
  - Get all products (no auth required)
  - Create product (requires admin auth)
  - Update product (requires admin auth)
  - Delete product (requires admin auth)
  - Authorization validation

#### CartTests
- **Purpose**: Test shopping cart functionality
- **Test Cases**: 6
- **Coverage**:
  - Get user cart
  - Add items to cart
  - Update cart items
  - Remove cart items
  - Clear entire cart
  - Authorization checks

#### OrdersTests
- **Purpose**: Test order management and sales reports
- **Test Cases**: 10+
- **Coverage**:
  - Get user orders
  - Get specific order
  - Create orders
  - Cancel pending orders
  - Update order status (admin only)
  - Get all orders (admin only)
  - Get sales reports with various date ranges
  - Authorization validation

### 2. Unit Tests

Unit tests verify individual components and business logic without external dependencies.

**Location**: `UnitTests/` folder

**Test Classes**:

#### OrderDomainTests
- Tests Order entity initialization
- Tests OrderStatus enum values
- Tests adding items to orders
- Tests status history tracking
- **Total Tests**: 6

#### DtoMappingTests
- Tests DTO field mapping
- Tests SalesReportDto structure
- Tests OrderDto mapping from domain models
- Tests DTO validation
- **Total Tests**: 7

#### ProductDomainTests
- Tests Product entity initialization
- Tests adding attributes (Color, Size, Material)
- Tests adding images
- Tests various price and stock values
- **Total Tests**: 9

#### ValidationTests
- Tests null/empty validation
- Tests negative value handling
- Tests various email formats
- Tests enum values
- Tests DateTime UTC handling
- **Total Tests**: 12+

## Running Tests

### Quick Start

```bash
# Start test environment
./run-tests.sh start

# Run all tests
./run-tests.sh test

# Stop test environment
./run-tests.sh stop
```

### Detailed Commands

**Start Test Environment**
```bash
./run-tests.sh start
```
- Starts Docker containers
- Waits for services to be ready
- Applies database migrations
- Ready for testing in ~30 seconds

**Run All Tests**
```bash
./run-tests.sh test
```

**Run Specific Test Class**
```bash
./run-tests.sh test --filter "AuthenticationTests"
./run-tests.sh test --filter "Tests.IntegrationTests.ProductsTests"
```

**Run Tests by Category**
```bash
# All integration tests
./run-tests.sh test --filter "Tests.IntegrationTests"

# All unit tests
./run-tests.sh test --filter "Tests.UnitTests"
```

**Watch Mode** (re-run on file changes)
```bash
./run-tests.sh test-watch
```

**View Logs**
```bash
./run-tests.sh logs                    # All logs
./run-tests.sh logs application-test   # Application only
./run-tests.sh logs postgres-test      # Database only
```

**View Status**
```bash
./run-tests.sh status
```

**Clean Up**
```bash
./run-tests.sh clean  # Remove all containers and volumes
```

## Test Authentication

### Test Credentials

The application has pre-seeded test accounts:

```
Admin Account:
  Email: altmannvonw@icloud.com
  Password: 12345678
  Role: ADMINISTRATOR

Test User:
  Email: testuser@example.com
  Password: TestPassword123
  Role: USER
```

### Token Management

The `TestApplicationFixture` automatically:
1. Logs in with provided credentials
2. Extracts JWT token from response
3. Stores token for use in tests
4. Provides methods to set/clear authorization headers

```csharp
// In test class
public void MyTest()
{
    _fixture.SetAuthorizationHeader(_fixture.AdminToken);
    // Now all requests are authenticated as admin
    
    _fixture.ClearAuthorizationHeader();
    // Next requests will be unauthenticated
}
```

## Test Data

### Test Data Factory

`TestDataFactory` provides methods for creating test requests:

```csharp
// Create product request
var request = TestDataFactory.Products.CreateProductRequest();

// Create order request
var request = TestDataFactory.Orders.CreateOrderRequest();

// Create cart item
var request = TestDataFactory.Cart.AddCartItemRequest(productId, quantity);

// Login request
var request = TestDataFactory.Auth.LoginRequest(email, password);
```

### Valid Test IDs

```csharp
public static readonly Guid ValidProductId = new("00000000-0000-0000-0000-000000000001");
public static readonly Guid ValidCategoryId = new("00000000-0000-0000-0000-000000000010");
public static readonly Guid ValidUserId = new("9734c85d-20c5-47a1-8c97-6eda77a04735");
```

## Test Fixture System

### TestApplicationFixture

Implements `IAsyncLifetime` for xUnit fixture lifecycle:

```csharp
public class MyTests : IClassFixture<TestApplicationFixture>
{
    private readonly TestApplicationFixture _fixture;

    public MyTests(TestApplicationFixture fixture)
    {
        _fixture = fixture;  // Automatically initialized
    }

    [Fact]
    public async Task MyTest()
    {
        // _fixture is ready
        _fixture.HttpClient.BaseAddress = new Uri("http://localhost:8080");
        var response = await _fixture.HttpClient.GetAsync("/api/products");
    }
}
// _fixture automatically cleaned up after all tests run
```

### Features

- **Auto-initialization**: Waits for application to be ready
- **Token management**: Handles login and token storage
- **HTTP client**: Pre-configured with base address
- **Health checks**: Verifies services are healthy before running tests

## Assertions and Testing Patterns

### FluentAssertions

Tests use FluentAssertions for readable, chainable assertions:

```csharp
// Instead of:
Assert.Equal(HttpStatusCode.OK, response.StatusCode);

// Write:
response.StatusCode.Should().Be(HttpStatusCode.OK);

// Chain assertions:
response.StatusCode.Should().Be(HttpStatusCode.OK);
var json = await response.Content.ReadAsStringAsync();
json.Should().Contain("token");
json.Should().NotBeNullOrEmpty();
```

### AAA Pattern

All tests follow Arrange-Act-Assert pattern:

```csharp
[Fact]
public async Task Login_WithValidCredentials_ReturnsToken()
{
    // Arrange
    var loginRequest = new { email = "...", password = "..." };
    var content = new StringContent(JsonSerializer.Serialize(loginRequest));
    
    // Act
    var response = await _fixture.HttpClient.PostAsync("/api/users/login", content);
    
    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.OK);
    var json = await response.Content.ReadAsStringAsync();
    json.Should().Contain("token");
}
```

## Database Configuration for Tests

### Test Database

- **Name**: `electronix_test`
- **Host**: localhost
- **Port**: 5433
- **User**: postgres
- **Password**: postgres

### Connection String

From `appsettings.Testing.json`:
```json
"Default": "Host=postgres-test;Port=5432;Database=electronix_test;Username=postgres;Password=postgres"
```

### DbContexts

All 6 DbContexts are configured in test environment:
- UsersDbContext (schema: users)
- ProductsDbContext (schema: products)
- OrdersDbContext (schema: orders)
- CartDbContext (schema: cart)
- ReviewsDbContext (schema: reviews)
- SupportDbContext (schema: support)

## CI/CD Integration

### GitHub Actions Example

```yaml
name: Tests

on: [push, pull_request]

jobs:
  test:
    runs-on: ubuntu-latest
    
    steps:
      - uses: actions/checkout@v2
      
      - name: Setup .NET
        uses: actions/setup-dotnet@v1
        with:
          dotnet-version: '10.0.x'
      
      - name: Start test environment
        run: ./run-tests.sh start
      
      - name: Run tests
        run: ./run-tests.sh test
      
      - name: Stop test environment
        run: ./run-tests.sh stop
```

### Local CI Testing

```bash
#!/bin/bash
set -e

# Start environment
./run-tests.sh start

# Run tests
./run-tests.sh test --logger "trx;LogFileName=test-results.trx"

# Collect coverage (if configured)
dotnet test /p:CollectCoverage=true

# Stop environment
./run-tests.sh clean

echo "All tests passed!"
```

## Troubleshooting

### Application Won't Start

```bash
# Check container logs
docker-compose -f docker-compose.test.yml logs application-test

# Verify ports are available
netstat -tlnp | grep -E ":(8080|5433|9010|9011)"

# Check application health
curl http://localhost:8080/api/products
```

### Database Connection Errors

```bash
# Verify postgres is running
docker-compose -f docker-compose.test.yml ps postgres-test

# Check migration status
cd Application
dotnet ef database info --context ProductsDbContext -- --environment Testing

# View migration logs
docker-compose -f docker-compose.test.yml logs postgres-test
```

### Test Failures

1. **Check test output**: Detailed error messages indicate the problem
2. **View application logs**: `./run-tests.sh logs application-test`
3. **Verify test data**: Ensure test accounts exist
4. **Database state**: Tests may depend on specific data existing
5. **Check timestamps**: UTC timezone issues can cause failures

### Port Already in Use

```bash
# Find process using port
lsof -i :8080      # Application
lsof -i :5433      # PostgreSQL
lsof -i :9010      # MinIO

# Kill process
kill -9 <PID>

# Or change docker-compose ports temporarily
docker-compose -f docker-compose.test.yml up -d -e PORT=8081
```

## Best Practices

### Test Independence
- Each test is independent and can run in any order
- Tests don't depend on shared state
- Use fresh test data for each test

### Performance
- Run integration tests in batches
- Keep unit tests fast (< 100ms)
- Use mocks for external dependencies (ready for future use)

### Maintenance
- Keep test names descriptive
- Update tests when changing APIs
- Remove obsolete tests
- Group related tests in classes

### Coverage Goals
- Aim for 80%+ coverage on business logic
- 100% coverage on critical paths (auth, payments, orders)
- Don't stress coverage percentage, focus on quality

## Future Enhancements

- [ ] Parallel test execution
- [ ] Performance/load testing
- [ ] Security/penetration testing
- [ ] UI/E2E tests with Playwright
- [ ] Contract testing
- [ ] Mutation testing
- [ ] Coverage reports with badge
- [ ] Test result visualization dashboard

## Resources

- **xUnit Documentation**: https://xunit.net/
- **FluentAssertions**: https://fluentassertions.com/
- **ASP.NET Core Testing**: https://docs.microsoft.com/en-us/aspnet/core/test/
- **Docker Testing**: https://docs.docker.com/language/dotnet/test/

## Support

For issues or questions:
1. Check this guide
2. Review test output and logs
3. Examine similar test for reference
4. Check application documentation
