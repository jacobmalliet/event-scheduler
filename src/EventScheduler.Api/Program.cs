using EventScheduler.Api.Common;
using EventScheduler.Infrastructure;
using FluentValidation;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddSingleton(TimeProvider.System);

// Tests set Database:Name to give each test its own in-memory store.
builder.Services.AddInfrastructure(builder.Configuration["Database:Name"] ?? "EventScheduler");
builder.Services.AddValidatorsFromAssemblyContaining<Program>();
builder.Services.AddSlices(typeof(Program).Assembly);
builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapDefaultEndpoints();
app.MapSlices();

app.Run();

/// <summary>
/// Declared so the API tests can host the app with <c>WebApplicationFactory&lt;Program&gt;</c>.
/// </summary>
public partial class Program;
