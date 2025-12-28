using Microsoft.EntityFrameworkCore;
using SmartCampus.Business.Services;
using SmartCampus.DataAccess;
using SmartCampus.Entities;
using System;
using System.Threading.Tasks;
using Xunit;

namespace SmartCampus.Tests.Services
{
    public class WalletServiceTests : IDisposable
    {
        private readonly CampusDbContext _context;
        private readonly WalletService _service;

        public WalletServiceTests()
        {
            var options = new DbContextOptionsBuilder<CampusDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            
            _context = new CampusDbContext(options);
            _service = new WalletService(_context);
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }

        // ==================== GetBalanceAsync Tests ====================

        [Fact]
        public async Task GetBalanceAsync_ShouldCreateWallet_WhenNotExists()
        {
            // Arrange
            var userId = 1001;

            // Act
            var result = await _service.GetBalanceAsync(userId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(0, result.Balance);
            Assert.Equal("TRY", result.Currency);
            Assert.True(result.IsActive);
        }

        [Fact]
        public async Task GetBalanceAsync_ShouldReturnExistingWallet()
        {
            // Arrange
            var userId = 1001;
            var wallet = new Wallet { UserId = userId, Balance = 150.50m, Currency = "TRY", IsActive = true };
            _context.Wallets.Add(wallet);
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.GetBalanceAsync(userId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(150.50m, result.Balance);
        }

        // ==================== GetOrCreateWalletAsync Tests ====================

        [Fact]
        public async Task GetOrCreateWalletAsync_ShouldCreateNewWallet()
        {
            // Arrange
            var userId = 2001;

            // Act
            var result = await _service.GetOrCreateWalletAsync(userId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(0, result.Balance);
            
            var dbWallet = await _context.Wallets.FirstOrDefaultAsync(w => w.UserId == userId);
            Assert.NotNull(dbWallet);
        }

        // ==================== CreateTopUpSessionAsync Tests ====================

        [Fact]
        public async Task CreateTopUpSessionAsync_ShouldFail_WhenAmountTooLow()
        {
            // Arrange
            var userId = 1001;
            var amount = 10m; // MIN_TOPUP_AMOUNT = 50

            // Act
            var result = await _service.CreateTopUpSessionAsync(userId, amount);

            // Assert
            Assert.False(result.Success);
            Assert.Contains("Minimum", result.Message);
        }

        [Fact]
        public async Task CreateTopUpSessionAsync_ShouldFail_WhenAmountTooHigh()
        {
            // Arrange
            var userId = 1001;
            var amount = 10000m; // MAX_TOPUP_AMOUNT = 5000

            // Act
            var result = await _service.CreateTopUpSessionAsync(userId, amount);

            // Assert
            Assert.False(result.Success);
            Assert.Contains("Maximum", result.Message);
        }

        [Fact]
        public async Task CreateTopUpSessionAsync_ShouldSucceed_WhenValidAmount()
        {
            // Arrange
            var userId = 1001;
            var amount = 100m;

            // Act
            var result = await _service.CreateTopUpSessionAsync(userId, amount);

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.PaymentUrl);
            Assert.NotNull(result.PaymentReference);
            Assert.Equal("Payment session created", result.Message);

            // Verify transaction created
            var tx = await _context.Transactions.FirstOrDefaultAsync(t => t.PaymentReference == result.PaymentReference);
            Assert.NotNull(tx);
            Assert.Equal("pending", tx.Status);
            Assert.Equal(amount, tx.Amount);
        }

        // ==================== ProcessTopUpWebhookAsync Tests ====================

        [Fact]
        public async Task ProcessTopUpWebhookAsync_ShouldReturnFalse_WhenTransactionNotFound()
        {
            // Act
            var result = await _service.ProcessTopUpWebhookAsync("INVALID-REF", true);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task ProcessTopUpWebhookAsync_ShouldUpdateBalance_WhenSuccess()
        {
            // Arrange
            var userId = 1001;
            var wallet = new Wallet { Id = 1, UserId = userId, Balance = 100m, Currency = "TRY", IsActive = true };
            _context.Wallets.Add(wallet);

            var paymentRef = "PAY-TEST123";
            var tx = new Transaction
            {
                WalletId = 1,
                Type = "credit",
                Amount = 200m,
                BalanceAfter = 100m,
                ReferenceType = "topup",
                PaymentReference = paymentRef,
                Status = "pending",
                CreatedAt = DateTime.UtcNow
            };
            _context.Transactions.Add(tx);
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.ProcessTopUpWebhookAsync(paymentRef, true);

            // Assert
            Assert.True(result);

            var updatedWallet = await _context.Wallets.FindAsync(1);
            Assert.Equal(300m, updatedWallet!.Balance);

            var updatedTx = await _context.Transactions.FirstAsync(t => t.PaymentReference == paymentRef);
            Assert.Equal("completed", updatedTx.Status);
        }

        [Fact]
        public async Task ProcessTopUpWebhookAsync_ShouldMarkFailed_WhenNotSuccess()
        {
            // Arrange
            var userId = 1001;
            var wallet = new Wallet { Id = 1, UserId = userId, Balance = 100m, Currency = "TRY", IsActive = true };
            _context.Wallets.Add(wallet);

            var paymentRef = "PAY-FAIL123";
            var tx = new Transaction
            {
                WalletId = 1,
                Type = "credit",
                Amount = 200m,
                BalanceAfter = 100m,
                ReferenceType = "topup",
                PaymentReference = paymentRef,
                Status = "pending",
                CreatedAt = DateTime.UtcNow
            };
            _context.Transactions.Add(tx);
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.ProcessTopUpWebhookAsync(paymentRef, false);

            // Assert
            Assert.True(result);

            var updatedTx = await _context.Transactions.FirstAsync(t => t.PaymentReference == paymentRef);
            Assert.Equal("failed", updatedTx.Status);

            // Balance should remain unchanged
            var updatedWallet = await _context.Wallets.FindAsync(1);
            Assert.Equal(100m, updatedWallet!.Balance);
        }

        // ==================== GetTransactionsAsync Tests ====================

        [Fact]
        public async Task GetTransactionsAsync_ShouldReturnEmpty_WhenNoWallet()
        {
            // Act
            var result = await _service.GetTransactionsAsync(9999);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetTransactionsAsync_ShouldReturnTransactions()
        {
            // Arrange
            var userId = 1001;
            var wallet = new Wallet { Id = 1, UserId = userId, Balance = 100m, Currency = "TRY", IsActive = true };
            _context.Wallets.Add(wallet);

            _context.Transactions.Add(new Transaction { WalletId = 1, Type = "credit", Amount = 50m, Status = "completed", CreatedAt = DateTime.UtcNow });
            _context.Transactions.Add(new Transaction { WalletId = 1, Type = "debit", Amount = 20m, Status = "completed", CreatedAt = DateTime.UtcNow });
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.GetTransactionsAsync(userId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
        }

        // ==================== AddBalanceAsync Tests ====================

        [Fact]
        public async Task AddBalanceAsync_ShouldAddPositiveAmount()
        {
            // Arrange
            var userId = 1001;
            var wallet = new Wallet { Id = 1, UserId = userId, Balance = 100m, Currency = "TRY", IsActive = true };
            _context.Wallets.Add(wallet);
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.AddBalanceAsync(userId, 50m, "Admin credit");

            // Assert
            Assert.NotNull(result);
            Assert.Equal(150m, result.Balance);

            var tx = await _context.Transactions.FirstOrDefaultAsync(t => t.WalletId == 1);
            Assert.NotNull(tx);
            Assert.Equal("credit", tx.Type);
            Assert.Equal(50m, tx.Amount);
        }

        [Fact]
        public async Task AddBalanceAsync_ShouldDeductNegativeAmount()
        {
            // Arrange
            var userId = 1001;
            var wallet = new Wallet { Id = 1, UserId = userId, Balance = 100m, Currency = "TRY", IsActive = true };
            _context.Wallets.Add(wallet);
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.AddBalanceAsync(userId, -30m, "Admin debit");

            // Assert
            Assert.NotNull(result);
            Assert.Equal(70m, result.Balance);

            var tx = await _context.Transactions.FirstOrDefaultAsync(t => t.WalletId == 1);
            Assert.NotNull(tx);
            Assert.Equal("debit", tx.Type);
            Assert.Equal(30m, tx.Amount); // Stored as positive
        }
    }
}
