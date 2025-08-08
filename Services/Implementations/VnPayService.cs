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
                ["vnp_SecureHashType"] = "HMACSHA512" // gửi kèm, KHÔNG ký
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

        private string BuildSignedQuery(IDictionary<string, string?> rawParams, string secret)
        {
            // Bỏ key/value rỗng
            var cleaned = rawParams
                .Where(kv => !string.IsNullOrEmpty(kv.Key) && !string.IsNullOrEmpty(kv.Value))
                .ToDictionary(kv => kv.Key!, kv => kv.Value!);

            // Ký trên form-encode chuẩn, KHÔNG gồm vnp_SecureHash / vnp_SecureHashType
            var signData = BuildDataToSign(
                cleaned.Where(kv => kv.Key != "vnp_SecureHash" && kv.Key != "vnp_SecureHashType")
                       .ToDictionary(kv => kv.Key, kv => kv.Value));

            var secureHash = ComputeHmacSha512((_config["VnPay:HashSecret"] ?? string.Empty).Trim(), signData);

            _logger.LogInformation("[VNPay SEND] signData={signData}", signData);
            _logger.LogInformation("[VNPay SEND] secureHash={secureHash}", secureHash);

            // Build query gửi đi (form-encode chuẩn: space -> '+', HEX CHỮ HOA)
            var sorted = new SortedDictionary<string, string>(cleaned, StringComparer.Ordinal);
            var query = string.Join("&", sorted.Select(kv => $"{kv.Key}={FormEncode(kv.Value)}"));
            return $"{query}&vnp_SecureHash={secureHash}";
        }

        /// Tạo chuỗi ký: key=UrlEncode(value)&... (sort Ordinal)
        private static string BuildDataToSign(IDictionary<string, string> parameters)
        {
            var sorted = new SortedDictionary<string, string>(parameters, StringComparer.Ordinal);
            return string.Join("&", sorted.Select(kv => $"{kv.Key}={FormEncode(kv.Value)}"));
        }

        /// Form-encode chuẩn .NET: space -> '+', HEX CHỮ HOA (KHÔNG ép lowercase)
        private static string FormEncode(string value) =>
            HttpUtility.UrlEncode(value ?? string.Empty, Encoding.UTF8);

        private static string ComputeHmacSha512(string key, string data)
        {
            using var hmac = new HMACSHA512(Encoding.UTF8.GetBytes(key ?? string.Empty));
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data ?? string.Empty));
            return BitConverter.ToString(hash).Replace("-", "").ToUpperInvariant();
        }
    }
}
