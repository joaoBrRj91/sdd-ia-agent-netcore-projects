using PaymentService.Api.Application.Services;
using PaymentService.Api.Models;

namespace PaymentService.Api.Endpoints;

public static class PaymentRoutes
{
    public static void MapPaymentEndpoints(this WebApplication app)
    {
        app.MapPost("/payments", async (PaymentIntentRequest req, ICreatePaymentService service) =>
        {
            try
            {
                var response = await service.CreatePaymentAsync(req);
                return Results.Ok(response);
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        });

        app.MapPost("/payments/callback", async (CallbackNotification notification, IProcessPaymentCallbackService service) =>
        {
            try
            {
                await service.ProcessCallbackAsync(notification);
                return Results.NoContent();
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        });

        app.MapGet("/payments/{ticket:guid}", async (Guid ticket, IGetPaymentStatusService service) =>
        {
            var response = await service.GetPaymentStatusAsync(ticket);
            if (response is null)
                return Results.NotFound();

            return Results.Ok(response);
        });
    }
}
