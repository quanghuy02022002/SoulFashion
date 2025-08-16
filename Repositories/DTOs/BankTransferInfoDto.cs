namespace Repositories.DTOs
{
    public class BankTransferInfoDto
    {
        public string BankName { get; set; } = string.Empty;
        public string AccountNumber { get; set; } = string.Empty;
        public string AccountName { get; set; } = string.Empty;
        public string Branch { get; set; } = string.Empty;
        public string TransferContent { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public int OrderId { get; set; }
        public string QrCodeUrl { get; set; } = string.Empty;
    }
}

