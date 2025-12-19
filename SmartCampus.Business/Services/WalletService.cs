using SmartCampus.DataAccess;
using SmartCampus.Entities;
using Microsoft.EntityFrameworkCore;

namespace SmartCampus.Business.Services
{
    public interface IWalletService
    {
        Task<WalletDto> GetBalanceAsync(int userId);
        Task<WalletDto> GetOrCreateWalletAsync(int userId);
        Task<TopUpResultDto> CreateTopUpSessionAsync(int userId, decimal amount);
        Task<bool> ProcessTopUpWebhookAsync(string paymentReference, bool success);
        Task<List<TransactionDto>> GetTransactionsAsync(int userId, int page = 1, int pageSize = 20);
        Task<WalletDto> AddBalanceAsync(int userId, decimal amount, string description);
    }

    // ==================== DTOs ====================

    public class WalletDto
    {
        public int Id { get; set; }
        public decimal Balance { get; set; }
        public string Currency { get; set; } = "TRY";
        public bool IsActive { get; set; }
    }

    public class TopUpResultDto
    {
        public bool Success { get; set; }
        public string? PaymentUrl { get; set; }
        public string? PaymentReference { get; set; }
        public string? Message { get; set; }
    }

    public class TransactionDto
    {
        public int Id { get; set; }
        public string Type { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public decimal BalanceAfter { get; set; }
        public string ReferenceType { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    public class TopUpRequestDto
    {
        public decimal Amount { get; set; }
    }

    // ==================== SERVICE ====================

    public class WalletService : IWalletService
    {
        private readonly CampusDbContext _context;
        private const decimal MIN_TOPUP_AMOUNT = 50;
        private const decimal MAX_TOPUP_AMOUNT = 5000;

        public WalletService(CampusDbContext context)
        {
            _context = context;
        }

        public async Task<WalletDto> GetBalanceAsync(int userId)
        {
            var wallet = await GetOrCreateWalletInternalAsync(userId);
            return MapToWalletDto(wallet);
        }

        public async Task<WalletDto> GetOrCreateWalletAsync(int userId)
        {
            var wallet = await GetOrCreateWalletInternalAsync(userId);
            return MapToWalletDto(wallet);
        }

        public async Task<TopUpResultDto> CreateTopUpSessionAsync(int userId, decimal amount)
        {
            // Validate amount
            if (amount < MIN_TOPUP_AMOUNT)
            {
                return new TopUpResultDto
                {
                    Success = false,
                    Message = $"Minimum top-up amount is {MIN_TOPUP_AMOUNT} TRY"
                };
            }

            if (amount > MAX_TOPUP_AMOUNT)
            {
                return new TopUpResultDto
                {
                    Success = false,
                    Message = $"Maximum top-up amount is {MAX_TOPUP_AMOUNT} TRY"
                };
            }

            var wallet = await GetOrCreateWalletInternalAsync(userId);

            // Create pending transaction
            var paymentReference = $"PAY-{Guid.NewGuid():N}"[..24].ToUpper();

            var transaction = new Transaction
            {
                WalletId = wallet.Id,
                Type = "credit",
                Amount = amount,
                BalanceAfter = wallet.Balance, // Will be updated on success
                ReferenceType = "topup",
                Description = $"Para yükleme - {amount:N2} TRY",
                PaymentReference = paymentReference,
                Status = "pending",
                CreatedAt = DateTime.UtcNow
            };

            _context.Transactions.Add(transaction);
            await _context.SaveChangesAsync();

            // In production, this would integrate with Stripe/PayTR
            // For demo purposes, return a mock payment URL
            var paymentUrl = $"/api/v1/wallet/topup/complete?ref={paymentReference}";

            return new TopUpResultDto
            {
                Success = true,
                PaymentUrl = paymentUrl,
                PaymentReference = paymentReference,
                Message = "Payment session created"
            };
        }

        public async Task<bool> ProcessTopUpWebhookAsync(string paymentReference, bool success)
        {
            var transaction = await _context.Transactions
                .Include(t => t.Wallet)
                .FirstOrDefaultAsync(t => t.PaymentReference == paymentReference && t.Status == "pending");

            if (transaction == null)
                return false;

            if (success)
            {
                // Update transaction
                transaction.Status = "completed";
                transaction.BalanceAfter = transaction.Wallet!.Balance + transaction.Amount;

                // Update wallet balance (atomic)
                transaction.Wallet.Balance += transaction.Amount;
                transaction.Wallet.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                transaction.Status = "failed";
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<List<TransactionDto>> GetTransactionsAsync(int userId, int page = 1, int pageSize = 20)
        {
            var wallet = await _context.Wallets.FirstOrDefaultAsync(w => w.UserId == userId);
            if (wallet == null)
                return new List<TransactionDto>();

            var transactions = await _context.Transactions
                .Where(t => t.WalletId == wallet.Id)
                .OrderByDescending(t => t.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return transactions.Select(t => new TransactionDto
            {
                Id = t.Id,
                Type = t.Type,
                Amount = t.Amount,
                BalanceAfter = t.BalanceAfter,
                ReferenceType = t.ReferenceType,
                Description = t.Description,
                Status = t.Status,
                CreatedAt = t.CreatedAt
            }).ToList();
        }

        public async Task<WalletDto> AddBalanceAsync(int userId, decimal amount, string description)
        {
            var wallet = await GetOrCreateWalletInternalAsync(userId);

            wallet.Balance += amount;
            wallet.UpdatedAt = DateTime.UtcNow;

            var transaction = new Transaction
            {
                WalletId = wallet.Id,
                Type = amount >= 0 ? "credit" : "debit",
                Amount = Math.Abs(amount),
                BalanceAfter = wallet.Balance,
                ReferenceType = "admin",
                Description = description,
                Status = "completed",
                CreatedAt = DateTime.UtcNow
            };

            _context.Transactions.Add(transaction);
            await _context.SaveChangesAsync();

            return MapToWalletDto(wallet);
        }

        // ==================== HELPER METHODS ====================

        private async Task<Wallet> GetOrCreateWalletInternalAsync(int userId)
        {
            var wallet = await _context.Wallets.FirstOrDefaultAsync(w => w.UserId == userId);

            if (wallet == null)
            {
                wallet = new Wallet
                {
                    UserId = userId,
                    Balance = 0,
                    Currency = "TRY",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Wallets.Add(wallet);
                await _context.SaveChangesAsync();
            }

            return wallet;
        }

        private WalletDto MapToWalletDto(Wallet wallet)
        {
            return new WalletDto
            {
                Id = wallet.Id,
                Balance = wallet.Balance,
                Currency = wallet.Currency,
                IsActive = wallet.IsActive
            };
        }
    }
}
