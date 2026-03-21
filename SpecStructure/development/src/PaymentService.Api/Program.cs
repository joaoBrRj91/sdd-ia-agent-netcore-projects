using System.Text.Json.Serialization;
using PaymentService.Api.Application.Commands;
using PaymentService.Api.Application.Handlers;
using PaymentService.Api.Application.DTOs;
using PaymentService.Api.Application.Services;
using PaymentService.Api.Domain.Repositories;
using PaymentService.Api.Infrastructure.Repositories;
using PaymentService.Api.Endpoints;

var builder = WebApplication.CreateBuilder(args);

builder.Services.ConfigureHttpJsonOptions(o =>
{
    o.SerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.SnakeCaseLower;
    o.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
});

// Register infrastructure
builder.Services.AddScoped<IPaymentRepository, InMemoryPaymentRepository>();

// Register command handlers
builder.Services.AddScoped<ICommandHandler<CreatePaymentCommand, CreatePaymentResponseDto>, CreatePaymentCommandHandler>();
builder.Services.AddScoped<ICommandHandler<ProcessPaymentCallbackCommand, Unit>, ProcessPaymentCallbackCommandHandler>();

// Register application services
builder.Services.AddScoped<ICreatePaymentService, CreatePaymentService>();
builder.Services.AddScoped<IProcessPaymentCallbackService, ProcessPaymentCallbackService>();

var app = builder.Build();

if (!app.Environment.IsEnvironment("Testing"))
{
    app.UseHttpsRedirection();
}

// Map endpoints
app.MapPaymentEndpoints();

app.Run();
