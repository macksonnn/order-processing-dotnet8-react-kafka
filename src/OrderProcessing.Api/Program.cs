using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using OrderProcessing.Api.Authentication;
using OrderProcessing.Api.ExceptionHandling;
using OrderProcessing.Application;
using OrderProcessing.Application.Abstractions;
using OrderProcessing.Infrastructure;
using OrderProcessing.Infrastructure.Health;
using OrderProcessing.Infrastructure.Persistence;
using AuthenticationOptions = OrderProcessing.Api.Authentication.AuthenticationOptions;

var builder = WebApplication.CreateBuilder(args);

var authenticationOptions = builder.Configuration
    .GetSection(AuthenticationOptions.SectionName)
    .Get<AuthenticationOptions>()
    ?? new AuthenticationOptions();

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, HttpCurrentUser>();
builder.Services.AddScoped<ICorrelationIdAccessor, CorrelationIdAccessor>();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Pedidos Santa Cruz",
        Version = "v1"
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Token do POST /api/auth/login. Exemplo: Bearer {token}",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
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

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = authenticationOptions.Authority;
        options.Audience = authenticationOptions.Audience;
        options.RequireHttpsMetadata = authenticationOptions.RequireHttpsMetadata;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidAudience = authenticationOptions.Audience,
            NameClaimType = "preferred_username"
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        var origins = builder.Configuration.GetSection("Cors:Origins").Get<string[]>()
            ?? ["http://localhost:5173"];

        policy.WithOrigins(origins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .WithExposedHeaders(CorrelationIdMiddleware.HeaderName);
    });
});

builder.Services.AddHealthChecks()
    .AddCheck<PostgresHealthCheck>("postgres");

var app = builder.Build();

var connectionString = app.Configuration.GetConnectionString("Database")
    ?? throw new InvalidOperationException("Connection string 'Database' is not configured.");

await DatabaseUpgrader.UpgradeWithRetryAsync(
    connectionString,
    app.Logger,
    CancellationToken.None);

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseExceptionHandler();
app.UseCors("Frontend");
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health").AllowAnonymous();

app.Run();
