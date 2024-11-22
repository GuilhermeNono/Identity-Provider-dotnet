using System.Security.Claims;
using Auth.IdentityProvider.Extensions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGenWithAuth(builder.Configuration);

builder.Services.AddAuthorization();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(x =>
    {
        x.RequireHttpsMetadata = false;
        x.Audience = builder.Configuration["Authentication:Audience"];
        x.MetadataAddress = builder.Configuration["Authentication:MetadataAddress"]!;
        x.TokenValidationParameters = new TokenValidationParameters
        {
            ValidIssuer = builder.Configuration["Authentication:ValidIssuer"],
        };
    });

builder.Services
    .AddOpenTelemetry()
    .ConfigureResource(resourceBuilder => resourceBuilder.AddService("Keycloak.Auth.Api"))
    .WithTracing(tracing =>
    {
        tracing.AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation();

        tracing.AddOtlpExporter();
    });

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// app.UseHttpsRedirection();

app.MapGet("users/me", (ClaimsPrincipal claims) =>
{
    return claims.Claims.ToDictionary(x => x.Type, x => x.Value);
}).RequireAuthorization();

app.UseAuthentication();

app.UseAuthorization();

app.Run();
