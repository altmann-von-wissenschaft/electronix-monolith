using Application;
using Application.Services;
using Infrastructure.Contexts;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Minio;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.UseUrls("http://*:80");
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddControllers();

var connectionString = builder.Configuration.GetConnectionString("Default");

builder.Services.AddDbContext<UsersDbContext>(options =>
    options.UseNpgsql(connectionString, x => x.MigrationsHistoryTable("__EFMigrationsHistory", "users")));

builder.Services.AddDbContext<ProductsDbContext>(options =>
    options.UseNpgsql(connectionString, x => x.MigrationsHistoryTable("__EFMigrationsHistory", "products")));

builder.Services.AddDbContext<OrdersDbContext>(options =>
    options.UseNpgsql(connectionString, x => x.MigrationsHistoryTable("__EFMigrationsHistory", "orders")));

builder.Services.AddDbContext<CartDbContext>(options =>
    options.UseNpgsql(connectionString, x => x.MigrationsHistoryTable("__EFMigrationsHistory", "cart")));

builder.Services.AddDbContext<ReviewsDbContext>(options =>
    options.UseNpgsql(connectionString, x => x.MigrationsHistoryTable("__EFMigrationsHistory", "reviews")));

builder.Services.AddDbContext<SupportDbContext>(options =>
    options.UseNpgsql(connectionString, x => x.MigrationsHistoryTable("__EFMigrationsHistory", "support")));

// Register services
builder.Services.AddScoped<AuthService>();
builder.Services.AddHttpClient<ProductsService>();
builder.Services.AddHttpClient<OrdersService>();

// Add HttpClientFactory for inter-module communication
builder.Services.AddHttpClient();

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo 
    { 
        Title = "Electronix API", 
        Version = "v1",
        Description = "API with JWT Authentication"
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Scheme = "Bearer"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement()
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                },
                Scheme = "oauth2",
                Name = "Bearer",
                In = ParameterLocation.Header
            }, 
            new List<string>()
        }
    });
});

builder.Services.AddAuthentication(x =>
    {
        x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(x =>
    {
        x.RequireHttpsMetadata = false;
        x.SaveToken = true;
        x.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(AuthToken.key),
            ValidateIssuer = false,
            ValidateAudience = false
        };
    }
);

// MinIO client registration
var minioEndpoint = builder.Configuration["MINIO_ENDPOINT"] ?? "minio:9000";
var minioAccessKey = builder.Configuration["MINIO_ACCESS_KEY"] ?? "minioadmin";
var minioSecretKey = builder.Configuration["MINIO_SECRET_KEY"] ?? "minioadmin";
var minioClient = new MinioClient()
    .WithEndpoint(minioEndpoint)
    .WithCredentials(minioAccessKey, minioSecretKey)
    .Build();
builder.Services.AddSingleton(minioClient);
builder.Services.AddScoped<MinioService>();

// bucket name config
builder.Services.Configure<MinioOptions>(options =>
{
    options.Bucket = builder.Configuration["MINIO_BUCKET"] ?? "images";
});

var app = builder.Build();

app.UseRouting();
app.UseSwagger();
app.UseSwaggerUI();
app.UseAuthentication();
app.UseAuthorization();
app.UseEndpoints(endpoints => 
{
    _ = endpoints.MapControllers();
});

app.Run();

public class MinioOptions
{
    public string Bucket { get; set; } = "images";
}

