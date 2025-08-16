using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
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
        private readonly ILogger<PayOsService> _logger;
        private readonly string _clientId;
        private readonly string _apiKey;
        private readonly string _createUrl;
        private readonly string _returnUrl;
        private readonly string _cancelUrl;
        private readonly string _checksumKey;

        public PayOsService(IConfiguration config, IHttpClientFactory httpClientFactory, IOrderRepository orderRepo, ILogger<PayOsService> logger)
        {
            _http = httpClientFactory.CreateClient();
            _orderRepo = orderRepo;
            _logger = logger;

            _clientId = config["PayOS:ClientId"] ?? "";
            _apiKey = config["PayOS:ApiKey"] ?? "";
            _checksumKey = config["PayOS:ChecksumKey"] ?? "";
            _createUrl = config["PayOS:CreateUrl"] ?? "";
            _returnUrl = config["PayOS:ReturnUrl"] ?? "";
            _cancelUrl = config["PayOS:CancelUrl"] ?? "";

            _logger.LogInformation("PayOS Service Initialized: ClientId={ClientId}, ApiKey={ApiKey}, CreateUrl={CreateUrl}", 
                _clientId, _apiKey, _createUrl);
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

                _logger.LogInformation("PayOS: Creating payment link for Order #{OrderId}, Amount: {TotalPrice}", orderId, order.TotalPrice.Value);

                // Test kết nối đến PayOS trước
                try
                {
                    _logger.LogInformation("PayOS: Testing connection to PayOS API...");
                    var testReq = new HttpRequestMessage(HttpMethod.Get, "https://api-merchant.payos.vn");
                    var testRes = await _http.SendAsync(testReq);
                    _logger.LogInformation("PayOS: Test connection status: {StatusCode}", testRes.StatusCode);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning("PayOS: Test connection failed: {Message}", ex.Message);
                }

                // Test với request đơn giản nhất
                try
                {
                    _logger.LogInformation("PayOS: Testing minimal request to PayOS API...");
                    var minimalPayload = new
                    {
                        orderCode = "TEST123",
                        amount = 1000,
                        description = "Test payment",
                        cancelUrl = "https://example.com/cancel",
                        returnUrl = "https://example.com/return"
                    };

                    var minimalReq = new HttpRequestMessage(HttpMethod.Post, _createUrl)
                    {
                        Content = JsonContent.Create(minimalPayload)
                    };
                    minimalReq.Headers.Add("x-client-id", _clientId);
                    minimalReq.Headers.Add("x-api-key", _apiKey);

                    var minimalRes = await _http.SendAsync(minimalReq);
                    var minimalRaw = await minimalRes.Content.ReadAsStringAsync();

                    _logger.LogInformation("PayOS: Minimal test response:");
                    _logger.LogInformation("  Status: {StatusCode}", minimalRes.StatusCode);
                    _logger.LogInformation("  Body: {RawBody}", minimalRaw);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning("PayOS: Minimal test failed: {Message}", ex.Message);
                }

                // Thử các format signature khác nhau
                var signatureFormats = new[]
                {
                    // Format 0: Không có signature (test xem PayOS có chấp nhận không)
                    () => (string?)null,
                    
                    // Format 1: MD5(checksumKey + orderCode + amount) - Phổ biến nhất
                    () => GenerateMD5Signature($"{_checksumKey}{order.OrderId}{order.TotalPrice.Value}"),
                    
                    // Format 2: MD5(orderCode + amount + checksumKey) - Format cũ
                    () => GenerateMD5Signature($"{order.OrderId}{order.TotalPrice.Value}{_checksumKey}"),
                    
                    // Format 3: HMAC-SHA256(orderCode + amount, checksumKey)
                    () => GenerateHMACSignature($"{order.OrderId}{order.TotalPrice.Value}", _checksumKey),
                    
                    // Format 4: SHA1(checksumKey + orderCode + amount)
                    () => GenerateSHA1Signature($"{_checksumKey}{order.OrderId}{order.TotalPrice.Value}"),
                    
                    // Format 5: MD5(orderCode + amount) - Đơn giản nhất
                    () => GenerateMD5Signature($"{order.OrderId}{order.TotalPrice.Value}")
                };

                int attemptCount = 0;
                foreach (var signatureFunc in signatureFormats)
                {
                    attemptCount++;
                    try
                    {
                        var signature = signatureFunc();
                        
                        _logger.LogInformation("PayOS: Attempt {AttemptCount}/6 - Trying signature: {Signature}", 
                            attemptCount, signature ?? "NULL");
                        
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

                        _logger.LogInformation("PayOS: Request details:");
                        _logger.LogInformation("  URL: {CreateUrl}", _createUrl);
                        _logger.LogInformation("  Headers: x-client-id={ClientId}, x-api-key={ApiKey}", _clientId, _apiKey);
                        
                        try
                        {
                            var payloadJson = JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true });
                            _logger.LogInformation("  Payload: {Payload}", payloadJson);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "PayOS: Error serializing payload");
                        }

                        var req = new HttpRequestMessage(HttpMethod.Post, _createUrl)
                        {
                            Content = JsonContent.Create(payload)
                        };

                        // Nếu signature là null, loại bỏ field signature khỏi JSON
                        if (signature == null)
                        {
                            var jsonContent = JsonSerializer.Serialize(new
                            {
                                orderCode = order.OrderId.ToString(),
                                amount = order.TotalPrice.Value,
                                description = $"Thanh toán đơn hàng #{order.OrderId} - SoulFashion",
                                cancelUrl = _cancelUrl,
                                returnUrl = _returnUrl
                            });
                            req.Content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                        }

                        req.Headers.Add("x-client-id", _clientId);
                        req.Headers.Add("x-api-key", _apiKey);

                        _logger.LogInformation("PayOS: Sending request...");
                        var res = await _http.SendAsync(req);
                        var raw = await res.Content.ReadAsStringAsync();

                        _logger.LogInformation("PayOS: Response received:");
                        _logger.LogInformation("  Status: {StatusCode}", res.StatusCode);
                        _logger.LogInformation("  Content-Type: {ContentType}", res.Content.Headers.ContentType);
                        _logger.LogInformation("  Body: {RawBody}", raw);

                        if (!res.IsSuccessStatusCode)
                        {
                            _logger.LogWarning("PayOS: HTTP error {StatusCode}, trying next signature format", res.StatusCode);
                            continue;
                        }

                        JsonDocument doc;
                        try
                        {
                            doc = JsonDocument.Parse(raw);
                        }
                        catch (JsonException ex)
                        {
                            _logger.LogError(ex, "PayOS: Invalid JSON response: {RawBody}", raw);
                            continue;
                        }

                        using (doc)
                        {
                            var root = doc.RootElement;

                            if (root.TryGetProperty("code", out var codeEl))
                            {
                                var code = codeEl.GetString();
                                _logger.LogInformation("PayOS: Response code: {Code}", code);
                                
                                if (code == "00" || code == "0")
                                {
                                    if (root.TryGetProperty("data", out var dataEl) && dataEl.ValueKind != JsonValueKind.Null)
                                    {
                                        string checkoutUrl = dataEl.GetProperty("checkoutUrl").GetString() 
                                            ?? throw new Exception($"checkoutUrl not found in response: {raw}");
                                        
                                        string? qrCode = dataEl.TryGetProperty("qrCode", out var qrEl) ? qrEl.GetString() : null;

                                        _logger.LogInformation("PayOS: SUCCESS with signature: {Signature}", signature);
                                        return (checkoutUrl, qrCode, raw);
                                    }
                                }
                                else if (code == "201") // Signature error, try next format
                                {
                                    var desc = root.TryGetProperty("desc", out var descEl) ? descEl.GetString() : "Unknown error";
                                    _logger.LogWarning("PayOS: Signature error (Code: 201) - {Desc}, trying next format", desc);
                                    continue;
                                }
                                else
                                {
                                    var desc = root.TryGetProperty("desc", out var descEl) ? descEl.GetString() : "Unknown error";
                                    _logger.LogWarning("PayOS: Business error (Code: {Code}) - {Desc}, trying next format", code, desc);
                                    continue;
                                }
                            }
                            else
                            {
                                _logger.LogWarning("PayOS: Response missing 'code' field, trying next format");
                                continue;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "PayOS: Error with signature format {AttemptCount}: {Message}", attemptCount, ex.Message);
                        continue;
                    }
                }

                _logger.LogError("PayOS: All signature formats failed. Summary of attempts:");
                _logger.LogError("  - Tried 6 different signature formats (including no signature)");
                _logger.LogError("  - All returned business errors or signature errors");
                _logger.LogError("  - Check if PayOS API endpoint is correct: {CreateUrl}", _createUrl);
                _logger.LogError("  - Check if credentials are valid: ClientId={ClientId}, ApiKey={ApiKey}", _clientId, _apiKey);
                throw new Exception("All signature formats failed. Check application logs for details.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "PayOS CreatePaymentLinkAsync Error: {Message}", ex.Message);
                throw;
            }
        }

        private string GenerateMD5Signature(string data)
        {
            using var md5 = MD5.Create();
            var hash = md5.ComputeHash(Encoding.UTF8.GetBytes(data));
            var signature = BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
            
            _logger.LogDebug("PayOS MD5 Signature: Data={Data}, Signature={Signature}", data, signature);
            return signature;
        }

        private string GenerateHMACSignature(string data, string key)
        {
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(key));
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
            var signature = BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
            
            _logger.LogDebug("PayOS HMAC-SHA256 Signature: Data={Data}, Key={Key}, Signature={Signature}", data, key, signature);
            return signature;
        }

        private string GenerateSHA1Signature(string data)
        {
            using var sha1 = SHA1.Create();
            var hash = sha1.ComputeHash(Encoding.UTF8.GetBytes(data));
            var signature = BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
            
            _logger.LogDebug("PayOS SHA1 Signature: Data={Data}, Signature={Signature}", data, signature);
            return signature;
        }

        // Xác thực webhook từ PayOS
        public bool VerifyWebhook(string rawBody, string signatureHeader)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(signatureHeader))
                {
                    _logger.LogWarning("PayOS Webhook: Empty signature header");
                    return false;
                }

                _logger.LogInformation("PayOS Webhook: Verifying webhook");
                _logger.LogInformation("  Raw Body: {RawBody}", rawBody);
                _logger.LogInformation("  Received Signature: {ReceivedSignature}", signatureHeader);

                // Verify với checksumKey
                using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_checksumKey));
                var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(rawBody));
                var computed = BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                var received = signatureHeader.Trim().ToLowerInvariant();

                var isValid = string.Equals(computed, received, StringComparison.Ordinal);

                _logger.LogInformation("PayOS Webhook Verification:");
                _logger.LogInformation("  Computed Signature: {ComputedSignature}", computed);
                _logger.LogInformation("  Received Signature: {ReceivedSignature}", received);
                _logger.LogInformation("  Is Valid: {IsValid}", isValid);

                return isValid;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "PayOS VerifyWebhook Error: {Message}", ex.Message);
                return false;
            }
        }
    }
}
