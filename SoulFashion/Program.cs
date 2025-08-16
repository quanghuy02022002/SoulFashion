using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Repositories.Implementations;
using Repositories.Interfaces;
using Repositories.Models;
using Services.Implementations;
using Services.Interfaces;
using System.Text;
using Microsoft.Extensions.Logging.AzureAppServices;

var builder = WebApplication.CreateBuilder(args);

// ---- Logging (Azure App Service) ----
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddAzureWebAppDiagnostics(); // cần package Microsoft.Extensions.Logging.AzureAppServices

builder.Services.Configure<AzureFileLoggerOptions>(o =>
{
    o.FileName = "app-logs-";
    o.FileSizeLimit = 10 * 1024 * 1024;
    o.RetainedFileCountLimit = 5;
});

// ---- Services / Infra ----
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "SoulFashion API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header. Dán token: Bearer {token}",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" },
                In = ParameterLocation.Header
            },
            new List<string>()
        }
    });
});

// ✅ CHỈ 1 LẦN
builder.Services.AddDbContext<AppDBContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var issuer = builder.Configuration["Jwt:Issuer"];
        var key = builder.Configuration["Jwt:Key"];
        if (string.IsNullOrWhiteSpace(issuer) || string.IsNullOrWhiteSpace(key))
            throw new InvalidOperationException("Jwt:Issuer hoặc Jwt:Key chưa cấu hình.");

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = false,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = issuer,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key))
        };
    });

builder.Services.AddAuthorization();

// ---- DI Repos ----
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<ICostumeRepository, CostumeRepository>();
builder.Services.AddScoped<ICostumeImageRepository, CostumeImageRepository>();
builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();
builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<IOrderItemRepository, OrderItemRepository>();
builder.Services.AddScoped<IPaymentRepository, PaymentRepository>();
builder.Services.AddScoped<ICartItemRepository, CartItemRepository>();
builder.Services.AddScoped<IReturnInspectionRepository, ReturnInspectionRepository>();
builder.Services.AddScoped<IDepositRepository, DepositRepository>();
builder.Services.AddScoped<IOrderStatusHistoryRepository, OrderStatusHistoryRepository>();
builder.Services.AddScoped<ICollaboratorEearningRepository, CollaboratorEarningRepository>();
builder.Services.AddScoped<IReviewRepository, ReviewRepository>();
builder.Services.AddScoped<IBankTransferRepository, BankTransferRepository>();

// ---- DI Services ----
builder.Services.AddScoped<IAccountService, AccountService>();
builder.Services.AddScoped<TokenService>();
builder.Services.AddScoped<IS3Service, S3Service>();
builder.Services.AddScoped<ICostumeService, CostumeService>();
builder.Services.AddScoped<ICostumeImageService, CostumeImageService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<IOrderItemService, OrderItemService>();
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddScoped<IVnPayService, VnPayService>();
builder.Services.AddScoped<IMomoService, MomoService>();
builder.Services.AddScoped<IReturnInspectionService, ReturnInspectionService>();
builder.Services.AddScoped<IDepositService, DepositService>();
builder.Services.AddScoped<IOrderStatusHistoryService, OrderStatusHistoryService>();
builder.Services.AddScoped<IEarningService, EarningService>();
builder.Services.AddScoped<ICollaboratorEarningCrudService, CollaboratorEarningCrudService>();
builder.Services.AddScoped<IReviewService, ReviewService>();
builder.Services.AddScoped<IBankTransferService, BankTransferService>();
builder.Services.AddScoped<IQrCodeGeneratorService, QrCodeGeneratorService>();
builder.Services.AddHttpClient();
// ---- CORS ----
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend3000", policy =>
        policy.WithOrigins("http://localhost:3000", "https://soul-of-fashion.vercel.app")
              .AllowAnyHeader()
              .AllowAnyMethod());
});

var app = builder.Build();

// Dev error page giúp debug local
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "SoulFashion API v1");
        c.RoutePrefix = string.Empty;
    });
}
else
{
    // Production: handler chung để tránh lộ stacktrace
    app.UseExceptionHandler("/error");
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "SoulFashion API v1");
        c.RoutePrefix = string.Empty;
    });
}

app.UseHttpsRedirection();
app.UseCors("AllowFrontend3000");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapGet("/", () => "SoulFashion API is running 🚀");

app.Run();
