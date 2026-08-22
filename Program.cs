using Dapper;
using HomeDiary_api.Data;
using HomeDiary_api.Repositories;
using HomeDiary_api.Middleware;
using HomeDiary_api.Services;
using HomeDiary_api.Security;
using HomeDiary_api.Configuration;
using Amazon;
using Amazon.SimpleEmailV2;
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

builder.Services.AddAuthorization(options =>
    options.AddPolicy("HomeDiaryAdmin", policy =>
        policy.RequireClaim("homediary_admin", "true")));

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
builder.Services.AddSingleton<ApplicationParameterProtector>();
builder.Services.AddScoped<ClientContext>();
builder.Services.AddOptions<InvitationEmailOptions>()
    .Bind(builder.Configuration.GetSection(InvitationEmailOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();
builder.Services.AddSingleton<IAmazonSimpleEmailServiceV2>(services =>
{
    var options = services.GetRequiredService<Microsoft.Extensions.Options.IOptions<InvitationEmailOptions>>().Value;
    return new AmazonSimpleEmailServiceV2Client(RegionEndpoint.GetBySystemName(options.Region));
});
builder.Services.AddScoped<IInvitationEmailSender, SesInvitationEmailSender>();

builder.Services.AddScoped<IAreaRepository,             AreaRepository>();
builder.Services.AddScoped<IApplicationParameterRepository, ApplicationParameterRepository>();
builder.Services.AddScoped<IContactRepository,          ContactRepository>();
builder.Services.AddScoped<IClientInvitationRepository, ClientInvitationRepository>();
builder.Services.AddScoped<IEventContactLinkRepository, EventContactLinkRepository>();
builder.Services.AddScoped<IEventDocumentRepository,    EventDocumentRepository>();
builder.Services.AddScoped<IEventImageRepository,       EventImageRepository>();
builder.Services.AddScoped<IEventPriorityRepository,     EventPriorityRepository>();
builder.Services.AddScoped<IEventStatusRepository,      EventStatusRepository>();
builder.Services.AddScoped<IEventTypeRepository,        EventTypeRepository>();
builder.Services.AddScoped<IEmailTriageRepository,      EmailTriageRepository>();
builder.Services.AddScoped<IHomeEventsRepository,       HomeEventsRepository>();
builder.Services.AddScoped<IGlobalSearchRepository,     GlobalSearchRepository>();
builder.Services.AddScoped<INoteRepository,             NoteRepository>();
builder.Services.AddScoped<IOnboardingRepository,       OnboardingRepository>();
builder.Services.AddScoped<IProjectRepository,          ProjectRepository>();
builder.Services.AddScoped<IPropertySettingRepository,  PropertySettingRepository>();
builder.Services.AddScoped<IRecentItemRepository,       RecentItemRepository>();
builder.Services.AddScoped<IUserRepository,             UserRepository>();
builder.Services.AddSingleton<DocumentTextExtractor>();
builder.Services.AddHttpClient<PropertyExternalService>(client =>
{
    client.DefaultRequestHeaders.UserAgent.ParseAdd("HomeDiary/1.0 (local property management application)");
    client.Timeout = TimeSpan.FromSeconds(12);
});

builder.Services.AddControllers();
builder.Services.AddOpenApi();

// ── Pipeline ──────────────────────────────────────────────────────────────
var app = builder.Build();

if (app.Environment.IsDevelopment())
    app.MapOpenApi();

// Local development normally serves the API over HTTP and has no HTTPS port
// configured. Redirect only outside Development so ASP.NET does not emit
// "Failed to determine the https port for redirect" on every local request.
if (!app.Environment.IsDevelopment())
    app.UseHttpsRedirection();
app.UseCors();
app.UseAuthentication();
app.UseMiddleware<HomeDiaryUserAccessMiddleware>();
app.UseAuthorization();

// All controller endpoints require a valid Auth0 JWT
app.MapControllers().RequireAuthorization();

app.Run();
