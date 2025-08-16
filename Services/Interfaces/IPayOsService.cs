public interface IPayOsService
{
    Task<(string checkoutUrl, string? qrCode, string rawResponse)> CreatePaymentLinkAsync(int orderId);
    bool VerifyWebhook(string rawBody, string signatureHeader);
}
