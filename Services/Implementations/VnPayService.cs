using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Repositories.DTOs;
using Repositories.Interfaces;
using Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

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

            // Luôn dùng giờ VN (UTC+7) để tránh lệch
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
                ["vnp_SecureHashType"] = "HMACSHA512" // gửi kèm nhưng KHÔNG ký
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

            // Lấy tất cả param trừ hash & type, encode RFC3986 rồi ký lại
            var data = vnpParams
                .Where(kv => kv.Key != "vnp_SecureHash" && kv.Key != "vnp_SecureHashType")
                .ToDictionary(kv => kv.Key, kv => kv.Value.ToString());

            var signData = BuildDataToSign(data);
            var computed = ComputeHmacSha512(secret, signData);

            return computed.Equals(secureHashFromVnp, StringComparison.InvariantCultureIgnoreCase);
        }

        // ===================== Helpers =====================

        private static string BuildSignedQuery(IDictionary<string, string> rawParams, string secret)
        {
            var sorted = new SortedDictionary<string, string>(
                rawParams.Where(kv => !string.IsNullOrEmpty(kv.Value))
                         .ToDictionary(kv => kv.Key, kv => kv.Value),
                StringComparer.Ordinal);

            var signData = BuildDataToSign(sorted);
            var secureHash = ComputeHmacSha512(secret, signData);

            var encodedPairs = sorted.Select(kv =>
                $"{kv.Key}={UrlEncodeRfc3986(kv.Value)}");

            var query = string.Join("&", encodedPairs);
            query += $"&vnp_SecureHash={secureHash}";
            return query;
        }

        private static string BuildDataToSign(IDictionary<string, string> parameters)
        {
            var sorted = new SortedDictionary<string, string>(
                parameters.Where(kv => kv.Key != "vnp_SecureHash" && kv.Key != "vnp_SecureHashType")
                          .Where(kv => !string.IsNullOrEmpty(kv.Value))
                          .ToDictionary(kv => kv.Key, kv => kv.Value),
                StringComparer.Ordinal);

            var encodedPairs = sorted.Select(kv =>
                $"{kv.Key}={UrlEncodeRfc3986(kv.Value)}");

            return string.Join("&", encodedPairs);
        }

        private static string UrlEncodeRfc3986(string value)
        {
            // RFC3986: space -> %20 (không phải '+'); giữ encode thống nhất 2 chiều
            return Uri.EscapeDataString(value ?? string.Empty);
        }

        private static string ComputeHmacSha512(string key, string data)
        {
            using var hmac = new HMACSHA512(Encoding.UTF8.GetBytes(key ?? ""));
            var hashValue = hmac.ComputeHash(Encoding.UTF8.GetBytes(data ?? ""));
            return BitConverter.ToString(hashValue).Replace("-", "").ToUpperInvariant();
        }
    }
}
