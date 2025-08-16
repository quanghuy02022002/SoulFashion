using Microsoft.Extensions.Configuration;
using Services.Interfaces;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace Services.Implementations
{
    public class QrCodeGeneratorService : IQrCodeGeneratorService
    {
        private readonly IConfiguration _config;
        private readonly HttpClient _httpClient;

        public QrCodeGeneratorService(IConfiguration config, HttpClient httpClient)
        {
            _config = config;
            _httpClient = httpClient;
        }

        public async Task<string> GenerateVietQRCodeAsync(string accountNumber, string accountName, decimal amount, string content)
        {
            try
            {
                // Sử dụng API của VietQR.net để tạo QR code đẹp
                var url = $"https://vietqr.net/api/qr-code?" +
                         $"bank=VCB" +
                         $"&account={accountNumber}" +
                         $"&name={Uri.EscapeDataString(accountName)}" +
                         $"&amount={amount:N0}" +
                         $"&content={Uri.EscapeDataString(content)}" +
                         $"&template=default" +
                         $"&size=400";

                // Kiểm tra xem API có hoạt động không
                var response = await _httpClient.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    return url;
                }

                // Fallback: Sử dụng QR server API
                var qrData = $"VCB|{accountNumber}|{accountName}|{amount:N0}|{content}";
                return $"https://api.qrserver.com/v1/create-qr-code/?size=400x400&data={Uri.EscapeDataString(qrData)}&format=png&margin=15&ecc=H&qzone=2";
            }
            catch (Exception)
            {
                // Fallback: Sử dụng QR server API
                var qrData = $"VCB|{accountNumber}|{accountName}|{amount:N0}|{content}";
                return $"https://api.qrserver.com/v1/create-qr-code/?size=400x400&data={Uri.EscapeDataString(qrData)}&format=png&margin=15&ecc=H&qzone=2";
            }
        }

        public async Task<byte[]> GenerateCustomQrCodeAsync(string data, int size = 400)
        {
            try
            {
                var url = $"https://api.qrserver.com/v1/create-qr-code/?size={size}x{size}&data={Uri.EscapeDataString(data)}&format=png&margin=15&ecc=H&qzone=2";
                var response = await _httpClient.GetByteArrayAsync(url);
                return response;
            }
            catch (Exception)
            {
                throw new Exception("Không thể tạo QR code");
            }
        }
    }
}
