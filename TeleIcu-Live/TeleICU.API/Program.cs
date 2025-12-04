using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Security.Claims;
using StackExchange.Redis;
using System.Text;
using TeleICU.API.Helpers;
using TeleICU.API.Hubs;
using TeleICU.API.Repository;
using TeleICU.API.Repository.Interface;
using TeleICU.API.Services;
using TeleICU.API.Services.Interface;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddHttpContextAccessor();

builder.Services.AddSingleton<DapperContext>();

builder.Services.AddScoped<IAuthRepository, AuthRepository>();
builder.Services.AddScoped<IAuthService, AuthService>();

builder.Services.AddScoped<IStateRepository, StateRepository>();
builder.Services.AddScoped<IStateService, StateService>();

builder.Services.AddScoped<ICoeRepository, CoeRepository>();
builder.Services.AddScoped<ICoeService, CoeService>();

builder.Services.AddSingleton<ILogRepository, LogRepository>();
builder.Services.AddSingleton<ILogService, LogService>();

builder.Services.AddScoped<ISpokeRepository, SpokeRepository>();
builder.Services.AddScoped<ISpokeService, SpokeService>();

builder.Services.AddScoped<ISpecialistRepository, SpecialistRepository>();
builder.Services.AddScoped<ISpecialistService, SpecialistService>();

builder.Services.AddScoped<IDoctorRepository, DoctorRepository>();
builder.Services.AddScoped<IDoctorService, DoctorService>();

builder.Services.AddScoped<INurseRepository, NurseRepository>();
builder.Services.AddScoped<INurseService, NurseService>();

builder.Services.AddScoped<IDashboardRepository, DashboardRepository>();
builder.Services.AddScoped<IDashboardService, DashboardService>();

builder.Services.AddScoped<IPatientRepository, PatientRepository>();
builder.Services.AddScoped<IPatientService, PatientService>();

builder.Services.AddScoped<IBedsRepository, BedsRepository>();
builder.Services.AddScoped<IBedsService, BedsService>();

builder.Services.AddScoped<ICaseRepository, CaseRepository>();
builder.Services.AddScoped<ICaseService, CaseService>();

builder.Services.AddScoped<ICallRepository, CallRepository>();
builder.Services.AddScoped<ICallService, CallService>();

builder.Services.AddScoped<IPrescriptionRepository, PrescriptionRepository>();
builder.Services.AddScoped<IPrescriptionPdfService, PrescriptionPdfService>();
builder.Services.AddScoped<IPrescriptionService, PrescriptionService>();

builder.Services.AddScoped<IMedicineService, MedicineService>();
builder.Services.AddScoped<IMedicineRepository, MedicineRepository>();

builder.Services.AddScoped<IReportsService, ReportsService>();
builder.Services.AddScoped<IReportsRepository, ReportsRepository>();

// Add Redis-based encounter storage service
builder.Services.AddSingleton<EncounterStorageService>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.IncludeErrorDetails = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]))
        };

        // Enforce single active token per user: any older token becomes invalid
        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = async context =>
            {
                try
                {
                    var cache = context.HttpContext.RequestServices.GetRequiredService<RedisCacheHelper>();
                    var userId = context.Principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                    var authHeader = context.Request.Headers["Authorization"].ToString();

                    if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(authHeader) || !authHeader.StartsWith("Bearer "))
                    {
                        context.Fail("Invalid authorization header or claims.");
                        return;
                    }

                    var incomingToken = authHeader.Substring("Bearer ".Length).Trim();
                    var cacheKey = $"auth:token:{userId}";
                    var latestToken = await cache.GetAsync<string>(cacheKey);

                    if (string.IsNullOrWhiteSpace(latestToken) || !string.Equals(latestToken, incomingToken, StringComparison.Ordinal))
                    {
                        context.Fail("Token is no longer valid.");
                        return;
                    }
                }
                catch
                {
                    context.Fail("Token validation error.");
                }
            },
            OnMessageReceived = context =>
            {
                // Allow SignalR to receive JWT token from query string
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;
                
                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs/call"))
                {
                    context.Token = accessToken;
                }
                
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddSingleton<IConnectionMultiplexer>(
    ConnectionMultiplexer.Connect(builder.Configuration.GetConnectionString("Redis")));

builder.Services.AddScoped<RedisOtpStore>();
builder.Services.AddScoped<RedisCacheHelper>();


builder.Services.Configure<EmailSettings>(
    builder.Configuration.GetSection("EmailSettings"));


builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend",
        policy => policy

            //.WithOrigins("http://localhost:4200", "https://teleicu.esanjeevani.in")

             .WithOrigins("http://localhost:80", "https://teleicu.esanjeevani.in")


            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials());
});

// Add SignalR
builder.Services.AddSignalR(options =>
{
    options.EnableDetailedErrors = true;
    options.KeepAliveInterval = TimeSpan.FromSeconds(15);
    options.ClientTimeoutInterval = TimeSpan.FromSeconds(30);
});





builder.Services.AddAuthorization();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "TeleICU API", Version = "v1" });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer", 
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter your JWT token in this format: Bearer {your token}"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment() || app.Environment.IsProduction())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
// global error handler
app.UseMiddleware<ExceptionMiddleware>();
app.UseStaticFiles();
app.UseHttpsRedirection();
app.UseCors("AllowFrontend");
app.UseAuthentication();    
app.UseAuthorization();
app.MapControllers();

// Map SignalR Hub
app.MapHub<CallHub>("/hubs/call");

app.Run();
