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

            Console.WriteLine($"PayOS Service Initialized:");
            Console.WriteLine($"  ClientId: {_clientId}");
            Console.WriteLine($"  ApiKey: {_apiKey}");
            Console.WriteLine($"  ChecksumKey: {_checksumKey}");
            Console.WriteLine($"  CreateUrl: {_createUrl}");
            Console.WriteLine($"  ReturnUrl: {_returnUrl}");
            Console.WriteLine($"  CancelUrl: {_cancelUrl}");
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

                Console.WriteLine($"PayOS: Creating payment link for Order #{orderId}, Amount: {order.TotalPrice.Value}");

                // Thử các format signature khác nhau
                var signatureFormats = new[]
                {
                    // Format 1: MD5(checksumKey + orderCode + amount) - Phổ biến nhất
                    () => GenerateMD5Signature($"{_checksumKey}{order.OrderId}{order.TotalPrice.Value}"),
                    
                    // Format 2: MD5(orderCode + amount + checksumKey) - Format cũ
                    () => GenerateMD5Signature($"{order.OrderId}{order.TotalPrice.Value}{_checksumKey}"),
                    
                    // Format 3: HMAC-SHA256(orderCode + amount, checksumKey)
                    () => GenerateHMACSignature($"{order.OrderId}{order.TotalPrice.Value}", _checksumKey),
                    
                    // Format 4: SHA1(checksumKey + orderCode + amount)
                    () => GenerateSHA1Signature($"{_checksumKey}{order.OrderId}{order.TotalPrice.Value}")
                };

                foreach (var signatureFunc in signatureFormats)
                {
                    try
                    {
                        var signature = signatureFunc();
                        
                        // Tạo payload với signature này
                        var payload = new
                        {
                            orderCode = order.OrderId.ToString(),
                            amount = order.TotalPrice.Value,
                            description = $"Thanh toán đơn hàng #{order.OrderId} - SoulFashion",
                            cancelUrl = _cancelUrl,
                            returnUrl = _returnUrl,
                            signature = signature
                        };

                        Console.WriteLine($"PayOS: Trying signature: {signature}");
                        Console.WriteLine($"PayOS Request:");
                        Console.WriteLine($"  URL: {_createUrl}");
                        Console.WriteLine($"  Headers: x-client-id={_clientId}, x-api-key={_apiKey}");
                        Console.WriteLine($"  Payload: {JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true })}");

                        var req = new HttpRequestMessage(HttpMethod.Post, _createUrl)
                        {
                            Content = JsonContent.Create(payload)
                        };

                        req.Headers.Add("x-client-id", _clientId);
                        req.Headers.Add("x-api-key", _apiKey);

                        var res = await _http.SendAsync(req);
                        var raw = await res.Content.ReadAsStringAsync();

                        Console.WriteLine($"PayOS Response:");
                        Console.WriteLine($"  Status: {res.StatusCode}");
                        Console.WriteLine($"  Content-Type: {res.Content.Headers.ContentType}");
                        Console.WriteLine($"  Body: {raw}");

                        if (!res.IsSuccessStatusCode)
                        {
                            Console.WriteLine($"PayOS: HTTP error, trying next signature format");
                            continue;
                        }

                        using var doc = JsonDocument.Parse(raw);
                        var root = doc.RootElement;

                        if (root.TryGetProperty("code", out var codeEl))
                        {
                            var code = codeEl.GetString();
                            if (code == "00" || code == "0")
                            {
                                if (root.TryGetProperty("data", out var dataEl) && dataEl.ValueKind != JsonValueKind.Null)
                                {
                                    string checkoutUrl = dataEl.GetProperty("checkoutUrl").GetString() 
                                        ?? throw new Exception($"checkoutUrl not found in response: {raw}");
                                    
                                    string? qrCode = dataEl.TryGetProperty("qrCode", out var qrEl) ? qrEl.GetString() : null;

                                    Console.WriteLine($"PayOS: SUCCESS with signature: {signature}");
                                    return (checkoutUrl, qrCode, raw);
                                }
                            }
                            else if (code == "201") // Signature error, try next format
                            {
                                Console.WriteLine($"PayOS: Signature error (Code: 201), trying next format");
                                continue;
                            }
                            else
                            {
                                var desc = root.TryGetProperty("desc", out var descEl) ? descEl.GetString() : "Unknown error";
                                throw new Exception($"PayOS business error: {desc} (Code: {code})");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"PayOS: Error with signature format: {ex.Message}");
                        continue;
                    }
                }

                throw new Exception("All signature formats failed. Check console logs for details.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"PayOS CreatePaymentLinkAsync Error: {ex.Message}");
                throw;
            }
        }

        private string GenerateMD5Signature(string data)
        {
            using var md5 = MD5.Create();
            var hash = md5.ComputeHash(Encoding.UTF8.GetBytes(data));
            return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
        }

        private string GenerateHMACSignature(string data, string key)
        {
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(key));
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
            return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
        }

        private string GenerateSHA1Signature(string data)
        {
            using var sha1 = SHA1.Create();
            var hash = sha1.ComputeHash(Encoding.UTF8.GetBytes(data));
            return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
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

                Console.WriteLine($"PayOS Webhook: Verifying webhook");
                Console.WriteLine($"  Raw Body: {rawBody}");
                Console.WriteLine($"  Received Signature: {signatureHeader}");

                // Verify với checksumKey
                using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_checksumKey));
                var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(rawBody));
                var computed = BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                var received = signatureHeader.Trim().ToLowerInvariant();

                var isValid = string.Equals(computed, received, StringComparison.Ordinal);

                Console.WriteLine($"PayOS Webhook Verification:");
                Console.WriteLine($"  Computed Signature: {computed}");
                Console.WriteLine($"  Received Signature: {received}");
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
