using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Repositories.DTOs;
using Services.Interfaces;
using System.Text.Json;

namespace SoulFashion.Controllers
{
    [ApiController]
    [Route("api/payments")]
    public class PaymentsController : ControllerBase
    {
        private readonly IPaymentService _paymentService;
        private readonly IVnPayService _vnPayService;
        private readonly IMomoService _momoService;

        public PaymentsController(IPaymentService paymentService, IVnPayService vnPayService, IMomoService momoService)
        {
            _paymentService = paymentService;
            _vnPayService = vnPayService;
            _momoService = momoService;
        }

        [HttpGet("order/{orderId}")]
        public async Task<IActionResult> GetByOrder(int orderId) =>
            Ok(await _paymentService.GetByOrderIdAsync(orderId));

        [HttpPost("vnpay")]
        public async Task<IActionResult> CreateVnPayLink([FromBody] PaymentDto dto)
        {
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
            var txnRef = "TXN" + dto.OrderId + DateTime.Now.Ticks;

            // Chỉ lưu dto.OrderId + method, không dùng amount
            await _paymentService.CreatePaymentAsync(dto, txnRef);
            var url = _vnPayService.CreatePaymentUrl(dto, ip, txnRef);

            return Ok(new { paymentUrl = url });
        }

        [HttpPost("momo")]
        public async Task<IActionResult> CreateMomoLink([FromBody] PaymentDto dto)
        {
            var txnRef = "MOMO" + dto.OrderId + DateTime.Now.Ticks;

            await _paymentService.CreatePaymentAsync(dto, txnRef);
            var payUrl = await _momoService.CreatePaymentAsync(dto, txnRef);

            return Ok(new { paymentUrl = payUrl });
        }

        [HttpPost("vnpay-callback-test")]
        public async Task<IActionResult> TestCallback([FromBody] string txnRef)
        {
            if (string.IsNullOrWhiteSpace(txnRef))
                return BadRequest(new { success = false, message = "Thiếu transactionRef" });

            await _paymentService.MarkAsPaid(txnRef);

            return Ok(new
            {
                success = true,
                message = "Thanh toán test thành công",
                transactionRef = txnRef
            });
        }

        [HttpPost("momo-notify")]
        public async Task<IActionResult> MomoNotify([FromBody] JsonElement body)
        {
            Console.WriteLine("== Momo Notify Callback ==");
            Console.WriteLine(body.ToString());

            try
            {
                var orderId = body.GetProperty("orderId").GetString();
                var resultCode = body.GetProperty("resultCode").GetInt32();

                if (resultCode == 0 && !string.IsNullOrEmpty(orderId))
                {
                    await _paymentService.MarkAsPaid(orderId);
                    return Ok(new
                    {
                        message = "OK",
                        status = "paid",
                        transactionRef = orderId
                    });
                }

                return BadRequest(new { message = "FAILED", status = "error" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Notify error", error = ex.Message });
            }
        }

        [HttpGet("momo-return")]
        public IActionResult MomoReturn([FromQuery] string orderId, [FromQuery] int resultCode)
        {
            if (resultCode == 0)
            {
                return Ok(new
                {
                    success = true,
                    message = "Thanh toán thành công",
                    transactionRef = orderId
                });
            }

            return BadRequest(new
            {
                success = false,
                message = "Thanh toán thất bại hoặc bị hủy"
            });
        }
    }
}
