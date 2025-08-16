using System.Threading.Tasks;

namespace Services.Interfaces
{
    public interface IQrCodeGeneratorService
    {
        Task<string> GenerateVietQRCodeAsync(string accountNumber, string accountName, decimal amount, string content);
        Task<byte[]> GenerateCustomQrCodeAsync(string data, int size = 400);
    }
}
