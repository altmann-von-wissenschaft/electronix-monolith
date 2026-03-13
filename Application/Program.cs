using Infrastructure.Catalog;
using Infrastructure.Identity;
using Infrastructure.Sales;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.UseUrls("http://*:80");
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddControllers();
builder.Services.AddDbContext<IdentityDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Default")));
builder.Services.AddDbContext<CatalogDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Default")));
builder.Services.AddDbContext<SalesDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

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
        Type = SecuritySchemeType.OAuth2,
        In = ParameterLocation.Header,
        Scheme = "Bearer"
    });

    options.AddSecurityRequirement(document => 
        new OpenApiSecurityRequirement()
    {
        [new OpenApiSecuritySchemeReference("OAuth2", document)] = []
    });
});

/*builder.Services.AddAuthentication(x =>
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
);*/

/*// MinIO client registration
var minioEndpoint = builder.Configuration["MINIO_ENDPOINT"] ?? "minio:9000";
var minioAccessKey = builder.Configuration["MINIO_ACCESS_KEY"] ?? "minioadmin";
var minioSecretKey = builder.Configuration["MINIO_SECRET_KEY"] ?? "minioadmin";
var minioClient = new Minio.MinioClient()
    .WithEndpoint(minioEndpoint)
    .WithCredentials(minioAccessKey, minioSecretKey)
    .Build();
builder.Services.AddSingleton(minioClient);
// bucket name config
builder.Services.Configure<MinioOptions>(options =>
{
    options.Bucket = builder.Configuration["MINIO_BUCKET"] ?? "product-images";
});*/

var app = builder.Build();

app.UseRouting();
app.UseSwagger();
app.UseSwaggerUI();
//app.UseAuthentication();
//app.UseAuthorization();
app.UseEndpoints(endpoints => 
{
    _ = endpoints.MapControllers();
});

app.Run();

/*public class MinioOptions
{
    public string Bucket { get; set; } = "product-images";
}*/
