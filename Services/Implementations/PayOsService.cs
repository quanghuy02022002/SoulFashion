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
        private readonly string _checksumKey;

        public PayOsService(IConfiguration config, IHttpClientFactory httpClientFactory, IOrderRepository orderRepo)
        {
            _http = httpClientFactory.CreateClient();
            _orderRepo = orderRepo;

            _clientId = config["PayOS:ClientId"] ?? "";
            _apiKey = config["PayOS:ApiKey"] ?? "";
            _checksumKey = config["PayOS:ChecksumKey"] ?? "";
            _createUrl = config["PayOS:CreateUrl"] ?? "";
            _returnUrl = config["PayOS:ReturnUrl"] ?? "";
            _cancelUrl = config["PayOS:CancelUrl"] ?? "";
        }

        // Tạo link thanh toán PayOS
        public async Task<(string checkoutUrl, string? qrCode, string rawResponse)> CreatePaymentLinkAsync(int orderId)
        {
            try
            {
                var order = await _orderRepo.GetByIdAsync(orderId)
                            ?? throw new Exception($"Order #{orderId} not found");

                if (order.TotalPrice == null)
                    throw new Exception($"Order #{orderId} chưa có TotalPrice");

                // ✅ Payload đúng theo PayOS API v2 specification
                var payload = new
                {
                    orderCode = order.OrderId.ToString(),
                    amount = order.TotalPrice.Value,
                    description = $"Thanh toán đơn hàng #{order.OrderId} - SoulFashion",
                    cancelUrl = _cancelUrl,
                    returnUrl = _returnUrl,
                    signature = GenerateSignature(order.OrderId.ToString(), order.TotalPrice.Value)
                };

                // Log payload để debug
                Console.WriteLine($"PayOS Request Payload: {JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true })}");

                var req = new HttpRequestMessage(HttpMethod.Post, _createUrl)
                {
                    Content = JsonContent.Create(payload)
                };

                // Headers theo PayOS API spec
                req.Headers.Add("x-client-id", _clientId);
                req.Headers.Add("x-api-key", _apiKey);
                req.Headers.Add("Content-Type", "application/json");

                var res = await _http.SendAsync(req);
                var raw = await res.Content.ReadAsStringAsync();

                Console.WriteLine($"PayOS Response Status: {res.StatusCode}");
                Console.WriteLine($"PayOS Response Body: {raw}");

                if (!res.IsSuccessStatusCode)
                    throw new Exception($"PayOS API error: Status {res.StatusCode}, Response: {raw}");

                using var doc = JsonDocument.Parse(raw);
                var root = doc.RootElement;

                // Kiểm tra response format
                if (!root.TryGetProperty("code", out var codeEl))
                    throw new Exception($"PayOS response missing 'code': {raw}");

                var code = codeEl.GetString();
                if (code != "00" && code != "0")
                {
                    var desc = root.TryGetProperty("desc", out var descEl) ? descEl.GetString() : "Unknown error";
                    throw new Exception($"PayOS business error: {desc} (Code: {code})");
                }

                if (!root.TryGetProperty("data", out var dataEl) || dataEl.ValueKind == JsonValueKind.Null)
                    throw new Exception($"PayOS response missing 'data': {raw}");

                string checkoutUrl = dataEl.GetProperty("checkoutUrl").GetString() 
                    ?? throw new Exception($"checkoutUrl not found in response: {raw}");
                
                string? qrCode = dataEl.TryGetProperty("qrCode", out var qrEl) ? qrEl.GetString() : null;

                return (checkoutUrl, qrCode, raw);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"PayOS CreatePaymentLinkAsync Error: {ex.Message}");
                throw;
            }
        }

        private string GenerateSignature(string orderCode, decimal amount)
        {
            try
            {
                // ✅ Format signature theo PayOS API v2: orderCode + amount + checksumKey
                var data = $"{orderCode}{amount}{_checksumKey}";
                using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_checksumKey));
                var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
                var signature = BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                
                Console.WriteLine($"PayOS Signature Debug:");
                Console.WriteLine($"  Data: {data}");
                Console.WriteLine($"  ChecksumKey: {_checksumKey}");
                Console.WriteLine($"  Signature: {signature}");
                
                return signature;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"PayOS GenerateSignature Error: {ex.Message}");
                throw;
            }
        }

        // Xác thực webhook từ PayOS
        public bool VerifyWebhook(string rawBody, string signatureHeader)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(signatureHeader))
                {
                    Console.WriteLine("PayOS Webhook: Empty signature header");
                    return false;
                }

                // ✅ Verify webhook với checksumKey
                using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_checksumKey));
                var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(rawBody));
                var computed = BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                var received = signatureHeader.Trim().ToLowerInvariant();

                var isValid = string.Equals(computed, received, StringComparison.Ordinal);
                
                Console.WriteLine($"PayOS Webhook Verification:");
                Console.WriteLine($"  Raw Body: {rawBody}");
                Console.WriteLine($"  Received Signature: {received}");
                Console.WriteLine($"  Computed Signature: {computed}");
                Console.WriteLine($"  Is Valid: {isValid}");

                return isValid;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"PayOS VerifyWebhook Error: {ex.Message}");
                return false;
            }
        }
    }
}
