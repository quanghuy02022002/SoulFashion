using Google.Apis.Http;
using Microsoft.Extensions.Configuration;
using Repositories.Interfaces;
using Services.Interfaces;
using System.Net.Http;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Net.Http;

namespace Services.Implementations
{
    public class PayOsService : IPayOsService
    {
        private readonly HttpClient _http;
        private readonly IOrderRepository _orderRepo;

        private readonly string _clientId;
        private readonly string _apiKey;
        private readonly string _checksumKey;
        private readonly string _createUrl;
        private readonly string _returnUrl;
        private readonly string _cancelUrl;

        public PayOsService(
            IConfiguration config,
            System.Net.Http.IHttpClientFactory httpClientFactory,
            IOrderRepository orderRepo)
        {
            _http = httpClientFactory.CreateClient();
            _orderRepo = orderRepo;

            _clientId = config["PayOS:ClientId"] ?? "";
            _apiKey = config["PayOS:ApiKey"] ?? "";
            _checksumKey = config["PayOS:ChecksumKey"] ?? "";
            _createUrl = config["PayOS:CreateUrl"] ?? "https://api-merchant.payos.vn/v2/payment-requests";
            _returnUrl = config["PayOS:ReturnUrl"] ?? "";
            _cancelUrl = config["PayOS:CancelUrl"] ?? "";
        }

        public async Task<(string checkoutUrl, string? qrCode, string rawResponse)>
            CreatePaymentLinkAsync(int orderId, string orderCode, string? description = null)
        {
            // 1) Lấy tổng tiền từ Order (đồng bộ với PaymentService của bạn)
            var order = await _orderRepo.GetByIdAsync(orderId)
                        ?? throw new Exception($"Order #{orderId} not found");

            if (order.TotalPrice is null)
                throw new Exception($"Order #{orderId} chưa có TotalPrice");

            // PayOS nhận đơn vị VND (đồng), dạng số nguyên
            var amountVnd = (long)decimal.Round(order.TotalPrice.Value, 0, MidpointRounding.AwayFromZero);

            // 2) Build payload theo spec PayOS
            var payload = new
            {
                orderCode = orderCode,                                        // duy nhất
                amount = amountVnd,                                        // VND
                description = description ?? $"Thanh toán đơn hàng #{orderId}",
                returnUrl = _returnUrl,
                cancelUrl = _cancelUrl
                // có thể bổ sung items, buyer, expiredAt... nếu cần
            };

            // 3) Gọi API PayOS
            var req = new HttpRequestMessage(HttpMethod.Post, _createUrl)
            {
                Content = JsonContent.Create(payload)
            };
            req.Headers.Add("x-client-id", _clientId);
            req.Headers.Add("x-api-key", _apiKey);

            var res = await _http.SendAsync(req);
            var raw = await res.Content.ReadAsStringAsync();
            if (!res.IsSuccessStatusCode)
                throw new Exception($"PayOS create link failed: {raw}");

            // 4) Lấy checkoutUrl / qrCode từ response
            using var doc = JsonDocument.Parse(raw);
            var root = doc.RootElement;

            // PayOS thường trả { code, desc, data: { checkoutUrl, qrCode, ... } }
            string checkoutUrl = TryGet(root, "data.checkoutUrl")
                              ?? TryGet(root, "checkoutUrl")
                              ?? throw new Exception("checkoutUrl not found in PayOS response");
            string? qrCode = TryGet(root, "data.qrCode") ?? TryGet(root, "qrCode");

            return (checkoutUrl, qrCode, raw);

            static string? TryGet(JsonElement el, string path)
            {
                var cur = el;
                foreach (var seg in path.Split('.'))
                {
                    if (cur.ValueKind != JsonValueKind.Object || !cur.TryGetProperty(seg, out cur))
                        return null;
                }
                return cur.ValueKind == JsonValueKind.String ? cur.GetString() : cur.ToString();
            }
        }

        public bool VerifyWebhook(string rawBody, string signatureHeader)
        {
            if (string.IsNullOrWhiteSpace(signatureHeader)) return false;

            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_checksumKey));
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(rawBody));
            var computed = BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();

            return string.Equals(computed, signatureHeader.Trim().ToLowerInvariant(), StringComparison.Ordinal);
        }
    }
}
