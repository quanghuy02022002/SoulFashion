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
using System.Web;

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

            // Thời gian hiện tại & hết hạn (khuyến nghị: +15 phút)
            var now = DateTime.UtcNow.AddHours(7);
            var expire = now.AddMinutes(15);

            // Tham số theo spec VNPAY
            var p = new Dictionary<string, string>
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
                ["vnp_TxnRef"] = txnRef
                // ["vnp_BankCode"] = "VNPAYQR" // nếu muốn cố định, còn không thì bỏ
            };

            // Tạo query đã ký
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

            // Lấy tất cả params (decode sẵn) -> encode lại & ký như lúc gửi
            var data = vnpParams
                .Where(kv => kv.Key != "vnp_SecureHash" && kv.Key != "vnp_SecureHashType")
                .ToDictionary(kv => kv.Key, kv => kv.Value.ToString());

            var signData = BuildDataToSign(data);
            var computed = ComputeHmacSha512(secret, signData);

            return computed.Equals(secureHashFromVnp, StringComparison.InvariantCultureIgnoreCase);
        }

        // ===================== Helpers =====================

        /// <summary>
        /// Xây dựng chuỗi query (đã URL-encode) và append vnp_SecureHash.
        /// </summary>
        private static string BuildSignedQuery(IDictionary<string, string> rawParams, string secret)
        {
            // Sắp xếp theo key tăng dần
            var sorted = new SortedDictionary<string, string>(
                rawParams.Where(kv => !string.IsNullOrEmpty(kv.Value))
                         .ToDictionary(kv => kv.Key, kv => kv.Value),
                StringComparer.Ordinal);

            // Chuỗi dữ liệu để ký (encode value UTF-8)
            var signData = BuildDataToSign(sorted);

            // Hash HMAC SHA512
            var secureHash = ComputeHmacSha512(secret, signData);

            // Ghép query (đã encode) + secure hash
            var encodedPairs = sorted.Select(kv =>
                $"{kv.Key}={HttpUtility.UrlEncode(kv.Value, Encoding.UTF8)}");

            var query = string.Join("&", encodedPairs);
            query += $"&vnp_SecureHash={secureHash}";

            return query;
        }

        /// <summary>
        /// Tạo chuỗi data để ký: key=UrlEncode(value)&... (bỏ vnp_SecureHash/Type nếu có).
        /// </summary>
        private static string BuildDataToSign(IDictionary<string, string> parameters)
        {
            var sorted = new SortedDictionary<string, string>(
                parameters.Where(kv => kv.Key != "vnp_SecureHash" && kv.Key != "vnp_SecureHashType")
                          .Where(kv => !string.IsNullOrEmpty(kv.Value))
                          .ToDictionary(kv => kv.Key, kv => kv.Value),
                StringComparer.Ordinal);

            var encodedPairs = sorted.Select(kv =>
                $"{kv.Key}={HttpUtility.UrlEncode(kv.Value, Encoding.UTF8)}");

            return string.Join("&", encodedPairs);
        }

        private static string ComputeHmacSha512(string key, string data)
        {
            using var hmac = new HMACSHA512(Encoding.UTF8.GetBytes(key ?? ""));
            var hashValue = hmac.ComputeHash(Encoding.UTF8.GetBytes(data ?? ""));
            return BitConverter.ToString(hashValue).Replace("-", "").ToUpperInvariant();
        }
    }
}
