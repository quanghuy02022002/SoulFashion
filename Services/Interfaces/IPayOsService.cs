using System.Threading.Tasks;

namespace Services.Interfaces
{
    public interface IPayOsService
    {
        /// <summary>
        /// Tạo link thanh toán + QR cho order; tự lấy amount từ OrderRepo
        /// </summary>
        Task<(string checkoutUrl, string? qrCode, string rawResponse)>
            CreatePaymentLinkAsync(int orderId, string orderCode, string? description = null);

        /// <summary>
        /// Verify chữ ký webhook (HMAC-SHA256 trên raw body với ChecksumKey)
        /// </summary>
        bool VerifyWebhook(string rawBody, string signatureHeader);
    }
}
