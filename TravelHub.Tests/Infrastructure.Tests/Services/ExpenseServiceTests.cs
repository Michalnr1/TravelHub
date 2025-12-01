using Moq;
using TravelHub.Domain.DTOs;
using TravelHub.Domain.Entities;
using TravelHub.Domain.Interfaces.Repositories;
using TravelHub.Domain.Interfaces.Services;
using TravelHub.Infrastructure.Services;

namespace TravelHub.Tests.Infrastructure.Tests.Services
{
    public class ExpenseServiceTests
    {
        private readonly Mock<IExpenseRepository> _expenseRepositoryMock;
        private readonly Mock<ITripService> _tripServiceMock;
        private readonly Mock<IExchangeRateService> _exchangeRateServiceMock;
        private readonly ExpenseService _expenseService;

        public ExpenseServiceTests()
        {
            _expenseRepositoryMock = new Mock<IExpenseRepository>();
            _tripServiceMock = new Mock<ITripService>();
            _exchangeRateServiceMock = new Mock<IExchangeRateService>();

            _expenseService = new ExpenseService(
                _expenseRepositoryMock.Object,
                _exchangeRateServiceMock.Object,
                _tripServiceMock.Object
            );
        }

        #region GetByIdAsync Tests

        [Fact]
        public async Task GetByIdAsync_WithValidIntId_ReturnsExpenseWithParticipants()
        {
            // Arrange
            var expenseId = 1;
            var expectedExpense = new Expense { Id = expenseId, Name = "Test Expense", PaidById = "test" };

            _expenseRepositoryMock
                .Setup(x => x.GetByIdWithParticipantsAsync(expenseId))
                .ReturnsAsync(expectedExpense);

            // Act
            var result = await _expenseService.GetByIdAsync(expenseId);

            // Assert
            Assert.Equal(expectedExpense, result);
            _expenseRepositoryMock.Verify(x => x.GetByIdWithParticipantsAsync(expenseId), Times.Once);
        }

        [Fact]
        public async Task GetByIdAsync_WithNonExistingIntId_ThrowsInvalidOperationException()
        {
            // Arrange
            var expenseId = 999;

            _expenseRepositoryMock
                .Setup(x => x.GetByIdWithParticipantsAsync(expenseId))
                .ReturnsAsync((Expense)null);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _expenseService.GetByIdAsync(expenseId));
        }

        [Fact]
        public async Task GetByIdAsync_WithNonIntId_CallsBaseMethod()
        {
            // Arrange
            var stringId = "invalid-id";

            // Act & Assert - This should use the base implementation
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _expenseService.GetByIdAsync(stringId));
        }

        #endregion

        #region AddAsync Tests

        [Fact]
        public async Task AddAsync_WithoutParticipantShares_CallsRepositoryAdd()
        {
            // Arrange
            var expense = new Expense { Id = 1, Value = 100m, PaidById = "test", Name = "test" };

            _expenseRepositoryMock
                .Setup(x => x.AddAsync(expense))
                .ReturnsAsync(expense);

            // Act
            var result = await _expenseService.AddAsync(expense);

            // Assert
            Assert.Equal(expense, result);
            _expenseRepositoryMock.Verify(x => x.AddAsync(expense), Times.Once);
        }

        [Fact]
        public async Task AddAsync_WithAmountShares_CreatesParticipantsWithSpecifiedAmounts()
        {
            // Arrange
            var expense = new Expense { Id = 1, Value = 100m, PaidById = "test", Name = "test" };
            var participantShares = new List<ParticipantShareDto>
            {
                new() { PersonId = "user1", ShareType = 1, InputValue = 60m }, // Amount
                new() { PersonId = "user2", ShareType = 1, InputValue = 40m }  // Amount
            };

            _expenseRepositoryMock
                .Setup(x => x.AddAsync(It.IsAny<Expense>()))
                .ReturnsAsync((Expense e) => e);

            // Act
            var result = await _expenseService.AddAsync(expense, participantShares);

            // Assert
            Assert.Equal(2, result.Participants.Count);

            var participant1 = result.Participants.First(p => p.PersonId == "user1");
            var participant2 = result.Participants.First(p => p.PersonId == "user2");

            Assert.Equal(0.600m, participant1.Share);
            Assert.Equal(60.00m, participant1.ActualShareValue);
            Assert.Equal(0.400m, participant2.Share);
            Assert.Equal(40.00m, participant2.ActualShareValue);
        }

        [Fact]
        public async Task AddAsync_WithPercentageShares_CreatesParticipantsWithSpecifiedPercentages()
        {
            // Arrange
            var expense = new Expense { Id = 1, Value = 200m, PaidById = "test", Name = "test" };
            var participantShares = new List<ParticipantShareDto>
            {
                new() { PersonId = "user1", ShareType = 2, InputValue = 0.3m }, // 30%
                new() { PersonId = "user2", ShareType = 2, InputValue = 0.7m }  // 70%
            };

            _expenseRepositoryMock
                .Setup(x => x.AddAsync(It.IsAny<Expense>()))
                .ReturnsAsync((Expense e) => e);

            // Act
            var result = await _expenseService.AddAsync(expense, participantShares);

            // Assert
            Assert.Equal(2, result.Participants.Count);

            var participant1 = result.Participants.First(p => p.PersonId == "user1");
            var participant2 = result.Participants.First(p => p.PersonId == "user2");

            Assert.Equal(0.300m, participant1.Share);
            Assert.Equal(60.00m, participant1.ActualShareValue);
            Assert.Equal(0.700m, participant2.Share);
            Assert.Equal(140.00m, participant2.ActualShareValue);
        }

        [Fact]
        public async Task AddAsync_WithZeroExpenseValue_CreatesZeroShares()
        {
            // Arrange
            var expense = new Expense { Id = 1, Value = 0m, PaidById = "test", Name = "test" };
            var participantShares = new List<ParticipantShareDto>
            {
                new() { PersonId = "user1", ShareType = 1, InputValue = 0m },
                new() { PersonId = "user2", ShareType = 1, InputValue = 0m }
            };

            _expenseRepositoryMock
                .Setup(x => x.AddAsync(It.IsAny<Expense>()))
                .ReturnsAsync((Expense e) => e);

            // Act
            var result = await _expenseService.AddAsync(expense, participantShares);

            // Assert
            Assert.All(result.Participants, p =>
            {
                Assert.Equal(0.000m, p.Share);
                Assert.Equal(0.00m, p.ActualShareValue);
            });
        }

        #endregion

        #region UpdateAsync Tests

        [Fact]
        public async Task UpdateAsync_WithParticipantShares_UpdatesParticipantsCorrectly()
        {
            // Arrange
            var existingExpense = new Expense
            {
                Id = 1,
                Value = 100m,
                Participants = new List<ExpenseParticipant>
                {
                    new() { PersonId = "user1", Share = 0.5m, ActualShareValue = 50m },
                    new() { PersonId = "user2", Share = 0.5m, ActualShareValue = 50m }
                },
                PaidById = "test", Name = "test"
            };

            var newParticipantShares = new List<ParticipantShareDto>
            {
                new() { PersonId = "user1", ShareType = 1, InputValue = 70m }, // Changed to 70
                new() { PersonId = "user3", ShareType = 1, InputValue = 30m }  // New participant
            };

            _expenseRepositoryMock
                .Setup(x => x.UpdateAsync(It.IsAny<Expense>()))
                .Returns(Task.CompletedTask);

            // Act
            await _expenseService.UpdateAsync(existingExpense, newParticipantShares);

            // Assert
            Assert.Equal(2, existingExpense.Participants.Count);

            var participant1 = existingExpense.Participants.First(p => p.PersonId == "user1");
            var participant3 = existingExpense.Participants.First(p => p.PersonId == "user3");

            Assert.Equal(0.700m, participant1.Share);
            Assert.Equal(70.00m, participant1.ActualShareValue);
            Assert.Equal(0.300m, participant3.Share);
            Assert.Equal(30.00m, participant3.ActualShareValue);

            _expenseRepositoryMock.Verify(x => x.UpdateAsync(existingExpense), Times.Once);
        }

        [Fact]
        public async Task UpdateAsync_RemovingParticipants_RemovesCorrectly()
        {
            // Arrange
            var existingExpense = new Expense
            {
                Id = 1,
                Value = 100m,
                Participants = new List<ExpenseParticipant>
                {
                    new() { PersonId = "user1", Share = 0.33m, ActualShareValue = 33m },
                    new() { PersonId = "user2", Share = 0.33m, ActualShareValue = 33m },
                    new() { PersonId = "user3", Share = 0.34m, ActualShareValue = 34m }
                },
                PaidById = "test",
                Name = "test"
            };

            var newParticipantShares = new List<ParticipantShareDto>
            {
                new() { PersonId = "user1", ShareType = 1, InputValue = 100m } // Only user1 remains
            };

            _expenseRepositoryMock
                .Setup(x => x.UpdateAsync(It.IsAny<Expense>()))
                .Returns(Task.CompletedTask);

            // Act
            await _expenseService.UpdateAsync(existingExpense, newParticipantShares);

            // Assert
            Assert.Single(existingExpense.Participants);
            Assert.Equal("user1", existingExpense.Participants.First().PersonId);
            Assert.Equal(1.000m, existingExpense.Participants.First().Share);
            Assert.Equal(100.00m, existingExpense.Participants.First().ActualShareValue);
        }

        #endregion

        #region GetUserExpensesAsync Tests

        [Fact]
        public async Task GetUserExpensesAsync_WithValidUserId_ReturnsExpenses()
        {
            // Arrange
            var userId = "user123";
            var expectedExpenses = new List<Expense>
            {
                new() { Id = 1, Name = "Expense 1", PaidById = userId },
                new() { Id = 2, Name = "Expense 2", PaidById = userId }
            };

            _expenseRepositoryMock
                .Setup(x => x.GetExpensesByUserIdAsync(userId))
                .ReturnsAsync(expectedExpenses);

            // Act
            var result = await _expenseService.GetUserExpensesAsync(userId);

            // Assert
            Assert.Equal(expectedExpenses, result);
            _expenseRepositoryMock.Verify(x => x.GetExpensesByUserIdAsync(userId), Times.Once);
        }

        #endregion

        #region CalculateTripExpensesInTripCurrencyAsync Tests

        [Fact]
        public async Task CalculateTripExpensesInTripCurrencyAsync_SameCurrency_NoConversion()
        {
            // Arrange
            var tripId = 1;
            var tripCurrency = CurrencyCode.USD;
            var expenses = new List<Expense>
            {
                new()
                {
                    Id = 1,
                    Value = 100m,
                    IsEstimated = false,
                    ExchangeRate = new ExchangeRate { CurrencyCodeKey = CurrencyCode.USD, ExchangeRateValue = 1.0m },
                    AdditionalFee = 5m,
                    PercentageFee = 10m, 
                    PaidById = "test", Name = "test"
                }
            };

            _expenseRepositoryMock
                .Setup(x => x.GetByTripIdWithParticipantsAsync(tripId))
                .ReturnsAsync(expenses);

            // Act
            var result = await _expenseService.CalculateTripExpensesInTripCurrencyAsync(tripId, tripCurrency);

            // Assert
            Assert.Single(result.ExpenseCalculations);
            var calculation = result.ExpenseCalculations.First();
            // 100 + 5 + (100 * 0.1) = 115
            Assert.Equal(115m, calculation.ConvertedValue);
        }

        [Fact]
        public async Task CalculateTripExpensesInTripCurrencyAsync_DifferentCurrency_WithConversion()
        {
            // Arrange
            var tripId = 1;
            var tripCurrency = CurrencyCode.USD;
            var expenses = new List<Expense>
            {
                new()
                {
                    Id = 1,
                    Value = 100m,
                    IsEstimated = false,
                    ExchangeRate = new ExchangeRate { CurrencyCodeKey = CurrencyCode.EUR, ExchangeRateValue = 1.2m },
                    AdditionalFee = 5m,
                    PercentageFee = 10m, 
                    PaidById = "test", Name = "test"
                }
            };

            _expenseRepositoryMock
                .Setup(x => x.GetByTripIdWithParticipantsAsync(tripId))
                .ReturnsAsync(expenses);

            // Act
            var result = await _expenseService.CalculateTripExpensesInTripCurrencyAsync(tripId, tripCurrency);

            // Assert
            var calculation = result.ExpenseCalculations.First();
            // (100 * 1.2) + 5 + (120 * 0.1) = 120 + 5 + 12 = 137
            Assert.Equal(137m, calculation.ConvertedValue);
        }

        [Fact]
        public async Task CalculateTripExpensesInTripCurrencyAsync_EstimatedExpense_UsesEstimatedValue()
        {
            // Arrange
            var tripId = 1;
            var tripCurrency = CurrencyCode.USD;
            var expenses = new List<Expense>
            {
                new()
                {
                    Id = 1,
                    Value = 100m,
                    EstimatedValue = 150m,
                    IsEstimated = true,
                    ExchangeRate = new ExchangeRate { CurrencyCodeKey = CurrencyCode.USD, ExchangeRateValue = 1.0m }, 
                    PaidById = "test", Name = "test"
                }
            };

            _expenseRepositoryMock
                .Setup(x => x.GetByTripIdWithParticipantsAsync(tripId))
                .ReturnsAsync(expenses);

            // Act
            var result = await _expenseService.CalculateTripExpensesInTripCurrencyAsync(tripId, tripCurrency);

            // Assert
            var calculation = result.ExpenseCalculations.First();
            Assert.Equal(150m, calculation.ConvertedValue); // Uses EstimatedValue, not Value
        }

        #endregion

        #region CalculateBalancesAsync Tests

        [Fact]
        public async Task CalculateBalancesAsync_WithSimpleExpense_CalculatesCorrectBalances()
        {
            // Arrange
            var tripId = 1;
            var trip = new Trip
            {
                Id = tripId,
                Name = "Test Trip",
                CurrencyCode = CurrencyCode.USD,
                PersonId = "owner"
            };

            var participants = new List<Person>
            {
                new() { Id = "user1", FirstName = "John", LastName = "Doe", Nationality = "Poland", IsPrivate = false },
                new() { Id = "user2", FirstName = "Jane", LastName = "Smith", Nationality = "Poland", IsPrivate = false }
            };

            var expenses = new List<Expense>
            {
                new()
                {
                    Id = 1,
                    Value = 100m,
                    IsEstimated = false,
                    PaidById = "user1",
                    Participants = new List<ExpenseParticipant>
                    {
                        new() { PersonId = "user1", Share = 0.5m, ActualShareValue = 50m },
                        new() { PersonId = "user2", Share = 0.5m, ActualShareValue = 50m }
                    },
                    ExchangeRate = new ExchangeRate { CurrencyCodeKey = CurrencyCode.USD, ExchangeRateValue = 1.0m },
                    Name = "test"
                }
            };

            _tripServiceMock
                .Setup(x => x.GetByIdAsync(tripId))
                .ReturnsAsync(trip);

            _tripServiceMock
                .Setup(x => x.GetAllTripParticipantsAsync(tripId))
                .ReturnsAsync(participants);

            _expenseRepositoryMock
                .Setup(x => x.GetByTripIdWithParticipantsAsync(tripId))
                .ReturnsAsync(expenses);

            // Act
            var result = await _expenseService.CalculateBalancesAsync(tripId);

            // Assert
            Assert.Equal(tripId, result.TripId);
            Assert.Equal(2, result.ParticipantBalances.Count);

            var johnBalance = result.ParticipantBalances.First(p => p.PersonId == "user1");
            var janeBalance = result.ParticipantBalances.First(p => p.PersonId == "user2");

            // John paid 100, his share is 50, so net +50
            Assert.Equal(50m, johnBalance.NetBalance);
            // Jane's share is 50, so net -50
            Assert.Equal(-50m, janeBalance.NetBalance);

            // Should have one debt: Jane owes John 50
            Assert.Single(result.DebtDetails);
            var debt = result.DebtDetails.First();
            Assert.Equal("user2", debt.FromPersonId);
            Assert.Equal("user1", debt.ToPersonId);
            Assert.Equal(50m, debt.Amount);
        }

        [Fact]
        public async Task CalculateBalancesAsync_WithTransfer_CalculatesCorrectBalances()
        {
            // Arrange
            var tripId = 1;
            var trip = new Trip { Id = tripId, CurrencyCode = CurrencyCode.USD, Name = "test", PersonId = "owner" };
            var participants = new List<Person>
            {
                new() { Id = "user1", FirstName = "John", LastName = "Doe", Nationality = "Poland", IsPrivate = false },
                new() { Id = "user2", FirstName = "Jane", LastName = "Smith", Nationality = "Poland", IsPrivate = false }
            };

            var expenses = new List<Expense>
            {
                new()
                {
                    Id = 1,
                    Value = 50m,
                    IsEstimated = false,
                    PaidById = "user1",
                    TransferredToId = "user2", // John transfers 50 to Jane
                    ExchangeRate = new ExchangeRate { CurrencyCodeKey = CurrencyCode.USD, ExchangeRateValue = 1.0m },
                    Name = "test"
                }
            };

            _tripServiceMock.Setup(x => x.GetByIdAsync(tripId)).ReturnsAsync(trip);
            _tripServiceMock.Setup(x => x.GetAllTripParticipantsAsync(tripId)).ReturnsAsync(participants);
            _expenseRepositoryMock.Setup(x => x.GetByTripIdWithParticipantsAsync(tripId)).ReturnsAsync(expenses);

            // Act
            var result = await _expenseService.CalculateBalancesAsync(tripId);

            // Assert
            var johnBalance = result.ParticipantBalances.First(p => p.PersonId == "user1");
            var janeBalance = result.ParticipantBalances.First(p => p.PersonId == "user2");

            // John paid transfer: +50 (he paid out money)
            Assert.Equal(50m, johnBalance.NetBalance);
            // Jane received transfer: -50 (she received money)
            Assert.Equal(-50m, janeBalance.NetBalance);
        }

        #endregion

        #region GetBudgetSummaryAsync Tests

        [Fact]
        public async Task GetBudgetSummaryAsync_WithValidFilter_ReturnsSummary()
        {
            // Arrange
            var filter = new BudgetFilterDto
            {
                TripId = 1,
                PersonId = "user1",
                CategoryId = 1,
                IncludeTransfers = true,
                IncludeEstimated = true
            };

            var trip = new Trip { Id = 1, Name = "Test Trip", CurrencyCode = CurrencyCode.USD, PersonId = "owner" };
            var expenses = new List<Expense>
            {
                new()
                {
                    Id = 1,
                    Value = 100m,
                    IsEstimated = false,
                    PaidById = "user1",
                    CategoryId = 1,
                    Participants = new List<ExpenseParticipant>
                    {
                        new() { PersonId = "user1", Share = 1.0m, ActualShareValue = 100m }
                    },
                    ExchangeRate = new ExchangeRate { CurrencyCodeKey = CurrencyCode.USD, ExchangeRateValue = 1.0m }, 
                    Name = "test"
                }
            };

            _tripServiceMock
                .Setup(x => x.GetByIdAsync(filter.TripId))
                .ReturnsAsync(trip);

            _expenseRepositoryMock
                .Setup(x => x.GetByTripIdWithParticipantsAsync(filter.TripId))
                .ReturnsAsync(expenses);

            // Act
            var result = await _expenseService.GetBudgetSummaryAsync(filter);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(filter.TripId, result.TripId);
            Assert.Single(result.CategorySummaries);

            var categorySummary = result.CategorySummaries.First();
            Assert.Equal(100m, categorySummary.ActualExpenses);
        }

        #endregion

        #region Helper Method Tests

        [Fact]
        public void CalculateFees_WithAdditionalFeeOnly_CalculatesCorrectly()
        {
            // Arrange
            var additionalFee = 10m;
            var percentageFee = 0m;
            var isEstimated = false;
            var amount = 100m;

            // Act
            var result = _expenseService.TestCalculateFees(additionalFee, percentageFee, isEstimated, amount);

            // Assert
            Assert.Equal(10m, result);
        }

        [Fact]
        public void CalculateFees_WithPercentageFeeOnly_CalculatesCorrectly()
        {
            // Arrange
            var additionalFee = 0m;
            var percentageFee = 10m; // 10%
            var isEstimated = false;
            var amount = 100m;

            // Act
            var result = _expenseService.TestCalculateFees(additionalFee, percentageFee, isEstimated, amount);

            // Assert
            Assert.Equal(10m, result); // 100 * 0.1 = 10
        }

        [Fact]
        public void CalculateFees_WithBothFees_CalculatesCorrectly()
        {
            // Arrange
            var additionalFee = 5m;
            var percentageFee = 10m;
            var isEstimated = false;
            var amount = 100m;

            // Act
            var result = _expenseService.TestCalculateFees(additionalFee, percentageFee, isEstimated, amount);

            // Assert
            Assert.Equal(15m, result); // 5 + (100 * 0.1) = 15
        }

        [Fact]
        public void CalculateFees_ForEstimatedExpense_ReturnsZero()
        {
            // Arrange
            var additionalFee = 10m;
            var percentageFee = 10m;
            var isEstimated = true;
            var amount = 100m;

            // Act
            var result = _expenseService.TestCalculateFees(additionalFee, percentageFee, isEstimated, amount);

            // Assert
            Assert.Equal(0m, result); // Fees don't apply to estimated expenses
        }

        #endregion
    }

    // Test helper extension to access private methods
    public static class ExpenseServiceTestExtensions
    {
        public static decimal TestCalculateFees(this ExpenseService service, decimal additionalFee,
            decimal percentageFee, bool isEstimated, decimal convertedAmount)
        {
            var method = typeof(ExpenseService).GetMethod("CalculateFees",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            return (decimal)method.Invoke(service, new object[] { additionalFee, percentageFee, isEstimated, convertedAmount });
        }
    }
}