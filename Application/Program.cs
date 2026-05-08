using Application;
using Application.Services;
using Application.Swagger;
using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using Infrastructure.Contexts;
using Infrastructure.DependencyInjection;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Minio;

var builder = WebApplication.CreateBuilder(args);

var jwtOptions = JwtOptions.CreateWithLegacyFallbacks(
    builder.Configuration.GetSection("Jwt").Get<JwtOptions>());
builder.Services.AddSingleton(jwtOptions);

builder.WebHost.UseUrls("http://*:80");
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddControllers();

// Use postgres-test:5432 if running in docker (Testing environment), otherwise use localhost:5433
var connectionString = builder.Configuration.GetConnectionString("Default");
var isDemoEnv = builder.Environment.IsEnvironment("Testing");
if (isDemoEnv && builder.Configuration.GetConnectionString("DefaultDocker") != null)
{
    // Try to detect if we're inside docker by attempting to resolve postgres-test
    try
    {
        var addresses = System.Net.Dns.GetHostAddresses("postgres-test");
        if (addresses.Length > 0)
        {
            connectionString = builder.Configuration.GetConnectionString("DefaultDocker");
        }
    }
    catch { }  // If postgres-test can't be resolved, use the default localhost connection
}

builder.Services.AddElectronixNpgsqlDbContext<UsersDbContext>(connectionString, "users");
builder.Services.AddElectronixNpgsqlDbContext<ProductsDbContext>(connectionString, "products");
builder.Services.AddElectronixNpgsqlDbContext<OrdersDbContext>(connectionString, "orders");
builder.Services.AddElectronixNpgsqlDbContext<CartDbContext>(connectionString, "cart");
builder.Services.AddElectronixNpgsqlDbContext<ReviewsDbContext>(connectionString, "reviews");
builder.Services.AddElectronixNpgsqlDbContext<SupportDbContext>(connectionString, "support");

var firebaseCredPath = builder.Configuration["Firebase:CredentialsPath"];
if (!string.IsNullOrWhiteSpace(firebaseCredPath))
{
    var resolved = Path.IsPathRooted(firebaseCredPath)
        ? firebaseCredPath
        : Path.Combine(builder.Environment.ContentRootPath, firebaseCredPath);
    if (File.Exists(resolved))
    {
        FirebaseApp.Create(new AppOptions { Credential = GoogleCredential.FromFile(resolved) });
    }
}

// Register services
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<IPushNotificationSender, FirebasePushNotificationSender>();
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

    options.OperationFilter<ProductsFilterQueryOperationFilter>();
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
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SecretKey)),
            RequireSignedTokens = true,
            ValidateIssuer = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtOptions.Audience,
            ValidateLifetime = true,
            RequireExpirationTime = true,
            ClockSkew = TimeSpan.FromMinutes(2),
        };
        x.Events = new JwtBearerEvents
        {
            OnTokenValidated = context =>
            {
                if (context.SecurityToken is JwtSecurityToken jwt &&
                    !string.Equals(jwt.Header.Alg, SecurityAlgorithms.HmacSha256, StringComparison.Ordinal))
                {
                    context.Fail("Invalid token signing algorithm.");
                }
                return Task.CompletedTask;
            },
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

app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
});
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

