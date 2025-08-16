using Microsoft.Extensions.Configuration;
using Repositories.Interfaces;
using System.Net.Http;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using Services.Interfaces;

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

        public PayOsService(IConfiguration config, IHttpClientFactory httpClientFactory, IOrderRepository orderRepo)
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

        public async Task<(string checkoutUrl, string? qrCode, string rawResponse)> CreatePaymentLinkAsync(int orderId, string orderCode, string? description = null)
        {
            var order = await _orderRepo.GetByIdAsync(orderId) ?? throw new Exception($"Order #{orderId} not found");
            if (order.TotalPrice is null) throw new Exception($"Order #{orderId} chưa có TotalPrice");

            long amountVnd = (long)decimal.Round(order.TotalPrice.Value, 0, MidpointRounding.AwayFromZero);

            // Payload PayOS
            var payload = new Dictionary<string, object>
            {
                ["orderCode"] = orderCode,
                ["amount"] = amountVnd,
                ["description"] = description ?? $"Thanh toán đơn hàng #{orderId}",
                ["returnUrl"] = _returnUrl,
                ["cancelUrl"] = _cancelUrl
            };

            // Signature: alphabetically by key
            var sorted = payload.OrderBy(k => k.Key).Select(k => $"{k.Key}={k.Value}");
            var dataString = string.Join("&", sorted);
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_checksumKey));
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(dataString));
            payload["signature"] = BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();

            // Send request
            HttpResponseMessage res;
            string raw;
            try
            {
                var req = new HttpRequestMessage(HttpMethod.Post, _createUrl) { Content = JsonContent.Create(payload) };
                req.Headers.Add("x-client-id", _clientId);
                req.Headers.Add("x-api-key", _apiKey);

                res = await _http.SendAsync(req);
                raw = await res.Content.ReadAsStringAsync();

                if (!res.IsSuccessStatusCode)
                    throw new Exception($"PayOS returned error: {res.StatusCode}, {raw}");
            }
            catch (Exception ex)
            {
                throw new Exception($"PayOS request failed: {ex.Message}");
            }

            // Parse response
            using var doc = JsonDocument.Parse(raw);
            var root = doc.RootElement;
            string checkoutUrl = TryGet(root, "data.checkoutUrl") ?? throw new Exception($"checkoutUrl not found: {raw}");
            string? qrCode = TryGet(root, "data.qrCode");

            return (checkoutUrl, qrCode, raw);

            static string? TryGet(JsonElement el, string path)
            {
                var cur = el;
                foreach (var seg in path.Split('.'))
                    if (cur.ValueKind != JsonValueKind.Object || !cur.TryGetProperty(seg, out cur))
                        return null;
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
