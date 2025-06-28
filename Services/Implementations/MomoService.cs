using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Repositories.DTOs;
using Services.Interfaces;

namespace Services.Implementations
{

    public class MomoService : IMomoService
    {
        private readonly IConfiguration _config;
        private readonly HttpClient _http;

        public MomoService(IConfiguration config)
        {
            _config = config;
            _http = new HttpClient();
        }

        public async Task<string> CreatePaymentAsync(PaymentDto dto, string txnRef)
        {
            var endpoint = _config["Momo:ApiEndpoint"];
            var partnerCode = _config["Momo:PartnerCode"];
            var accessKey = _config["Momo:AccessKey"];
            var secretKey = _config["Momo:SecretKey"];
            var returnUrl = _config["Momo:ReturnUrl"];
            var notifyUrl = _config["Momo:NotifyUrl"];

            var orderInfo = $"Thanh toán đơn hàng #{dto.OrderId}";
            var amount = dto.Amount.ToString("0");
            var requestId = txnRef;
            var orderId = txnRef;
            var requestType = "captureWallet";

            string rawHash = $"accessKey={accessKey}&amount={amount}&extraData=&ipnUrl={notifyUrl}&orderId={orderId}&orderInfo={orderInfo}&partnerCode={partnerCode}&redirectUrl={returnUrl}&requestId={requestId}&requestType={requestType}";
            string signature = CreateSignature(secretKey, rawHash);

            var payload = new
            {
                partnerCode,
                accessKey,
                requestId,
                amount,
                orderId,
                orderInfo,
                redirectUrl = returnUrl,
                ipnUrl = notifyUrl,
                requestType,
                extraData = "",
                signature,
                lang = "vi"
            };

            var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
            var response = await _http.PostAsync(endpoint, content);
            var responseContent = await response.Content.ReadAsStringAsync();

            var json = JsonDocument.Parse(responseContent);
            var payUrl = json.RootElement.GetProperty("payUrl").GetString();

            return payUrl ?? throw new Exception("Không nhận được payUrl từ Momo");
        }

        private string CreateSignature(string secretKey, string rawData)
        {
            var encoding = new UTF8Encoding();
            byte[] keyBytes = encoding.GetBytes(secretKey);
            byte[] messageBytes = encoding.GetBytes(rawData);

            using var hmacsha256 = new HMACSHA256(keyBytes);
            byte[] hashMessage = hmacsha256.ComputeHash(messageBytes);
            return BitConverter.ToString(hashMessage).Replace("-", "").ToLower();
        }
    }
}
