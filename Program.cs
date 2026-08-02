using Dapper;
using HomeDiary_api.Data;
using HomeDiary_api.Repositories;
using Microsoft.AspNetCore.Authentication.JwtBearer;

// ── Dapper: map snake_case DB columns to PascalCase C# properties ─────────
DefaultTypeMap.MatchNamesWithUnderscores = true;
SqlMapper.AddTypeHandler(new DateOnlyTypeHandler());

var builder = WebApplication.CreateBuilder(args);

// ── Connection string — password stored in dotnet user-secrets ────────────
var baseConnectionString = builder.Configuration.GetConnectionString("HomeDiary")
    ?? throw new InvalidOperationException("Connection string 'HomeDiary' is missing.");

var dbPassword = builder.Configuration["DbPassword"]
    ?? throw new InvalidOperationException(
        "DbPassword secret is missing. Run: dotnet user-secrets set \"DbPassword\" \"<password>\"");

var connectionString = baseConnectionString + $"Password={dbPassword};";

// ── Auth0 JWT bearer authentication ──────────────────────────────────────
var auth0Domain   = builder.Configuration["Auth0:Domain"]!;
var auth0Audience = builder.Configuration["Auth0:Audience"]!;

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = $"https://{auth0Domain}/";
        options.Audience  = auth0Audience;
    });

builder.Services.AddAuthorization();

// ── CORS: allow Angular dev server ────────────────────────────────────────
var allowedOrigins = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>()
    ?? ["http://localhost:4200"];

builder.Services.AddCors(options =>
    options.AddDefaultPolicy(policy =>
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()));

// ── Application services ──────────────────────────────────────────────────
builder.Services.AddSingleton(new DbConnectionFactory(connectionString));
builder.Services.AddSingleton<ErrorLogRepository>();

builder.Services.AddScoped<IAreaRepository,             AreaRepository>();
builder.Services.AddScoped<IContactRepository,          ContactRepository>();
builder.Services.AddScoped<IEventContactLinkRepository, EventContactLinkRepository>();
builder.Services.AddScoped<IEventStatusRepository,      EventStatusRepository>();
builder.Services.AddScoped<IEventTypeRepository,        EventTypeRepository>();
builder.Services.AddScoped<IHomeEventsRepository,       HomeEventsRepository>();
builder.Services.AddScoped<IUserRepository,             UserRepository>();

builder.Services.AddControllers();
builder.Services.AddOpenApi();

// ── Pipeline ──────────────────────────────────────────────────────────────
var app = builder.Build();

if (app.Environment.IsDevelopment())
    app.MapOpenApi();

app.UseHttpsRedirection();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

// All controller endpoints require a valid Auth0 JWT
app.MapControllers().RequireAuthorization();

app.Run();
