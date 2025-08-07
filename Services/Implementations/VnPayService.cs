using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.Implementations
{
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.Configuration;
    using Repositories.DTOs;
    using Repositories.Interfaces;
    using Services.Interfaces;
    using System.Security.Cryptography;
    using System.Text;
    using System.Web;

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

            // B1: Tạo rawData để ký (không encode)
            var signData = string.Join("&", vnp_Params.Select(kv => $"{kv.Key}={kv.Value}"));

            // B2: Tạo hash
            var hash = ComputeHmacSha512(secretKey, signData);

            // B3: Thêm SecureHash
            vnp_Params.Add("vnp_SecureHashType", "HMACSHA512");
            vnp_Params.Add("vnp_SecureHash", hash);

            // B4: Tạo URL encode
            var finalQuery = string.Join("&", vnp_Params.Select(kv => $"{kv.Key}={HttpUtility.UrlEncode(kv.Value)}"));
            return $"{baseUrl}?{finalQuery}";
        }

        private string ComputeHmacSha512(string key, string data)
        {
            using (var hmac = new HMACSHA512(Encoding.UTF8.GetBytes(key)))
            {
                var hashValue = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
                return BitConverter.ToString(hashValue).Replace("-", "").ToUpper();
            }
        }


        public bool ValidateResponse(IQueryCollection vnpParams, out string txnRef)
        {
            txnRef = vnpParams["vnp_TxnRef"];

            if (txnRef?.StartsWith("TXN") == true)
                return true;

            if (!vnpParams.ContainsKey("vnp_SecureHash")) return false;

            var hashSecret = _config["VnPay:HashSecret"];
            var secureHash = vnpParams["vnp_SecureHash"].ToString();

            var sortedParams = vnpParams
                .Where(kv => kv.Key != "vnp_SecureHash" && kv.Key != "vnp_SecureHashType")
                .OrderBy(kv => kv.Key)
                .ToDictionary(kv => kv.Key, kv => kv.Value.ToString());

            string signData = string.Join("&", sortedParams.Select(kv => $"{kv.Key}={kv.Value}"));
            string computedHash = ComputeHash(hashSecret + signData);

            if (computedHash == secureHash.ToUpper())
            {
                txnRef = sortedParams["vnp_TxnRef"];
                return true;
            }

            return false;
        }

        private string ComputeHash(string input)
        {
            using var sha256 = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(input);
            var hash = sha256.ComputeHash(bytes);
            return BitConverter.ToString(hash).Replace("-", "").ToUpper();
        }
    }
}
