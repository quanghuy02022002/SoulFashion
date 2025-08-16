using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Repositories.DTOs;
using Services.Interfaces;
using System.Text.Json;
using System.Text;

namespace SoulFashion.Controllers
{
    [ApiController]
    [Route("api/payments")]
    public class PaymentsController : ControllerBase
    {
        private readonly IPaymentService _paymentService;
        private readonly IVnPayService _vnPayService;
        private readonly IMomoService _momoService;
        private readonly IPayOsService _payOsService;
        private readonly IConfiguration _config;

        public PaymentsController(
            IPaymentService paymentService,
            IVnPayService vnPayService,
            IMomoService momoService,
            IPayOsService payOsService,
            IConfiguration config)
        {
            _paymentService = paymentService;
            _vnPayService = vnPayService;
            _momoService = momoService;
            _payOsService = payOsService;
            _config = config;
        }

        [HttpGet("order/{orderId}")]
        public async Task<IActionResult> GetByOrder(int orderId) =>
            Ok(await _paymentService.GetByOrderIdAsync(orderId));

        [HttpPost("vnpay")]
        public async Task<IActionResult> CreateVnPayLink([FromBody] PaymentDto dto)
        {
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
            var txnRef = "TXN" + dto.OrderId + DateTime.UtcNow.Ticks;

            await _paymentService.CreatePaymentAsync(dto, txnRef);
            var url = _vnPayService.CreatePaymentUrl(dto, ip, txnRef);
            return Ok(new { paymentUrl = url });
        }

        [HttpPost("momo")]
        public async Task<IActionResult> CreateMomoLink([FromBody] PaymentDto dto)
        {
            var txnRef = "MOMO" + dto.OrderId + DateTime.UtcNow.Ticks;
            await _paymentService.CreatePaymentAsync(dto, txnRef);
            var payUrl = await _momoService.CreatePaymentAsync(dto, txnRef);
            return Ok(new { paymentUrl = payUrl });
        }

        // Tạo link thanh toán PayOS
        [HttpPost("payos")]
        public async Task<IActionResult> CreatePayOsLink([FromBody] PaymentDto dto)
        {
            try
            {
                await _paymentService.CreatePaymentAsync(dto, dto.OrderId.ToString());

                var (checkoutUrl, qrCode, rawResponse) = await _payOsService.CreatePaymentLinkAsync(dto.OrderId);

                return Ok(new { paymentUrl = checkoutUrl, qrCode, raw = rawResponse });
            }
            catch (Exception ex)
            {
                Console.WriteLine("PayOS create error: " + ex);
                return StatusCode(500, new { message = "PayOS create error", error = ex.Message });
            }
        }

        // Webhook PayOS
        [HttpPost("payos-webhook")]
        [AllowAnonymous]
        public async Task<IActionResult> PayOsWebhook()
        {
            using var reader = new StreamReader(Request.Body, Encoding.UTF8);
            var body = await reader.ReadToEndAsync();
            var signature = Request.Headers["x-signature"].ToString();

            if (!_payOsService.VerifyWebhook(body, signature))
                return Unauthorized(new { message = "Invalid signature" });

            using var doc = JsonDocument.Parse(body);
            var data = doc.RootElement.GetProperty("data");
            var status = data.GetProperty("status").GetString();
            var orderId = data.GetProperty("orderId").GetInt32();

            if (string.Equals(status, "PAID", StringComparison.OrdinalIgnoreCase))
                await _paymentService.MarkAsPaid(orderId.ToString());

            return Ok(new { message = "OK" });
        }

        // Return URL
        [HttpGet("payos-return")]
        [AllowAnonymous]
        public async Task<IActionResult> PayOsReturn([FromQuery] int orderId, [FromQuery] string status)
        {
            if (string.Equals(status, "PAID", StringComparison.OrdinalIgnoreCase))
            {
                await _paymentService.MarkAsPaid(orderId.ToString());
                return Redirect($"https://soul-of-fashion.vercel.app/payment-success?orderId={orderId}");
            }
            return Redirect("https://soul-of-fashion.vercel.app/payment-failed");
        }
    }
}
