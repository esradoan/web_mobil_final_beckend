using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartCampus.Business.Services;
using System.Security.Claims;

namespace SmartCampus.API.Controllers
{
    [ApiController]
    [Route("api/v1/wallet")]
    [Authorize]
    public class WalletController : ControllerBase
    {
        private readonly IWalletService _walletService;

        public WalletController(IWalletService walletService)
        {
            _walletService = walletService;
        }

        private int GetUserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");

        /// <summary>
        /// Bakiye sorgula
        /// </summary>
        [HttpGet("balance")]
        public async Task<IActionResult> GetBalance()
        {
            var result = await _walletService.GetBalanceAsync(GetUserId());
            return Ok(result);
        }

        /// <summary>
        /// Para yükleme oturumu oluştur
        /// Minimum: 50 TRY, Maximum: 5000 TRY
        /// </summary>
        [HttpPost("topup")]
        public async Task<IActionResult> TopUp([FromBody] TopUpRequestDto dto)
        {
            var result = await _walletService.CreateTopUpSessionAsync(GetUserId(), dto.Amount);
            
            if (!result.Success)
                return BadRequest(new { message = result.Message, error = "TopUpFailed" });

            return Ok(result);
        }

        /// <summary>
        /// Ödeme tamamlama (Demo - gerçek uygulamada webhook olacak)
        /// </summary>
        [HttpGet("topup/complete")]
        [AllowAnonymous]
        public async Task<IActionResult> CompleteTopUp([FromQuery] string @ref)
        {
            var success = await _walletService.ProcessTopUpWebhookAsync(@ref, true);
            
            if (!success)
                return NotFound(new { message = "Payment reference not found", error = "NotFound" });

            return Ok(new { message = "Payment completed successfully" });
        }

        /// <summary>
        /// Ödeme webhook'u (Stripe/PayTR)
        /// </summary>
        [HttpPost("topup/webhook")]
        [AllowAnonymous]
        public async Task<IActionResult> ProcessWebhook([FromBody] PaymentWebhookDto dto)
        {
            // In production, verify webhook signature here
            var success = await _walletService.ProcessTopUpWebhookAsync(dto.PaymentReference, dto.Success);
            
            if (!success)
                return NotFound(new { message = "Payment reference not found", error = "NotFound" });

            return Ok(new { received = true });
        }

        /// <summary>
        /// İşlem geçmişi (pagination)
        /// </summary>
        [HttpGet("transactions")]
        public async Task<IActionResult> GetTransactions([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            var result = await _walletService.GetTransactionsAsync(GetUserId(), page, pageSize);
            return Ok(new { 
                data = result, 
                page = page, 
                pageSize = pageSize 
            });
        }

        /// <summary>
        /// Manuel bakiye ekleme (Admin)
        /// </summary>
        [HttpPost("add-balance")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AddBalance([FromBody] AddBalanceDto dto)
        {
            var result = await _walletService.AddBalanceAsync(dto.UserId, dto.Amount, dto.Description);
            return Ok(result);
        }
    }

    public class PaymentWebhookDto
    {
        public string PaymentReference { get; set; } = string.Empty;
        public bool Success { get; set; }
    }

    public class AddBalanceDto
    {
        public int UserId { get; set; }
        public decimal Amount { get; set; }
        public string Description { get; set; } = string.Empty;
    }
}
