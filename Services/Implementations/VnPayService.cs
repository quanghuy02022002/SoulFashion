using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Repositories.DTOs;
using Repositories.Interfaces;
using Services.Interfaces;

namespace Services.Implementations
{
    public class VnPayService : IVnPayService
    {
        private readonly IConfiguration _config;
        private readonly IOrderRepository _orderRepo;

        public VnPayService(IConfiguration config, IOrderRepository orderRepo)
        {
            _config = config;
            _orderRepo = orderRepo;
        }

        public string CreatePaymentUrl(PaymentDto dto, string ipAddress, string txnRef)
        {
            var order = _orderRepo.GetByIdAsync(dto.OrderId).Result;
            if (order == null || order.TotalPrice == null)
                throw new Exception("Không tìm thấy đơn hàng hoặc đơn hàng không có tổng giá.");

            var tmnCode = _config["VnPay:TmnCode"];
            var secret = _config["VnPay:HashSecret"];
            var baseUrl = _config["VnPay:BaseUrl"];
            var returnUrl = _config["VnPay:ReturnUrl"];

            // Giờ VN (UTC+7) + hết hạn 15'
            var now = DateTime.UtcNow.AddHours(7);
            var expire = now.AddMinutes(15);

            var p = new Dictionary<string, string>
            {
                ["vnp_Version"] = "2.1.0",
                ["vnp_Command"] = "pay",
                ["vnp_TmnCode"] = tmnCode,
                ["vnp_Amount"] = ((long)(order.TotalPrice.Value * 100)).ToString(),
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
            };

            var signedQuery = BuildSignedQuery(p, secret);
            return $"{baseUrl}?{signedQuery}";
        }

        public bool ValidateResponse(IQueryCollection vnpParams, out string txnRef)
        {
            txnRef = vnpParams["vnp_TxnRef"];

            if (!vnpParams.ContainsKey("vnp_SecureHash"))
                return false;

            var secret = _config["VnPay:HashSecret"];
            var secureHashFromVnp = vnpParams["vnp_SecureHash"].ToString();

            // Build lại signData y hệt cách gửi đi (encode + hex thường)
            var data = vnpParams
                .Where(kv => kv.Key != "vnp_SecureHash" && kv.Key != "vnp_SecureHashType")
                .ToDictionary(kv => kv.Key, kv => kv.Value.ToString());

            var signData = BuildDataToSign(data);
            var computed = ComputeHmacSha512(secret, signData);

            // Log để so sánh nhanh khi cần
            Console.WriteLine("[VNPay RETURN] signData=" + signData);
            Console.WriteLine("[VNPay RETURN] computed=" + computed);
            Console.WriteLine("[VNPay RETURN] fromVNPay=" + secureHashFromVnp);

            return computed.Equals(secureHashFromVnp, StringComparison.InvariantCultureIgnoreCase);
        }

        // ===================== Helpers =====================

        private static string BuildSignedQuery(IDictionary<string, string> rawParams, string secret)
        {
            var sorted = new SortedDictionary<string, string>(
                rawParams.Where(kv => !string.IsNullOrEmpty(kv.Value))
                         .ToDictionary(kv => kv.Key, kv => kv.Value),
                StringComparer.Ordinal);

            var signData = BuildDataToSign(sorted);          // encode + hex thường
            var secureHash = ComputeHmacSha512(secret, signData);

            Console.WriteLine("[VNPay SEND] signData=" + signData);
            Console.WriteLine("[VNPay SEND] secureHash=" + secureHash);

            var encodedPairs = sorted.Select(kv =>
                $"{kv.Key}={UrlEncodeVnPay(kv.Value)}");

            var query = string.Join("&", encodedPairs);
            query += $"&vnp_SecureHash={secureHash}";
            return query;
        }

        /// Build chuỗi ký: key=UrlEncode(value)&... (bỏ vnp_SecureHash/Type)
        private static string BuildDataToSign(IDictionary<string, string> parameters)
        {
            var sorted = new SortedDictionary<string, string>(
                parameters.Where(kv => kv.Key != "vnp_SecureHash" && kv.Key != "vnp_SecureHashType")
                          .Where(kv => !string.IsNullOrEmpty(kv.Value))
                          .ToDictionary(kv => kv.Key, kv => kv.Value),
                StringComparer.Ordinal);

            var encodedPairs = sorted.Select(kv =>
                $"{kv.Key}={UrlEncodeVnPay(kv.Value)}");

            return string.Join("&", encodedPairs);
        }

        // Encode theo application/x-www-form-urlencoded (space -> '+')
        // và ép hex về chữ thường: %C3%A1 -> %c3%a1 (khớp sandbox VNPay)
        private static string UrlEncodeVnPay(string value)
        {
            var encoded = HttpUtility.UrlEncode(value ?? string.Empty, Encoding.UTF8); // space -> '+', hex HOA
            var sb = new StringBuilder(encoded.Length);
            for (int i = 0; i < encoded.Length; i++)
            {
                char c = encoded[i];
                if (c == '%' && i + 2 < encoded.Length)
                {
                    sb.Append('%');
                    sb.Append(char.ToLowerInvariant(encoded[i + 1]));
                    sb.Append(char.ToLowerInvariant(encoded[i + 2]));
                    i += 2;
                }
                else
                {
                    sb.Append(c);
                }
            }
            return sb.ToString();
        }

        private static string ComputeHmacSha512(string key, string data)
        {
            using var hmac = new HMACSHA512(Encoding.UTF8.GetBytes(key ?? ""));
            var hashValue = hmac.ComputeHash(Encoding.UTF8.GetBytes(data ?? ""));
            return BitConverter.ToString(hashValue).Replace("-", "").ToUpperInvariant();
        }
    }
}
