using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Repositories.DTOs;
using Repositories.Interfaces;
using Services.Interfaces;

namespace Services.Implementations
{
    public class VnPayService : IVnPayService
    {
        private readonly IConfiguration _config;
        private readonly IOrderRepository _orderRepo;
        private readonly ILogger<VnPayService> _logger;

        public VnPayService(IConfiguration config, IOrderRepository orderRepo, ILogger<VnPayService> logger)
        {
            _config = config;
            _orderRepo = orderRepo;
            _logger = logger;
        }

        public string CreatePaymentUrl(PaymentDto dto, string ipAddress, string txnRef)
        {
            var order = _orderRepo.GetByIdAsync(dto.OrderId).Result;
            if (order is null || order.TotalPrice is null)
                throw new Exception("Không tìm thấy đơn hàng hoặc tổng giá chưa có.");

            var tmnCode = _config["VnPay:TmnCode"];
            var secret = (_config["VnPay:HashSecret"] ?? string.Empty).Trim();
            var baseUrl = _config["VnPay:BaseUrl"];
            var returnUrl = _config["VnPay:ReturnUrl"];

            var now = DateTime.UtcNow.AddHours(7);
            var expire = now.AddMinutes(15);

            var p = new Dictionary<string, string?>
            {
                ["vnp_Version"] = "2.1.0",
                ["vnp_Command"] = "pay",
                ["vnp_TmnCode"] = tmnCode,
                ["vnp_Amount"] = ((long)(order.TotalPrice.Value * 100)).ToString(), // VND x100
                ["vnp_CreateDate"] = now.ToString("yyyyMMddHHmmss"),
                ["vnp_ExpireDate"] = expire.ToString("yyyyMMddHHmmss"),
                ["vnp_CurrCode"] = "VND",
                ["vnp_IpAddr"] = string.IsNullOrWhiteSpace(ipAddress) ? "127.0.0.1" : ipAddress,
                ["vnp_Locale"] = "vn",
                ["vnp_OrderInfo"] = $"Thanh toán đơn hàng #{order.OrderId}",
                ["vnp_OrderType"] = "other",
                ["vnp_ReturnUrl"] = returnUrl,
                ["vnp_TxnRef"] = txnRef,
                ["vnp_SecureHashType"] = "HMACSHA512" // Gửi kèm, KHÔNG ký
                // ["vnp_BankCode"]  = "VNPAYQR" // nếu cần thì thêm khi có giá trị
            };

            var signedQuery = BuildSignedQuery(p, secret);
            return $"{baseUrl}?{signedQuery}";
        }

        public bool ValidateResponse(IQueryCollection vnpParams, out string txnRef)
        {
            txnRef = vnpParams["vnp_TxnRef"];

            if (!vnpParams.ContainsKey("vnp_SecureHash"))
                return false;

            var secret = (_config["VnPay:HashSecret"] ?? string.Empty).Trim();
            var fromVnp = vnpParams["vnp_SecureHash"].ToString();

            // Lấy đúng tập vnp_*, bỏ rỗng, bỏ vnp_SecureHash/Type khỏi chuỗi ký
            var data = vnpParams
                .Where(kv => kv.Key.StartsWith("vnp_", StringComparison.Ordinal))
                .Where(kv => kv.Key != "vnp_SecureHash" && kv.Key != "vnp_SecureHashType")
                .Where(kv => !string.IsNullOrEmpty(kv.Value))
                .ToDictionary(kv => kv.Key, kv => kv.Value.ToString());

            var signData = BuildDataToSign(data);
            var computed = ComputeHmacSha512(secret, signData);

            _logger.LogInformation("[VNPay RETURN] signData={signData}", signData);
            _logger.LogInformation("[VNPay RETURN] computed={computed}", computed);
            _logger.LogInformation("[VNPay RETURN] fromVNPay={fromVNPay}", fromVnp);

            return computed.Equals(fromVnp, StringComparison.InvariantCultureIgnoreCase);
        }

        // ================= Helpers =================

        // Encode form chuẩn rồi ÉP HEX sau '%' về CHỮ HOA (space -> '+')
        private static string FormEncodeUpper(string? value)
        {
            var encoded = HttpUtility.UrlEncode(value ?? string.Empty, Encoding.UTF8) ?? string.Empty;
            var sb = new StringBuilder(encoded.Length);
            for (int i = 0; i < encoded.Length; i++)
            {
                char c = encoded[i];
                if (c == '%' && i + 2 < encoded.Length)
                {
                    sb.Append('%');
                    sb.Append(char.ToUpperInvariant(encoded[i + 1]));
                    sb.Append(char.ToUpperInvariant(encoded[i + 2]));
                    i += 2;
                }
                else
                {
                    sb.Append(c);
                }
            }
            return sb.ToString();
        }

        /// Tạo chuỗi ký: sort Ordinal, KHÔNG ký vnp_SecureHash/Type, dùng FormEncodeUpper
        private static string BuildDataToSign(IDictionary<string, string> parameters)
        {
            var sorted = new SortedDictionary<string, string>(
                parameters
                    .Where(kv => kv.Key != "vnp_SecureHash" && kv.Key != "vnp_SecureHashType")
                    .Where(kv => !string.IsNullOrEmpty(kv.Value))
                    .ToDictionary(kv => kv.Key, kv => kv.Value),
                StringComparer.Ordinal);

            return string.Join("&", sorted.Select(kv => $"{kv.Key}={FormEncodeUpper(kv.Value)}"));
        }

        // Ghép query gửi đi: dùng CÙNG encoder với chuỗi ký
        private string BuildSignedQuery(IDictionary<string, string?> rawParams, string secret)
        {
            // Bỏ key/value rỗng
            var cleaned = rawParams
                .Where(kv => !string.IsNullOrEmpty(kv.Key) && !string.IsNullOrEmpty(kv.Value))
                .ToDictionary(kv => kv.Key!, kv => kv.Value!);

            // Ký trên form-encode chuẩn (HEX CHỮ HOA), KHÔNG gồm vnp_SecureHash / vnp_SecureHashType
            var signData = BuildDataToSign(cleaned);
            var secureHash = ComputeHmacSha512((_config["VnPay:HashSecret"] ?? string.Empty).Trim(), signData);

            _logger.LogInformation("[VNPay SEND] signData={signData}", signData);
            _logger.LogInformation("[VNPay SEND] secureHash={secureHash}", secureHash);

            // Build query gửi đi (dùng cùng encoder)
            var sorted = new SortedDictionary<string, string>(cleaned, StringComparer.Ordinal);
            var query = string.Join("&", sorted.Select(kv => $"{kv.Key}={FormEncodeUpper(kv.Value)}"));
            return $"{query}&vnp_SecureHash={secureHash}";
        }

        private static string ComputeHmacSha512(string key, string data)
        {
            using var hmac = new HMACSHA512(Encoding.UTF8.GetBytes(key ?? string.Empty));
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data ?? string.Empty));
            return BitConverter.ToString(hash).Replace("-", "").ToUpperInvariant();
        }
    }
}
