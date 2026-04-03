
using FinTrackPortal.API.Services;
using FinTrackPortal.Interfaces;
using FinTrackPortal.Models;
using FinTrackPortal.Repositories;
using FinTrackPortal.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Reflection;
using System.Text;
using System.Threading.RateLimiting;
var builder = WebApplication.CreateBuilder(args);

// ============================================================
// 1 Configure JWT Authentication Settings
// ============================================================

var jwtSettingsSection = builder.Configuration.GetSection("JwtSettings");
builder.Services.Configure<JwtSettings>(jwtSettingsSection);

builder.Services.Configure<AppEmailOptions>(builder.Configuration.GetSection(AppEmailOptions.SectionName));

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

builder.Services.AddRateLimiter(options =>
{
    // FinShare WhatToDoAndWhere.pdf: login 5 attempts per IP per 15 minutes
    options.AddFixedWindowLimiter("login", opt =>
    {
        opt.Window = TimeSpan.FromMinutes(15);
        opt.PermitLimit = 5;
        opt.QueueLimit = 0;
        opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
    });
    options.AddFixedWindowLimiter("register", opt =>
    {
        opt.Window = TimeSpan.FromHours(1);
        opt.PermitLimit = 3;
        opt.QueueLimit = 0;
    });
    options.AddFixedWindowLimiter("forgotpw", opt =>
    {
        opt.Window = TimeSpan.FromMinutes(15);
        opt.PermitLimit = 3;
        opt.QueueLimit = 0;
    });
    options.OnRejected = async (context, token) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        await context.HttpContext.Response.WriteAsJsonAsync(
            new
            {
                success = false,
                message = "Too many attempts. Please wait and try again.",
                data = (object?)null,
                errors = (List<string>?)null
            },
            token);
    };
});

builder.Services.AddHostedService<SecurityMaintenanceHostedService>();
builder.Services.AddSingleton<SmtpEmailSender>();
builder.Services.AddSingleton<MicrosoftGraphEmailSender>();
builder.Services.AddSingleton<IEmailSender, EmailSenderSelector>();

var jwtSettings = jwtSettingsSection.Get<JwtSettings>()
    ?? throw new InvalidOperationException("JwtSettings configuration section is missing or invalid.");
if (string.IsNullOrWhiteSpace(jwtSettings.Key))
    throw new InvalidOperationException("JwtSettings:Key is required.");
var jwtKey = Encoding.UTF8.GetBytes(jwtSettings.Key);



// ============================================================
// 2 Add Configuration and Services
// ============================================================ 
// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "FinTrack WHIZ SOLUTIONS",
        Version = "v1",
        Description = "Expense splitting and settlement API. Authenticate via /api/Auth/login to get a JWT, then use the Authorize button."
    });

    // Include XML comments from all projects so Swagger shows endpoint descriptions
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath)) c.IncludeXmlComments(xmlPath);

    var modelsXml = Path.Combine(AppContext.BaseDirectory, "FinTrackPortal.Models.xml");
    if (File.Exists(modelsXml)) c.IncludeXmlComments(modelsXml);

    // Define the BearerAuth scheme
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token.",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
        
    });

    // Require Bearer token globally
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
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



// Register dependencies

builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IAuditLogRepository, AuditLogRepository>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IGroupRepository, GroupRepository>();
builder.Services.AddScoped<IGroupService, GroupService>();
builder.Services.AddScoped<IExpenseRepository, ExpenseRepository>();
builder.Services.AddScoped<IExpenseService, ExpenseService>();
builder.Services.AddScoped<IMemberRepository, MemberRepository>();
builder.Services.AddScoped<IMemberService, MemberService>();
builder.Services.AddScoped<ISettlementRepository, SettlementRepository>();
builder.Services.AddScoped<ISettlementService, SettlementService>();
builder.Services.AddScoped<ISubscriptionRepository, SubscriptionRepository>();
builder.Services.AddScoped<ISubscriptionService, SubscriptionService>();

var attachmentProvider = builder.Configuration["AttachmentStorage:Provider"]?.Trim() ?? "Azure";
if (string.Equals(attachmentProvider, "Local", StringComparison.OrdinalIgnoreCase))
    builder.Services.AddScoped<IAttachmentStorageService, LocalFileStorageService>();
else
    builder.Services.AddScoped<IAttachmentStorageService, BlobStorageService>();

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,

        ValidIssuer = jwtSettings.Issuer,
        ValidAudience = jwtSettings.Audience,
        IssuerSigningKey = new SymmetricSecurityKey(jwtKey)
    };

    options.Events = new JwtBearerEvents
    {
        OnChallenge = async context =>
        {
            context.HandleResponse();
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(new
            {
                success = false,
                message = "Unauthorized access",
                data = (object?)null,
                errors = (object?)null
            });
        },
        OnForbidden = async context =>
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(new
            {
                success = false,
                message = "Access denied",
                data = (object?)null,
                errors = (object?)null
            });
        }
    };
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        policy => policy
            .AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader());
});

builder.Services.AddAuthorization(); 

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler(appError =>
    {
        appError.Run(async context =>
        {
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(new
            {
                success = false,
                message = "An unexpected error occurred.",
                data = (object?)null,
                errors = (object?)null
            });
        });
    });
    app.UseHsts();
}

app.UseSwagger();
app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "FinTrack WHIZ SOLUTIONS v1"));

app.UseForwardedHeaders();
app.UseHttpsRedirection();

if (string.Equals(app.Configuration["AttachmentStorage:Provider"]?.Trim() ?? "Azure", "Local", StringComparison.OrdinalIgnoreCase))
{
    var configuredPath = app.Configuration["LocalStorage:Path"];
    if (!string.IsNullOrWhiteSpace(configuredPath))
    {
        var localPath = LocalFileStorageService.ResolvePhysicalStoragePath(configuredPath, app.Environment);
        try
        {
            if (!Directory.Exists(localPath))
                Directory.CreateDirectory(localPath);
        }
        catch (UnauthorizedAccessException ex)
        {
            throw new InvalidOperationException(
                $"Cannot create attachment folder '{localPath}'. On shared hosting use a path under your site, e.g. LocalStorage:Path = \"App_Data/attachments\", and ensure the app pool can write there. See appsettings.Production.json.",
                ex);
        }

        app.UseStaticFiles(new StaticFileOptions
        {
            FileProvider = new PhysicalFileProvider(localPath),
            RequestPath = "/attachments"
        });
    }
}


app.UseCors("AllowAll");

app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();
app.MapControllers();

app.Run();