using bookify_data.Enums;
using bookify_data.Helper;
using bookify_data.Model;
using bookify_service.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Web;

namespace bookify_service.Services
{
    public class VnpayService : IVnpayService
    {
        private string _tmnCode;
        private string _hashSecret;
        private string _callbackUrl;
        private string _baseUrl;
        private string _version;
        private string _orderType;
        private readonly IOrderService _orderService;
        private readonly ILogger<VnpayService> _logger;

        public VnpayService(IOrderService orderService, ILogger<VnpayService> logger)
        {
            _orderService = orderService;
            _logger = logger;
        }

        public void Initialize(string tmnCode, string hashSecret, string baseUrl, string callbackUrl,
            string version = "2.1.0", string orderType = "other")
        {
            _tmnCode = tmnCode;
            _hashSecret = hashSecret;
            _callbackUrl = callbackUrl;
            _baseUrl = baseUrl;
            _version = version;
            _orderType = orderType;
            EnsureParametersBeforePayment();
        }

        private static string FormEncode(string value) =>
            HttpUtility.UrlEncode(value ?? string.Empty, Encoding.UTF8);

        private static string BuildDataToSign(IDictionary<string, string> parameters)
        {
            var sorted = new SortedDictionary<string, string>(
                parameters
                    .Where(kv => kv.Key != "vnp_SecureHash" && kv.Key != "vnp_SecureHashType")
                    .Where(kv => !string.IsNullOrEmpty(kv.Value))
                    .ToDictionary(kv => kv.Key, kv => kv.Value),
                StringComparer.Ordinal
            );

            return string.Join("&", sorted.Select(kv => $"{kv.Key}={FormEncode(kv.Value)}"));
        }

        private string BuildSignedQuery(IDictionary<string, string> rawParams)
        {
            var cleaned = rawParams
                .Where(kv => !string.IsNullOrEmpty(kv.Key) && !string.IsNullOrEmpty(kv.Value))
                .ToDictionary(kv => kv.Key, kv => kv.Value);

            var signData = BuildDataToSign(cleaned);
            var secureHash = ComputeHmacSha512(_hashSecret.Trim(), signData);

            _logger.LogInformation("[VNPay SEND] signData={signData}", signData);
            _logger.LogInformation("[VNPay SEND] secureHash={secureHash}", secureHash);

            var sorted = new SortedDictionary<string, string>(cleaned, StringComparer.Ordinal);
            var query = string.Join("&", sorted.Select(kv => $"{kv.Key}={FormEncode(kv.Value)}"));
            return $"{query}&vnp_SecureHash={secureHash}";
        }

        public string GetPaymentUrl(VnpayPaymentRequest request)
        {
            EnsureParametersBeforePayment();

            if (request.Money < 5000 || request.Money > 1000000000)
                throw new ArgumentException("Số tiền thanh toán phải nằm trong khoảng 5.000 đến 1.000.000.000 VND.");
            if (string.IsNullOrEmpty(request.Description))
                throw new ArgumentException("Không được để trống mô tả giao dịch.");
            if (string.IsNullOrEmpty(request.IpAddress))
                throw new ArgumentException("Không được để trống địa chỉ IP.");

            var data = new Dictionary<string, string>
            {
                { "vnp_Version", _version },
                { "vnp_Command", "pay" },
                { "vnp_TmnCode", _tmnCode },
                { "vnp_Amount", (request.Money * 100).ToString() },
                { "vnp_CreateDate", request.CreatedDate.ToString("yyyyMMddHHmmss") },
                { "vnp_CurrCode", request.Currency.ToString().ToUpper() },
                { "vnp_IpAddr", request.IpAddress },
                { "vnp_Locale", EnumHelper.GetDescription(request.Language) },
                { "vnp_BankCode", request.BankCode == BankCode.ANY ? string.Empty : request.BankCode.ToString() },
                { "vnp_OrderInfo", request.Description.Trim() },
                { "vnp_OrderType", _orderType },
                { "vnp_ReturnUrl", _callbackUrl },
                { "vnp_TxnRef", request.PaymentId.ToString() }
            };

            var signedQuery = BuildSignedQuery(data);
            return $"{_baseUrl}?{signedQuery}";
        }

        public VnpayPaymentResult GetPaymentResult(IQueryCollection parameters)
        {
            var responseData = parameters
                .Where(kv => !string.IsNullOrEmpty(kv.Key) && kv.Key.StartsWith("vnp_"))
                .ToDictionary(kv => kv.Key, kv => kv.Value.ToString());

            var requiredKeys = new[] { "vnp_BankCode", "vnp_OrderInfo", "vnp_TransactionNo",
                "vnp_ResponseCode", "vnp_TransactionStatus", "vnp_TxnRef", "vnp_SecureHash" };

            if (requiredKeys.Any(k => string.IsNullOrEmpty(responseData.GetValueOrDefault(k))))
                throw new ArgumentException("Không đủ dữ liệu để xác thực giao dịch");

            var helper = new PaymentHelper();
            foreach (var (key, value) in responseData)
                if (!key.Equals("vnp_SecureHash"))
                    helper.AddResponseData(key, value);

            var responseCode = (ResponseCode)sbyte.Parse(responseData["vnp_ResponseCode"]);
            var transactionStatusCode = (TransactionStatusCode)sbyte.Parse(responseData["vnp_TransactionStatus"]);

            return new VnpayPaymentResult
            {
                PaymentId = long.Parse(responseData["vnp_TxnRef"]),
                VnpayTransactionId = long.Parse(responseData["vnp_TransactionNo"]),
                IsSuccess = transactionStatusCode == TransactionStatusCode.Code_00 &&
                            responseCode == ResponseCode.Code_00 &&
                            helper.IsSignatureCorrect(responseData["vnp_SecureHash"], _hashSecret),
                Description = responseData["vnp_OrderInfo"],
                PaymentMethod = string.IsNullOrEmpty(responseData.GetValueOrDefault("vnp_CardType"))
                    ? "Không xác định" : responseData["vnp_CardType"],
                Timestamp = string.IsNullOrEmpty(responseData.GetValueOrDefault("vnp_PayDate"))
                    ? DateTime.Now
                    : DateTime.ParseExact(responseData["vnp_PayDate"], "yyyyMMddHHmmss", CultureInfo.InvariantCulture),
                TransactionStatus = new VnpayTransactionStatus
                {
                    Code = transactionStatusCode,
                    Description = EnumHelper.GetDescription(transactionStatusCode)
                },
                PaymentResponse = new VnpayPaymentResponse
                {
                    Code = responseCode,
                    Description = EnumHelper.GetDescription(responseCode)
                },
                BankingInfor = new VnpayBankingInfor
                {
                    BankCode = responseData["vnp_BankCode"],
                    BankTransactionId = string.IsNullOrEmpty(responseData.GetValueOrDefault("vnp_BankTranNo"))
                        ? "Không xác định" : responseData["vnp_BankTranNo"]
                }
            };
        }

        private void EnsureParametersBeforePayment()
        {
            if (string.IsNullOrEmpty(_baseUrl) || string.IsNullOrEmpty(_tmnCode) ||
                string.IsNullOrEmpty(_hashSecret) || string.IsNullOrEmpty(_callbackUrl))
                throw new ArgumentException("Không tìm thấy BaseUrl, TmnCode, HashSecret hoặc CallbackUrl");
        }

        private static string ComputeHmacSha512(string key, string data)
        {
            using var hmac = new System.Security.Cryptography.HMACSHA512(Encoding.UTF8.GetBytes(key));
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
            return BitConverter.ToString(hash).Replace("-", string.Empty);
        }
    }
}
