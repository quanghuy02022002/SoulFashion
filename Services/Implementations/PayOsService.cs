using Microsoft.Extensions.Configuration;
using Repositories.Interfaces;
using Services.Interfaces;
using System.Net.Http;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Services.Implementations
{
    public class PayOsService : IPayOsService
    {
        private readonly HttpClient _http;
        private readonly IOrderRepository _orderRepo;
        private readonly string _clientId;
        private readonly string _apiKey;
        private readonly string _createUrl;
        private readonly string _returnUrl;
        private readonly string _cancelUrl;

        public PayOsService(IConfiguration config, IHttpClientFactory httpClientFactory, IOrderRepository orderRepo)
        {
            _http = httpClientFactory.CreateClient();
            _orderRepo = orderRepo;

            _clientId = config["PayOS:ClientId"] ?? "";
            _apiKey = config["PayOS:ApiKey"] ?? "";
            _createUrl = config["PayOS:CreateUrl"] ?? "";
            _returnUrl = config["PayOS:ReturnUrl"] ?? "";
            _cancelUrl = config["PayOS:CancelUrl"] ?? "";
        }

        // Tạo link thanh toán PayOS
        public async Task<(string checkoutUrl, string? qrCode, string rawResponse)> CreatePaymentLinkAsync(int orderId)
        {
            var order = await _orderRepo.GetByIdAsync(orderId)
                        ?? throw new Exception($"Order #{orderId} not found");

            if (order.TotalPrice == null)
                throw new Exception($"Order #{orderId} chưa có TotalPrice");

            // Payload theo API test PayOS
            var payload = new
            {
                paymentId = 0,
                orderId = order.OrderId,
                paymentMethod = "payos",
                paymentStatus = "pending",
                paidAt = DateTime.UtcNow
            };

            var req = new HttpRequestMessage(HttpMethod.Post, _createUrl)
            {
                Content = JsonContent.Create(payload)
            };
            req.Headers.Add("x-client-id", _clientId);
            req.Headers.Add("x-api-key", _apiKey);

            var res = await _http.SendAsync(req);
            var raw = await res.Content.ReadAsStringAsync();

            if (!res.IsSuccessStatusCode)
                throw new Exception($"PayOS create error: {raw}");

            using var doc = JsonDocument.Parse(raw);
            var root = doc.RootElement;

            if (!root.TryGetProperty("data", out var dataEl) || dataEl.ValueKind == JsonValueKind.Null)
                throw new Exception($"PayOS response missing 'data': {raw}");

            string checkoutUrl = dataEl.GetProperty("checkoutUrl").GetString() ?? throw new Exception("checkoutUrl not found");
            string? qrCode = dataEl.TryGetProperty("qrCode", out var qrEl) ? qrEl.GetString() : null;

            return (checkoutUrl, qrCode, raw);
        }

        // Xác thực webhook từ PayOS
        public bool VerifyWebhook(string rawBody, string signatureHeader)
        {
            if (string.IsNullOrWhiteSpace(signatureHeader)) return false;

            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_apiKey));
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(rawBody));
            var computed = BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();

            return string.Equals(computed, signatureHeader.Trim().ToLowerInvariant(), StringComparison.Ordinal);
        }
    }
}
