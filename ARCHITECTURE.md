# Electronix Backend - Modular Monolith Redesign

## Project Overview
The ASP.NET Core 10.0 backend has been redesigned as a **modular monolith** to support an automated electronics store system with comprehensive user management, product catalog, shopping cart, orders, reviews, and support features.

## Architecture Changes

### From Three DbContexts to Unified AppDbContext
**Before:** `IdentityDbContext`, `CatalogDbContext`, `SalesDbContext` (separate schemas)
**After:** Single `AppDbContext` with unified schema for optimal performance and consistency

### Module Structure
```
Controllers/
├── Users/          # Authentication, registration, user management
├── Products/       # Catalog, categories, product management
├── Orders/         # Order management, status tracking
├── Cart/           # Shopping cart operations
├── Reviews/        # Product reviews
└── Support/        # Customer support Q&A

Domain/
├── Users/          # User, Role, UserRole entities
├── Products/       # Product, Category, ProductAttribute, ProductImage
├── Orders/         # Order, OrderItem, OrderStatusHistory
├── Cart/           # Cart, CartItem
├── Reviews/        # Review
└── Support/        # Question, Answer

DTOs/               # Data transfer objects for each module

Services/
├── AuthService.cs  # JWT token generation, password hashing, authentication
```

## Role Hierarchy

Implemented with inheritance and permission escalation:
```
GUEST (Hierarchy: 0)
  └─ Can: register, login, view catalog

CLIENT (Hierarchy: 1)
  └─ Inherits: GUEST permissions
  └─ Can: manage cart, place orders, cancel orders, view order history, ask questions, leave reviews, logout

MANAGER (Hierarchy: 2)
  └─ Inherits: CLIENT permissions
  └─ Can: answer questions, view sales reports, update order status

MODERATOR (Hierarchy: 3)
  └─ Inherits: CLIENT permissions
  └─ Can: view reviews, delete reviews, block users

ADMINISTRATOR (Hierarchy: 4)
  └─ Inherits: MANAGER + MODERATOR permissions
  └─ Can: add/edit products, hide/show products, assign roles
```

## Database Schema

### Core Tables

**Users**
- Id (UUID)
- Email (unique)
- PasswordHash (BCrypt)
- Nickname
- IsBlocked
- CreatedAt, UpdatedAt

**Roles**
- Id (int)
- Code (GUEST, CLIENT, MANAGER, MODERATOR, ADMINISTRATOR)
- Name
- Hierarchy (0-4 for permission levels)

**UserRoles** (Junction table for flexibility)
- UserId, RoleId (composite key)
- AssignedAt

**Categories**
- Id (UUID)
- Name
- Description
- ParentId (self-reference for subcategories)
- DisplayOrder

**Products**
- Id (UUID)
- Name
- Description
- Price (12,2 precision)
- Stock
- IsHidden
- MainImagePath (Minio)
- CategoryId
- CreatedAt, UpdatedAt

**ProductAttributes**
- Id (UUID)
- ProductId
- Name (e.g., "Resistance")
- Value (e.g., "10k")
- Unit (e.g., "Ohm")

**ProductImages**
- Id (UUID)
- ProductId
- ObjectName (Minio path)
- DisplayOrder
- UploadedAt

**Orders**
- Id (UUID)
- UserId
- TotalAmount (14,2 precision)
- Status (enum: Pending, Processing, ReadyForPickup, Completed, Cancelled)
- CreatedAt, UpdatedAt

**OrderItems**
- Id (UUID)
- OrderId
- ProductId
- Quantity
- PriceAtPurchase (snapshot at purchase time)

**OrderStatusHistory**
- Id (UUID)
- OrderId
- Status
- ChangedByUserId (manager/admin)
- ChangedAt
- Notes

**Cart**
- Id (UUID)
- UserId (unique, one cart per user)
- CreatedAt, UpdatedAt

**CartItems**
- Id (UUID)
- CartId
- ProductId
- Quantity
- AddedAt

**Reviews**
- Id (UUID)
- ProductId
- UserId
- Rating (1-5)
- Title
- Content
- IsApproved
- CreatedAt

**Questions/Support**
- Question
  - Id (UUID)
  - UserId
  - Subject
  - Content
  - CreatedAt
  - IsAnswered
  
- Answer
  - Id (UUID)
  - QuestionId
  - ManagerUserId
  - Content
  - CreatedAt

## JWT Authentication

### Token Claims
- `NameIdentifier`: User ID (UUID)
- `Email`: User email
- `nickname`: User nickname
- `Role`: Multiple claims for each assigned role
- `hierarchy`: Highest role hierarchy level for permission checking

### Token Generation
- **Expiration**: 24 hours
- **Signing Algorithm**: HMAC SHA256
- **Source**: `AuthToken.key` (update in production!)

### Usage
```csharp
// In controllers
var userId = AuthToken.GetID(User);           // Guid?
var email = AuthToken.GetEmail(User);         // string?
var roles = AuthToken.GetRoles(User);         // IEnumerable<string>
var hierarchy = AuthToken.GetHierarchy(User); // int?
var nickname = AuthToken.GetNickname(User);   // string?

// Verify user ownership
AuthToken.VerifyID(User, userId)              // bool
```

## Key Features Implemented

### Users Module (`TokenController`)
- **POST /api/users/register** - User registration
- **POST /api/users/login** - Authentication & JWT token
- **GET /api/users/me** - Get current user info [Auth]
- **POST /api/users/refresh** - Refresh token [Auth]

### Products Module (`ProductsController`)
- **GET /api/products** - List products (pagination, filtering)
- **GET /api/products/{id}** - Get product details
- **POST /api/products** - Create product [Admin]
- **PUT /api/products/{id}** - Update product [Admin]
- **POST /api/products/{id}/hide** - Hide from catalog [Admin]
- **POST /api/products/{id}/show** - Show in catalog [Admin]

### Orders Module (`OrdersController`)
- **GET /api/orders** - User's orders [Auth]
- **GET /api/orders/{id}** - Order details [Auth]
- **POST /api/orders** - Create order from cart [Auth]
- **POST /api/orders/{id}/cancel** - Cancel order [Auth]
- **PUT /api/orders/{id}/status** - Update status [Manager/Admin]
- **GET /api/orders/admin/all** - All orders [Manager/Admin]

## DTOs Available
- **Users**: `RegisterRequest`, `LoginRequest`, `LoginResponse`, `UserDto`
- **Products**: `ProductDto`, `ProductAttributeDto`, `CreateProductRequest`, `UpdateProductRequest`, `CategoryDto`
- **Orders**: `OrderDto`, `OrderItemDto`, `CreateOrderRequest`, `UpdateOrderStatusRequest`
- **Cart**: `CartDto`, `CartItemDto`, `AddToCartRequest`, `UpdateCartItemRequest`
- **Reviews**: `ReviewDto`, `CreateReviewRequest`
- **Support**: `QuestionDto`, `AnswerDto`, `CreateQuestionRequest`, `CreateAnswerRequest`

## Security Considerations

1. **Password Hashing**: Using BCrypt.Net-Next (add to .csproj if not present)
2. **JWT Token**: Secure by default with HTTPS in production
3. **Role-based Authorization**: `[Authorize(Roles = "ROLE1,ROLE2")]`
4. **User Ownership Verification**: Built-in with `AuthToken.VerifyID()`

## Next Steps

1. **Database Migration**: Run `dotnet ef database update` to create tables
2. **Seed Initial Categories**: Add product categories
3. **Configure Minio**: Update appsettings.json with Minio connection
4. **Implement Missing Modules**: 
   - CartController for cart management
   - ReviewsController for product reviews
   - SupportController for Q&A
   - CategoriesController for admin management
5. **Add Integration Tests**: Test authentication flow, order creation, etc.
6. **Update Environment Variables**: Change JWT secret key in production
7. **API Documentation**: Review Swagger/OpenAPI endpoint descriptions

## File Locations

- **Migrations**: `/Application/Migrations/`
  - `20260315150000_InitialCreate.cs` - Initial schema
  - `AppDbContextModelSnapshot.cs` - Model snapshot

- **Entity Models**: `/Application/Domain/{ModuleName}/`
  - Each module has its own namespace

- **Controllers**: `/Application/Controllers/{ModuleName}/`
  - RESTful endpoints per module

- **Services**: `/Application/Services/`
  - Business logic (AuthService, etc.)

- **DTOs**: `/Application/DTOs/{ModuleName}/`
  - Request/Response objects for API

## Technology Stack
- **Framework**: ASP.NET Core 10.0
- **Database**: PostgreSQL (Npgsql)
- **ORM**: Entity Framework Core 10.0.1
- **Authentication**: JWT Bearer Tokens
- **Password Hashing**: BCrypt.Net-Next
- **Object Storage**: Minio (for product images)
- **Documentation**: Swagger/OpenAPI
