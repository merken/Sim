using System.Text.Json;

namespace MyShop.Business;

public class PaymentRequest
{
    public string CardNumber { get; set; }
    public decimal Amount { get; set; }
    public string[] Products { get; set; }
}

public class PaymentResult
{
    public bool Success { get; set; }
    public string Message { get; set; }
}

public interface IPaymentService
{
    Task<PaymentResult> SubmitPayment(PaymentRequest request);
}

public class PaymentService(HttpClient client) : IPaymentService
{
    public async Task<PaymentResult> SubmitPayment(PaymentRequest request)
    {
        var payload = JsonSerializer.Serialize(request, JsonSerializerOptions.Web);
        var stringContent = new StringContent(payload, System.Text.Encoding.UTF8, "application/json");
        var response = await client.PostAsync("api/payment", stringContent);
        if (response.IsSuccessStatusCode)
        {
            var responseContent = await response.Content.ReadAsStringAsync();
            var paymentResult = JsonSerializer.Deserialize<PaymentResult>(responseContent, JsonSerializerOptions.Web);
            return paymentResult;
        }

        return new PaymentResult() { Success = false, Message = "Payment service offline." };
    }
}
