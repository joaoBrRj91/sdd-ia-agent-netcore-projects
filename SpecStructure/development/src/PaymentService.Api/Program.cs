using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using PaymentService.Api.Application.Commands;
using PaymentService.Api.Application.Handlers;
using PaymentService.Api.Application.DTOs;
using PaymentService.Api.Application.Services;
using PaymentService.Api.Application.Cache;
using PaymentService.Api.Domain.Repositories;
using PaymentService.Api.Infrastructure.Repositories;
using PaymentService.Api.Infrastructure.Cache;
using PaymentService.Api.Infrastructure.Messaging;
using PaymentService.Api.Application.Messaging;
using PaymentService.Api.Endpoints;

var builder = WebApplication.CreateBuilder(args);

builder.Services.ConfigureHttpJsonOptions(o =>
{
    o.SerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.SnakeCaseLower;
    o.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
});

// Register infrastructure
builder.Services.AddMemoryCache();
// Singleton so the in-memory store is shared across HTTP requests (required for create → callback → GET flows).
builder.Services.AddSingleton<IPaymentRepository, InMemoryPaymentRepository>();
builder.Services.AddScoped<ICacheProvider, MemoryCacheProvider>();

// Bind CacheSettings from configuration
builder.Services.Configure<CacheSettings>(
    builder.Configuration.GetSection("CacheSettings"));

// Register command handlers
builder.Services.AddScoped<ICommandHandler<CreatePaymentCommand, CreatePaymentResponseDto>, CreatePaymentCommandHandler>();
builder.Services.AddSingleton<InMemoryIntegrationMessageQueue>();
builder.Services.AddSingleton<IIntegrationMessagePublisher>(sp =>
    sp.GetRequiredService<InMemoryIntegrationMessageQueue>());
builder.Services.AddHostedService<IntegrationMessageConsumerHostedService>();

builder.Services.AddScoped<ICommandHandler<ProcessPaymentCallbackCommand, ProcessPaymentCallbackResult>, ProcessPaymentCallbackCommandHandler>();

// Register application services
builder.Services.AddScoped<ICreatePaymentService, CreatePaymentService>();
builder.Services.AddScoped<IProcessPaymentCallbackService, ProcessPaymentCallbackService>();

// Decorator pattern for IGetPaymentStatusService
builder.Services.AddKeyedScoped<IGetPaymentStatusService, GetPaymentStatusService>("real");
builder.Services.AddScoped<IGetPaymentStatusService>(sp =>
    new CachedGetPaymentStatusService(
        sp.GetRequiredKeyedService<IGetPaymentStatusService>("real"),
        sp.GetRequiredService<ICacheProvider>(),
        sp.GetRequiredService<IOptions<CacheSettings>>()
    ));

var app = builder.Build();

if (!app.Environment.IsEnvironment("Testing"))
{
    app.UseHttpsRedirection();
}

// Map endpoints
app.MapPaymentEndpoints();

app.Run();
