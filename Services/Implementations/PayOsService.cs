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

            // Cấu hình HTTP client với SSL
            _http.Timeout = TimeSpan.FromSeconds(30);
            _http.DefaultRequestHeaders.Add("User-Agent", "SoulFashion-PayOS-Integration/1.0");
            
            // Thêm SSL configuration
            var handler = new HttpClientHandler();
            handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true;
            _http = new HttpClient(handler);
            _http.Timeout = TimeSpan.FromSeconds(30);
            _http.DefaultRequestHeaders.Add("User-Agent", "SoulFashion-PayOS-Integration/1.0");

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

                // ✅ Test kết nối trước
                try
                {
                    Console.WriteLine("PayOS: Testing connection to PayOS...");
                    var testResponse = await _http.GetAsync("https://api-merchant.payos.vn");
                    Console.WriteLine($"PayOS: Test connection status: {testResponse.StatusCode}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"PayOS: Test connection failed: {ex.Message}");
                }

                // ✅ Thử các URL khác nhau của PayOS
                var urls = new[]
                {
                    "https://api-merchant.payos.vn/v2/payment-requests", // URL chính thức
                    "https://api.payos.vn/v2/payment-requests", // URL backup
                    "https://api-merchant.payos.vn/v2/payment", // URL khác
                    "https://api.payos.vn/v2/payment", // URL khác
                    "https://api-merchant.payos.vn/v2/payment/create", // URL có thể đúng
                    "https://api.payos.vn/v2/payment/create", // URL có thể đúng
                    "https://api-merchant.payos.vn/v2/payment-request", // URL khác
                    "https://api.payos.vn/v2/payment-request", // URL khác
                    "https://api-merchant.payos.vn/v2/payment-requests/create", // URL khác
                    "https://api.payos.vn/v2/payment-requests/create", // URL khác
                    "https://api-merchant.payos.vn/v2/payment/create-payment", // URL khác
                    "https://api.payos.vn/v2/payment/create-payment" // URL khác
                };

                // ✅ Thử các format payload khác nhau
                var payloads = new object[]
                {
                    // Format 0: Đơn giản nhất - chỉ có orderCode và amount
                    new
                    {
                        orderCode = order.OrderId.ToString(),
                        amount = order.TotalPrice.Value
                    },
                    // Format 1: Không có signature (chỉ dùng headers)
                    new
                    {
                        orderCode = order.OrderId.ToString(),
                        amount = order.TotalPrice.Value,
                        description = $"Thanh toán đơn hàng #{order.OrderId} - SoulFashion",
                        cancelUrl = _cancelUrl,
                        returnUrl = _returnUrl
                    },
                    // Format 2: Với signature MD5
                    new
                    {
                        orderCode = order.OrderId.ToString(),
                        amount = order.TotalPrice.Value,
                        description = $"Thanh toán đơn hàng #{order.OrderId} - SoulFashion",
                        cancelUrl = _cancelUrl,
                        returnUrl = _returnUrl,
                        signature = GenerateMD5Signature(order.OrderId.ToString(), order.TotalPrice.Value)
                    },
                    // Format 3: Với signature HMAC-SHA256
                    new
                    {
                        orderCode = order.OrderId.ToString(),
                        amount = order.TotalPrice.Value,
                        description = $"Thanh toán đơn hàng #{order.OrderId} - SoulFashion",
                        cancelUrl = _cancelUrl,
                        returnUrl = _returnUrl,
                        signature = GenerateHMACSignature(order.OrderId.ToString(), order.TotalPrice.Value)
                    },
                    // Format 4: snake_case
                    new
                    {
                        order_code = order.OrderId.ToString(),
                        amount = order.TotalPrice.Value,
                        description = $"Thanh toán đơn hàng #{order.OrderId} - SoulFashion",
                        cancel_url = _cancelUrl,
                        return_url = _returnUrl
                    }
                };

                // Thử từng URL và từng format payload
                foreach (var url in urls)
                {
                    Console.WriteLine($"PayOS: Trying URL: {url}");
                    
                    for (int i = 0; i < payloads.Length; i++)
                    {
                        try
                        {
                            Console.WriteLine($"PayOS: Trying payload format {i + 1} with URL: {url}");
                            var payload = payloads[i];
                            
                            var req = new HttpRequestMessage(HttpMethod.Post, url)
                            {
                                Content = JsonContent.Create(payload)
                            };

                            req.Headers.Add("x-client-id", _clientId);
                            req.Headers.Add("x-api-key", _apiKey);

                            Console.WriteLine($"PayOS Request {i + 1}:");
                            Console.WriteLine($"  URL: {url}");
                            Console.WriteLine($"  Headers: x-client-id={_clientId}, x-api-key={_apiKey}");
                            Console.WriteLine($"  Payload: {JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true })}");

                            var res = await _http.SendAsync(req);
                            var raw = await res.Content.ReadAsStringAsync();

                            Console.WriteLine($"PayOS Response {i + 1}:");
                            Console.WriteLine($"  Status: {res.StatusCode}");
                            Console.WriteLine($"  Content-Type: {res.Content.Headers.ContentType}");
                            Console.WriteLine($"  Body Length: {raw?.Length ?? 0}");
                            Console.WriteLine($"  Body Preview: {(raw?.Length > 200 ? raw.Substring(0, 200) + "..." : raw)}");

                            // Kiểm tra nếu response là HTML
                            if (raw?.Contains("<!doctype html>") == true || raw?.Contains("<html") == true)
                            {
                                Console.WriteLine($"PayOS: HTML response detected - URL {url} might be wrong");
                                continue; // Thử URL khác
                            }

                            // Kiểm tra nếu response là error
                            if (raw?.Contains("error") == true || raw?.Contains("Error") == true)
                            {
                                Console.WriteLine($"PayOS: Error response detected from URL {url}: {raw}");
                                continue; // Thử URL khác
                            }

                            if (res.IsSuccessStatusCode)
                            {
                                try
                                {
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

                                                Console.WriteLine($"PayOS: SUCCESS with URL {url} and payload format {i + 1}!");
                                                return (checkoutUrl, qrCode, raw);
                                            }
                                        }
                                        else
                                        {
                                            var desc = root.TryGetProperty("desc", out var descEl) ? descEl.GetString() : "Unknown error";
                                            Console.WriteLine($"PayOS: Business error with URL {url} and format {i + 1}: {desc} (Code: {code})");
                                            
                                            // Nếu là lỗi signature, tiếp tục thử format khác
                                            if (desc.Contains("signature") || desc.Contains("kiểm tra"))
                                                continue;
                                        }
                                    }
                                    else
                                    {
                                        Console.WriteLine($"PayOS: Response missing 'code' field from URL {url}: {raw}");
                                    }
                                }
                                catch (JsonException ex)
                                {
                                    Console.WriteLine($"PayOS: Invalid JSON response from URL {url}: {ex.Message}");
                                    Console.WriteLine($"PayOS: Raw response: {raw}");
                                    continue; // Thử URL khác
                                }
                            }
                            else
                            {
                                Console.WriteLine($"PayOS: HTTP error {res.StatusCode} from URL {url}: {raw}");
                            }
                        }
                        catch (HttpRequestException ex)
                        {
                            Console.WriteLine($"PayOS: HTTP request error with URL {url} and format {i + 1}: {ex.Message}");
                            Console.WriteLine($"PayOS: Inner exception: {ex.InnerException?.Message}");
                            continue;
                        }
                        catch (TaskCanceledException ex)
                        {
                            Console.WriteLine($"PayOS: Request timeout with URL {url} and format {i + 1}: {ex.Message}");
                            continue;
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"PayOS: Error with URL {url} and payload format {i + 1}: {ex.Message}");
                            Console.WriteLine($"PayOS: Stack trace: {ex.StackTrace}");
                            continue;
                        }
                    }
                }

                Console.WriteLine("PayOS: All URLs and payload formats failed. Summary:");
                Console.WriteLine($"  - Tried {urls.Length} different URLs");
                Console.WriteLine($"  - Tried {payloads.Length} different payload formats");
                Console.WriteLine($"  - Total attempts: {urls.Length * payloads.Length}");
                Console.WriteLine("  - Check console logs above for detailed error information");
                
                throw new Exception("All URLs and payload formats failed. Check console logs for details.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"PayOS CreatePaymentLinkAsync Error: {ex.Message}");
                throw;
            }
        }

        private string GenerateMD5Signature(string orderCode, decimal amount)
        {
            try
            {
                // Format: orderCode + amount + checksumKey
                var data = $"{orderCode}{amount}{_checksumKey}";
                using var md5 = MD5.Create();
                var hash = md5.ComputeHash(Encoding.UTF8.GetBytes(data));
                var signature = BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                
                Console.WriteLine($"PayOS MD5 Signature:");
                Console.WriteLine($"  Data: {data}");
                Console.WriteLine($"  Signature: {signature}");
                
                return signature;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"PayOS GenerateMD5Signature Error: {ex.Message}");
                throw;
            }
        }

        private string GenerateHMACSignature(string orderCode, decimal amount)
        {
            try
            {
                // Format: orderCode + amount (không có checksumKey trong data)
                var data = $"{orderCode}{amount}";
                using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_checksumKey));
                var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
                var signature = BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                
                Console.WriteLine($"PayOS HMAC-SHA256 Signature:");
                Console.WriteLine($"  Data: {data}");
                Console.WriteLine($"  ChecksumKey: {_checksumKey}");
                Console.WriteLine($"  Signature: {signature}");
                
                return signature;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"PayOS GenerateHMACSignature Error: {ex.Message}");
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
