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
            var secretKey = _config["VnPay:HashSecret"];
            var baseUrl = _config["VnPay:BaseUrl"];
            var returnUrl = _config["VnPay:ReturnUrl"];

            // Các tham số gửi sang VNPAY
            var vnp_Params = new SortedDictionary<string, string>
            {
                { "vnp_Version", "2.1.0" },
                { "vnp_Command", "pay" },
                { "vnp_TmnCode", tmnCode },
                { "vnp_Amount", ((int)(order.TotalPrice.Value * 100)).ToString() },
                { "vnp_CreateDate", DateTime.Now.ToString("yyyyMMddHHmmss") },
                { "vnp_CurrCode", "VND" },
                { "vnp_IpAddr", ipAddress },
                { "vnp_Locale", "vn" },
                { "vnp_OrderInfo", $"Thanh toán đơn hàng #{order.OrderId}" },
                { "vnp_OrderType", "other" },
                { "vnp_ReturnUrl", returnUrl },
                { "vnp_TxnRef", txnRef }
            };

            // 1. Build chuỗi signData (không URL encode, đã sort sẵn do SortedDictionary)
            var signData = string.Join("&", vnp_Params.Select(kv => $"{kv.Key}={kv.Value}"));

            // 2. Tạo chữ ký bằng HMACSHA512
            var secureHash = ComputeHmacSha512(secretKey, signData);

            // 3. Thêm SecureHash vào params
            vnp_Params.Add("vnp_SecureHashType", "HMACSHA512");
            vnp_Params.Add("vnp_SecureHash", secureHash);

            // 4. Build query string (có URL encode khi tạo URL cuối)
            var queryString = string.Join("&", vnp_Params.Select(kv => $"{kv.Key}={HttpUtility.UrlEncode(kv.Value)}"));

            return $"{baseUrl}?{queryString}";
        }

        public bool ValidateResponse(IQueryCollection vnpParams, out string txnRef)
        {
            txnRef = vnpParams["vnp_TxnRef"];

            if (!vnpParams.ContainsKey("vnp_SecureHash")) return false;

            var hashSecret = _config["VnPay:HashSecret"];
            var vnp_SecureHash = vnpParams["vnp_SecureHash"].ToString();

            // Lọc bỏ SecureHash & SecureHashType để ký lại
            var sortedParams = vnpParams
                .Where(kv => kv.Key != "vnp_SecureHash" && kv.Key != "vnp_SecureHashType")
                .OrderBy(kv => kv.Key)
                .ToDictionary(kv => kv.Key, kv => kv.Value.ToString());

            var signData = string.Join("&", sortedParams.Select(kv => $"{kv.Key}={kv.Value}"));

            var computedHash = ComputeHmacSha512(hashSecret, signData);

            return computedHash.Equals(vnp_SecureHash, StringComparison.InvariantCultureIgnoreCase);
        }

        private string ComputeHmacSha512(string key, string data)
        {
            using var hmac = new HMACSHA512(Encoding.UTF8.GetBytes(key));
            var hashValue = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
            return BitConverter.ToString(hashValue).Replace("-", "").ToUpper();
        }
    }
}
