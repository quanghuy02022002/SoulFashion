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
        private readonly IPayOsService _payOsService; // ✅ thêm
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

        // ====== VNPay CALLBACK (GET) — VNPay redirect về bằng GET ======
        [HttpGet("vnpay-callback")]
        [AllowAnonymous]
        public async Task<IActionResult> VnPayCallback()
        {
            var isValid = _vnPayService.ValidateResponse(Request.Query, out var txnRef);
            var code = Request.Query["vnp_ResponseCode"].ToString();
            var status = Request.Query["vnp_TransactionStatus"].ToString();

            if (isValid && code == "00" && status == "00")
            {
                await _paymentService.MarkAsPaid(txnRef);

                var payment = await _paymentService.GetByTxnRefAsync(txnRef); // lấy orderId
                return Redirect($"https://soul-of-fashion.vercel.app/payment-success?txnRef={txnRef}&orderId={payment.OrderId}");
            }

            return Redirect("https://soul-of-fashion.vercel.app/payment-failed");
        }



        // (tuỳ VNPay/hoặc muốn bắt notify dạng POST)
        [HttpPost("vnpay-callback")]
        [AllowAnonymous]
        public Task<IActionResult> VnPayCallbackPost() => VnPayCallback();

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
            try
            {
                var orderId = body.GetProperty("orderId").GetString();
                var resultCode = body.GetProperty("resultCode").GetInt32();

                if (resultCode == 0 && !string.IsNullOrEmpty(orderId))
                {
                    await _paymentService.MarkAsPaid(orderId);
                    return Ok(new { message = "OK", status = "paid", transactionRef = orderId });
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
                return Ok(new { success = true, message = "Thanh toán thành công", transactionRef = orderId });

            return BadRequest(new { success = false, message = "Thanh toán thất bại hoặc bị hủy" });
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

        // ====== Tạo link PayOS ======
        [HttpPost("payos")]
        public async Task<IActionResult> CreatePayOsLink([FromBody] PaymentDto dto)
        {
            try
            {
                // orderCode số nguyên ≤ 9007199254740991
                long unixMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                long orderCodeNum = unixMs % 9000000000000000 + dto.OrderId;
                string orderCode = orderCodeNum.ToString();

                await _paymentService.CreatePaymentAsync(dto, orderCode);
                var (checkoutUrl, qrCode, rawResponse) = await _payOsService.CreatePaymentLinkAsync(dto.OrderId, orderCode);

                return Ok(new { paymentUrl = checkoutUrl, qrCode, raw = rawResponse });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message, stack = ex.StackTrace });
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
            var data = doc.RootElement.GetProperty("data");
            var status = data.GetProperty("status").GetString();
            var orderCode = data.GetProperty("orderCode").GetString();

            if (string.Equals(status, "PAID", StringComparison.OrdinalIgnoreCase)
                && !string.IsNullOrWhiteSpace(orderCode))
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
    }
    }
