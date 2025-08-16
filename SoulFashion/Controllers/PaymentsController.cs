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

        [HttpPost("payos")]
        public async Task<IActionResult> CreatePayOsLink([FromBody] PaymentDto dto)
        {
            try
            {
                // Lưu payment pending trước
                await _paymentService.CreatePaymentAsync(dto, dto.OrderId.ToString());

                // Tạo link PayOS dựa trên orderId (không tạo orderCode khác)
                var (checkoutUrl, qrCode, rawResponse) = await _payOsService.CreatePaymentLinkAsync(dto.OrderId);

                return Ok(new
                {
                    paymentUrl = checkoutUrl,
                    qrCode,
                    raw = rawResponse
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine("PayOS create error: " + ex);
                return StatusCode(500, new { message = "PayOS create error", error = ex.Message });
            }
        }

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
            if (!doc.RootElement.TryGetProperty("data", out var dataEl) || dataEl.ValueKind != JsonValueKind.Object)
                return BadRequest(new { message = "Invalid data in webhook" });

            var status = dataEl.GetProperty("status").GetString();
            var orderCode = dataEl.GetProperty("orderCode").GetString();

            if (string.Equals(status, "PAID", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(orderCode))
            {
                await _paymentService.MarkAsPaid(orderCode);
            }

            return Ok(new { message = "OK" });
        }

        [HttpGet("payos-return")]
        [AllowAnonymous]
        public async Task<IActionResult> PayOsReturn([FromQuery] string orderCode, [FromQuery] string status)
        {
            if (string.Equals(status, "PAID", StringComparison.OrdinalIgnoreCase))
            {
                await _paymentService.MarkAsPaid(orderCode);
                var payment = await _paymentService.GetByTxnRefAsync(orderCode);
                return Redirect($"https://soul-of-fashion.vercel.app/payment-success?txnRef={orderCode}&orderId={payment.OrderId}");
            }
            return Redirect("https://soul-of-fashion.vercel.app/payment-failed");
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            await _paymentService.DeleteAsync(id);
            return NoContent();
        }

        [HttpPut]
        public async Task<IActionResult> Update([FromBody] PaymentDto dto)
        {
            await _paymentService.UpdateAsync(dto);
            return Ok(new { message = "Cập nhật thành công" });
        }
    }
}
