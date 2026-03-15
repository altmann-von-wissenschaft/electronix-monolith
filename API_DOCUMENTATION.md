# Electronix Backend - REST API Documentation

## Quick Start

### 1. Install Dependencies
```bash
cd /home/altmann/Desktop/electronix/electronix-monolith/Application
dotnet restore
```

### 2. Apply Migrations
```bash
dotnet ef database update
```

### 3. Run the Application
```bash
dotnet run
```

Server will start on `http://localhost:80`
Swagger UI: `http://localhost:80/swagger`

## Database Setup

### PostgreSQL Connection
Edit `appsettings.json`:
```json
{
  "ConnectionStrings": {
    "Default": "Host=localhost;Port=5432;Database=electronix;Username=postgres;Password=postgres"
  }
}
```

### Docker PostgreSQL
```bash
docker run --name electronix-db -e POSTGRES_PASSWORD=postgres -p 5432:5432 -d postgres:15
```

## Authentication Flow

### 1. Register User
```bash
curl -X POST http://localhost:80/api/users/register \
  -H "Content-Type: application/json" \
  -d '{
    "email": "user@example.com",
    "password": "SecurePass123!",
    "nickname": "JohnDoe"
  }'
```

**Response:**
```json
{
  "message": "User registered successfully",
  "userId": "550e8400-e29b-41d4-a716-446655440000"
}
```

### 2. Login & Get Token
```bash
curl -X POST http://localhost:80/api/users/login \
  -H "Content-Type: application/json" \
  -d '{
    "email": "user@example.com",
    "password": "SecurePass123!"
  }'
```

**Response:**
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "userId": "550e8400-e29b-41d4-a716-446655440000",
  "email": "user@example.com",
  "nickname": "JohnDoe",
  "roles": ["GUEST", "CLIENT"]
}
```

### 3. Use Token in Requests
```bash
curl -X GET http://localhost:80/api/users/me \
  -H "Authorization: Bearer YOUR_JWT_TOKEN_HERE"
```

## API Endpoints

### Users Module

#### Authentication
- `POST /api/users/register` - Register new user
- `POST /api/users/login` - Login and get JWT token
- `POST /api/users/refresh` - Refresh token [Auth]
- `GET /api/users/me` - Get current user info [Auth]

### Products Module

#### Products
- `GET /api/products` - List products (pagination, category filter)
  - Query: `?categoryId=uuid&page=1&pageSize=20`
- `GET /api/products/{id}` - Get product details
- `POST /api/products` - Create product [Admin]
- `PUT /api/products/{id}` - Update product [Admin]
- `POST /api/products/{id}/hide` - Hide product [Admin]
- `POST /api/products/{id}/show` - Show product [Admin]

#### Categories
- `GET /api/categories` - Get all categories (hierarchical)
  - Query: `?parentId=uuid`
- `GET /api/categories/{id}` - Get category with subcategories
- `POST /api/categories` - Create category [Admin]
- `PUT /api/categories/{id}` - Update category [Admin]
- `DELETE /api/categories/{id}` - Delete category [Admin]

### Orders Module

- `GET /api/orders` - Get user's orders [Auth]
- `GET /api/orders/{id}` - Get order details [Auth]
- `POST /api/orders` - Create order from cart [Auth]
- `POST /api/orders/{id}/cancel` - Cancel order [Auth]
- `PUT /api/orders/{id}/status` - Update order status [Manager/Admin]
- `GET /api/orders/admin/all` - Get all orders [Manager/Admin]
  - Query: `?status=Processing`

### Cart Module

- `GET /api/cart` - Get shopping cart [Auth]
- `POST /api/cart/items` - Add item to cart [Auth]
- `PUT /api/cart/items/{itemId}` - Update cart item quantity [Auth]
- `DELETE /api/cart/items/{itemId}` - Remove item from cart [Auth]
- `DELETE /api/cart` - Clear cart [Auth]

### Reviews Module

- `GET /api/reviews` - Get product reviews
  - Query: `?productId=uuid&page=1&pageSize=20`
- `POST /api/reviews` - Create review [Auth]
- `GET /api/reviews/pending` - Get unapproved reviews [Moderator/Admin]
- `POST /api/reviews/{id}/approve` - Approve review [Moderator/Admin]
- `DELETE /api/reviews/{id}` - Delete review [Moderator/Admin]

### Support Module

- `GET /api/support/questions` - Get user's questions [Auth]
- `POST /api/support/questions` - Create question [Auth]
- `GET /api/support/questions/{id}` - Get question details [Auth]
- `POST /api/support/questions/{id}/answers` - Answer question [Manager/Admin]
- `GET /api/support/questions/unanswered` - Get unanswered questions [Manager/Admin]
- `DELETE /api/support/questions/{id}/answers/{answerId}` - Delete answer [Manager/Admin]

### Admin Module

- `GET /api/admin/users` - Get all users [Admin]
- `GET /api/admin/users/{id}` - Get user details [Admin]
- `POST /api/admin/users/{id}/block` - Block/unblock user [Admin]
- `POST /api/admin/users/{id}/roles` - Assign role to user [Admin]
- `DELETE /api/admin/users/{id}/roles/{roleCode}` - Remove role from user [Admin]
- `GET /api/reports/sales` - Get sales report [Manager/Admin]
  - Query: `?startDate=2026-01-01&endDate=2026-12-31`

## Workflow Examples

### Complete Order Flow

1. **Register & Login**
```bash
# Register
curl -X POST http://localhost:80/api/users/register \
  -H "Content-Type: application/json" \
  -d '{"email":"buyer@store.com","password":"Pass123!","nickname":"Buyer"}'

# Login to get token
curl -X POST http://localhost:80/api/users/login \
  -H "Content-Type: application/json" \
  -d '{"email":"buyer@store.com","password":"Pass123!"}'
```

2. **Browse Products**
```bash
# Get all products
curl http://localhost:80/api/products

# Get by category
curl "http://localhost:80/api/products?categoryId=CATEGORY_UUID"

# Get product details
curl http://localhost:80/api/products/PRODUCT_UUID
```

3. **Add to Cart**
```bash
curl -X POST http://localhost:80/api/cart/items \
  -H "Authorization: Bearer TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"productId":"PRODUCT_UUID","quantity":2}'
```

4. **Place Order**
```bash
# Create order from cart
curl -X POST http://localhost:80/api/orders \
  -H "Authorization: Bearer TOKEN"

# Check order status
curl http://localhost:80/api/orders \
  -H "Authorization: Bearer TOKEN"
```

5. **Manager Updates Status**
```bash
curl -X PUT http://localhost:80/api/orders/ORDER_UUID/status \
  -H "Authorization: Bearer MANAGER_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "status": "Processing",
    "notes": "Order picked and packed"
  }'
```

## Role-Based Access Control

### Role Hierarchy
```
GUEST (0) → Register/Login only
  ↓
CLIENT (1) → Shopping, Orders, Reviews, Questions
  ↓
MANAGER (2) + MODERATOR (3) → Manage orders, answer questions, review moderation
  ↓
ADMINISTRATOR (4) → Full system access
```

### Authorization Examples

```csharp
// Allow multiple roles
[Authorize(Roles = "MANAGER,ADMINISTRATOR")]

// Admin only
[Authorize(Roles = "ADMINISTRATOR")]

// Any authenticated user
[Authorize]

// Public endpoint
[AllowAnonymous]
```

## Error Handling

All endpoints return standardized error responses:

```json
{
  "message": "Error description",
  "error": "Additional error details"
}
```

### Common Status Codes
- `200 OK` - Success
- `201 Created` - Resource created
- `400 Bad Request` - Invalid input
- `401 Unauthorized` - Missing/invalid token
- `403 Forbidden` - Insufficient permissions
- `404 Not Found` - Resource not found
- `409 Conflict` - Resource already exists
- `500 Internal Server Error` - Server error

## Database Schema Overview

### Users/Auth
- **Users** - User accounts with email, password, status
- **Roles** - GUEST, CLIENT, MANAGER, MODERATOR, ADMINISTRATOR
- **UserRoles** - Many-to-many role assignments

### Products/Catalog
- **Categories** - Hierarchical product categories
- **Products** - Product information with pricing and stock
- **ProductAttributes** - Product specifications (e.g., Resistance: 10k Ohm)
- **ProductImages** - Images stored in Minio

### Orders/Sales
- **Orders** - Order header with user and total
- **OrderItems** - Line items with product snapshot
- **OrderStatusHistory** - Order status tracking and audit trail

### Shopping
- **Cart** - Shopping cart per user
- **CartItems** - Items in cart with quantity

### Reviews
- **Reviews** - Product reviews with ratings

### Support
- **Questions** - Customer questions
- **Answers** - Manager answers to questions

## Configuration

### JWT Secret Key
Edit `AuthToken.cs`:
```csharp
public static readonly byte[] key = Encoding.ASCII.GetBytes("YOUR_SECRET_KEY_HERE");
```

**⚠️ IMPORTANT:** Change this in production!

### Connection String
Edit `appsettings.json`:
```json
"ConnectionStrings": {
  "Default": "Host=your-db;Port=5432;Database=electronix;Username=user;Password=pass"
}
```

### Minio Configuration
To be added to `appsettings.json`:
```json
"Minio": {
  "Endpoint": "http://minio:9000",
  "AccessKey": "minioadmin",
  "SecretKey": "minioadmin",
  "BucketName": "electronix-products"
}
```

## Development Notes

### Adding New Endpoints

1. Create/Update Controller in `Controllers/{Module}/`
2. Create DTOs in `DTOs/{Module}/`
3. Add service class in `Services/` if needed
4. Define DbSet in `AppDbContext`
5. Add authorization attributes: `[Authorize]` or `[Authorize(Roles = "ROLE")]`

### Creating Migrations

```bash
# Create migration
dotnet ef migrations add MigrationName

# Apply migration
dotnet ef database update

# Rollback migration
dotnet ef database update PreviousMigrationName
```

### Testing with cURL

```bash
# Get token
TOKEN=$(curl -s -X POST http://localhost:80/api/users/login \
  -H "Content-Type: application/json" \
  -d '{"email":"user@example.com","password":"Pass123!"}' \
  | grep -o '"token":"[^"]*' | cut -d'"' -f4)

# Use token in requests
curl http://localhost:80/api/users/me \
  -H "Authorization: Bearer $TOKEN"
```

## Security Best Practices

1. **Always use HTTPS in production**
2. **Change JWT secret key**
3. **Use strong password hashing (BCrypt)**
4. **Implement CORS** if needed
5. **Add rate limiting** for auth endpoints
6. **Validate all inputs**
7. **Use parameterized queries** (EF Core does this)
8. **Implement request logging and monitoring**
9. **Regular security updates** for dependencies
10. **Store sensitive config** in environment variables

## Performance Tips

1. **Use pagination** for list endpoints
2. **Add indexes** to frequently queried columns
3. **Use `async/await`** for all database operations
4. **Implement caching** for catalog data
5. **Use connection pooling** for database
6. **Add distributed caching** (Redis) for sessions
7. **Implement background jobs** for heavy operations

## Next Steps

1. Implement Minio image upload service
2. Add integration tests
3. Setup CI/CD pipeline (GitHub Actions, etc.)
4. Implement email notifications
5. Add pagination metadata to list responses
6. Implement search/filtering for products
7. Add order notifications for users
8. Setup application monitoring and logging
