using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repositories.DTOs
{
    public class PendingTransferDto
    {
        public string BankName { get; set; } = "Vietcombank";
        public string AccountNumber { get; set; } = "1015917390";
        public string AccountName { get; set; } = "NGUYEN QUANG HUY";
        public string Branch { get; set; } = "Ho Chi Minh City";
        public string TransferContent { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public int OrderId { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
