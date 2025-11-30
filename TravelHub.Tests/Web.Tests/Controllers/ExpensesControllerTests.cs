using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Moq;
using System.Security.Claims;
using TravelHub.Domain.DTOs;
using TravelHub.Domain.Entities;
using TravelHub.Domain.Interfaces.Services;
using TravelHub.Web.Controllers;
using TravelHub.Web.ViewModels.Expenses;
using System.Reflection;

namespace TravelHub.Tests.Web.Tests.Controllers
{
    public class ExpensesControllerTests
    {
        private readonly Mock<IExpenseService> _expenseServiceMock;
        private readonly Mock<IExchangeRateService> _exchangeRateServiceMock;
        private readonly Mock<ICategoryService> _categoryServiceMock;
        private readonly Mock<ITripService> _tripServiceMock;
        private readonly Mock<ITripParticipantService> _tripParticipantServiceMock;
        private readonly Mock<ICurrencyConversionService> _currencyConversionServiceMock;
        private readonly Mock<ISpotService> _spotServiceMock;
        private readonly Mock<ITransportService> _transportServiceMock;
        private readonly Mock<UserManager<Person>> _userManagerMock;
        private readonly ExpensesController _controller;

        public ExpensesControllerTests()
        {
            _expenseServiceMock = new Mock<IExpenseService>();
            _exchangeRateServiceMock = new Mock<IExchangeRateService>();
            _categoryServiceMock = new Mock<ICategoryService>();
            _tripServiceMock = new Mock<ITripService>();
            _tripParticipantServiceMock = new Mock<ITripParticipantService>();
            _currencyConversionServiceMock = new Mock<ICurrencyConversionService>();
            _spotServiceMock = new Mock<ISpotService>();
            _transportServiceMock = new Mock<ITransportService>();

            var store = new Mock<IUserStore<Person>>();
            _userManagerMock = new Mock<UserManager<Person>>(store.Object, null, null, null, null, null, null, null, null);

            _controller = new ExpensesController(
                _expenseServiceMock.Object,
                _exchangeRateServiceMock.Object,
                _categoryServiceMock.Object,
                _userManagerMock.Object,
                _tripServiceMock.Object,
                _tripParticipantServiceMock.Object,
                _currencyConversionServiceMock.Object,
                _spotServiceMock.Object,
                _transportServiceMock.Object
            );

            // Mock TempData
            var tempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>());
            _controller.TempData = tempData;
        }

        private void SetupListsMocks(int tripId = 1)
        {
            _exchangeRateServiceMock
                .Setup(x => x.GetTripExchangeRatesAsync(tripId))
                .ReturnsAsync(new List<ExchangeRate>());

            _categoryServiceMock
                .Setup(x => x.GetAllCategoriesByTripAsync(tripId))
                .ReturnsAsync(new List<Category>
                {
                    new() { Id = 1, Name = "Food", Color = "", PersonId = "test" },
                    new() {Id = 2, Name = "Transport", Color = "", PersonId = "test"}
                });

            _tripServiceMock
                .Setup(x => x.GetAllTripParticipantsAsync(tripId))
                .ReturnsAsync(new List<Person>());

            _spotServiceMock
                .Setup(x => x.GetSpotsByTripAsync(tripId))
                .ReturnsAsync(new List<Spot>());

            _transportServiceMock
                .Setup(x => x.GetTripTransportsWithDetailsAsync(tripId))
                .ReturnsAsync(new List<Transport>());
        }

        private void SetupAuthenticatedUser(string userId = "test-user")
        {
            var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Name, "test@example.com")
            }, "TestAuthentication"));

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            };

            _userManagerMock
                .Setup(x => x.GetUserId(It.IsAny<ClaimsPrincipal>()))
                .Returns(userId);
        }

        #region Index Tests

        [Fact]
        public async Task Index_ReturnsViewWithExpenses()
        {
            // Arrange
            SetupAuthenticatedUser();
            var expenses = new List<Expense>
            {
                new() { Id = 1, Name = "Expense 1", Value = 100m, PaidBy = new Person { FirstName = "John", LastName = "Doe", IsPrivate = false, Nationality="Poland" }, PaidById = "test" },
                new() { Id = 2, Name = "Expense 2", Value = 200m, PaidBy = new Person { FirstName = "Jane", LastName = "Smith", IsPrivate = false, Nationality="Poland" }, PaidById = "test" }
            };

            _expenseServiceMock
                .Setup(x => x.GetAllAsync())
                .ReturnsAsync(expenses);

            // Act
            var result = await _controller.Index();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<IEnumerable<ExpenseViewModel>>(viewResult.Model);
            Assert.Equal(2, model.Count());
        }

        #endregion

        #region Details Tests

        [Fact]
        public async Task Details_WithNullId_ReturnsNotFound()
        {
            // Arrange
            SetupAuthenticatedUser();

            // Act
            var result = await _controller.Details(null);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Details_WithNonExistingId_ReturnsNotFound()
        {
            // Arrange
            SetupAuthenticatedUser();
            var expenseId = 999;

            _expenseServiceMock
                .Setup(x => x.GetByIdAsync(expenseId))
                .ReturnsAsync((Expense)null);

            // Act
            var result = await _controller.Details(expenseId);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Details_WithValidId_ReturnsView()
        {
            // Arrange
            var userId = "test-user";
            SetupAuthenticatedUser(userId);
            var expenseId = 1;
            var expense = new Expense
            {
                Id = expenseId,
                Name = "Test Expense",
                Value = 100m,
                TripId = 1,
                PaidBy = new Person { FirstName = "John", LastName = "Doe", IsPrivate = false, Nationality = "Poland" },
                ExchangeRate = new ExchangeRate { CurrencyCodeKey = CurrencyCode.USD, ExchangeRateValue = 1.0m },
                Trip = new Trip { CurrencyCode = CurrencyCode.USD, Name = "Test Trip", PersonId = "test" },
                Participants = new List<ExpenseParticipant>
                {
                    new() { Person = new Person { FirstName = "John", LastName = "Doe", IsPrivate = false, Nationality="Poland" }, ActualShareValue = 50m, Share = 0.5m, PersonId = "test" },
                    new() { Person = new Person { FirstName = "Jane", LastName = "Smith", IsPrivate = false, Nationality="Poland" }, ActualShareValue = 50m, Share = 0.5m, PersonId = "test" }
                }, PaidById = "test"
            };

            _expenseServiceMock
                .Setup(x => x.GetByIdAsync(expenseId))
                .ReturnsAsync(expense);

            _tripParticipantServiceMock
                .Setup(x => x.UserHasAccessToTripAsync(expense.TripId, userId))
                .ReturnsAsync(true);

            // Act
            var result = await _controller.Details(expenseId);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<ExpenseDetailsViewModel>(viewResult.Model);
            Assert.Equal(expenseId, model.Id);
            Assert.Equal(2, model.Participants.Count);
        }

        [Fact]
        public async Task Details_WithoutAccess_ReturnsForbid()
        {
            // Arrange
            var userId = "test-user";
            SetupAuthenticatedUser(userId);
            var expenseId = 1;
            var expense = new Expense { Id = expenseId, TripId = 1, Name = "test", PaidById = "test" };

            _expenseServiceMock
                .Setup(x => x.GetByIdAsync(expenseId))
                .ReturnsAsync(expense);

            _tripParticipantServiceMock
                .Setup(x => x.UserHasAccessToTripAsync(expense.TripId, userId))
                .ReturnsAsync(false);

            // Act
            var result = await _controller.Details(expenseId);

            // Assert
            Assert.IsType<ForbidResult>(result);
        }

        #endregion

        #region Create Tests

        [Fact]
        public async Task Create_Get_ReturnsViewWithPopulatedSelectLists()
        {
            // Arrange
            SetupAuthenticatedUser();

            SetupListsMocks();

            // Act
            var result = await _controller.Create();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<ExpenseCreateEditViewModel>(viewResult.Model);
            Assert.NotNull(model.CurrenciesGroups);
            Assert.NotNull(model.Categories);
            Assert.NotNull(model.People);
        }

        [Fact]
        public async Task Create_Post_WithValidModel_CreatesExpense()
        {
            // Arrange
            SetupAuthenticatedUser();
            var viewModel = new ExpenseCreateEditViewModel
            {
                Name = "Test Expense",
                Value = 100m,
                PaidById = "user1",
                TripId = 1,
                SelectedCurrencyCode = CurrencyCode.USD,
                ExchangeRateValue = 1.0m,
                ParticipantsShares = new List<ParticipantShareViewModel>
                {
                    new() { PersonId = "user1", ShareType = 1, ActualShareValue = 100m, FullName = "user1" }
                }
            };

            var exchangeRate = new ExchangeRate { Id = 1, CurrencyCodeKey = CurrencyCode.USD };

            _exchangeRateServiceMock
                .Setup(x => x.GetOrCreateExchangeRateAsync(viewModel.TripId, viewModel.SelectedCurrencyCode, viewModel.ExchangeRateValue))
                .ReturnsAsync(exchangeRate);

            _expenseServiceMock
                .Setup(x => x.AddAsync(It.IsAny<Expense>(), It.IsAny<IEnumerable<ParticipantShareDto>>()))
                .ReturnsAsync(new Expense { Id = 1, Name = "test", PaidById = "test" });

            // Act
            var result = await _controller.Create(viewModel);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);
            Assert.Equal("Expense created successfully!", _controller.TempData["SuccessMessage"]);
        }

        [Fact]
        public async Task Create_Post_WithTransfer_CreatesTransfer()
        {
            // Arrange
            SetupAuthenticatedUser();
            var viewModel = new ExpenseCreateEditViewModel
            {
                Name = "Transfer",
                Value = 50m,
                PaidById = "user1",
                TransferredToId = "user2",
                TripId = 1,
                SelectedCurrencyCode = CurrencyCode.USD,
                ExchangeRateValue = 1.0m
            };

            var exchangeRate = new ExchangeRate { Id = 1, CurrencyCodeKey = CurrencyCode.USD };

            _exchangeRateServiceMock
                .Setup(x => x.GetOrCreateExchangeRateAsync(viewModel.TripId, viewModel.SelectedCurrencyCode, viewModel.ExchangeRateValue))
                .ReturnsAsync(exchangeRate);

            _expenseServiceMock
                .Setup(x => x.AddAsync(It.IsAny<Expense>(), It.IsAny<IEnumerable<ParticipantShareDto>>()))
                .ReturnsAsync(new Expense {Id = 1, Name = "test", PaidById = "test" });

            // Act
            var result = await _controller.Create(viewModel);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Transfer created successfully!", _controller.TempData["SuccessMessage"]);
        }

        [Fact]
        public async Task Create_Post_WithInvalidModel_ReturnsView()
        {
            // Arrange
            SetupAuthenticatedUser();
            var viewModel = new ExpenseCreateEditViewModel();
            _controller.ModelState.AddModelError("Name", "Required");

            SetupListsMocks(viewModel.TripId);

            // Act
            var result = await _controller.Create(viewModel);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.IsType<ExpenseCreateEditViewModel>(viewResult.Model);
        }

        #endregion

        #region Edit Tests

        [Fact]
        public async Task Edit_Get_WithNullId_ReturnsNotFound()
        {
            // Arrange
            SetupAuthenticatedUser();

            // Act
            var result = await _controller.Edit(null);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Edit_Get_WithNonExistingId_ReturnsNotFound()
        {
            // Arrange
            SetupAuthenticatedUser();
            var expenseId = 999;

            _expenseServiceMock
                .Setup(x => x.GetByIdAsync(expenseId))
                .ReturnsAsync((Expense)null);

            // Act
            var result = await _controller.Edit(expenseId);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Edit_Get_WithoutAccess_ReturnsForbid()
        {
            // Arrange
            var userId = "test-user";
            SetupAuthenticatedUser(userId);
            var expenseId = 1;
            var expense = new Expense {Id = expenseId, TripId = 1, Name = "test", PaidById = "test" };

            _expenseServiceMock
                .Setup(x => x.GetByIdAsync(expenseId))
                .ReturnsAsync(expense);

            _tripParticipantServiceMock
                .Setup(x => x.UserHasAccessToTripAsync(expense.TripId, userId))
                .ReturnsAsync(false);

            // Act
            var result = await _controller.Edit(expenseId);

            // Assert
            Assert.IsType<ForbidResult>(result);
        }

        [Fact]
        public async Task Edit_Get_WithTransfer_ReturnsEditTransferView()
        {
            // Arrange
            var userId = "test";
            SetupAuthenticatedUser(userId);
            var expenseId = 1;
            var expense = new Expense
            {
                Id = expenseId,
                TripId = 1,
                TransferredToId = "user2",
                Trip = new Trip { CurrencyCode = CurrencyCode.USD, Name = "test", PersonId = "test" },
                Name = "Transfer",
                PaidById = "test"
            };

            _expenseServiceMock
                .Setup(x => x.GetByIdAsync(expenseId))
                .ReturnsAsync(expense);

            _tripParticipantServiceMock
                .Setup(x => x.UserHasAccessToTripAsync(expense.TripId, userId))
                .ReturnsAsync(true);

            SetupListsMocks(expense.TripId);

            // Act
            var result = await _controller.Edit(expenseId);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal("EditTransfer", viewResult.ViewName);
        }

        [Fact]
        public async Task Edit_Post_WithValidModel_UpdatesExpense()
        {
            // Arrange
            var userId = "test-user";
            SetupAuthenticatedUser(userId);
            var expenseId = 1;
            var viewModel = new ExpenseCreateEditViewModel
            {
                Id = expenseId,
                Name = "Updated Expense",
                Value = 150m,
                PaidById = "user1",
                TripId = 1,
                SelectedCurrencyCode = CurrencyCode.USD,
                ExchangeRateValue = 1.0m,
                ParticipantsShares = new List<ParticipantShareViewModel>
                {
                    new() { PersonId = "user1", ShareType = 1, ActualShareValue = 150m, FullName = "user1" }
                }
            };

            var existingExpense = new Expense { Id = expenseId, TripId = 1, Name = "test", PaidById = "test" };
            var exchangeRate = new ExchangeRate { Id = 1, CurrencyCodeKey = CurrencyCode.USD };

            _tripParticipantServiceMock
                .Setup(x => x.UserHasAccessToTripAsync(viewModel.TripId, userId))
                .ReturnsAsync(true);

            _expenseServiceMock
                .Setup(x => x.GetByIdAsync(expenseId))
                .ReturnsAsync(existingExpense);

            _exchangeRateServiceMock
                .Setup(x => x.GetOrCreateExchangeRateAsync(viewModel.TripId, viewModel.SelectedCurrencyCode, viewModel.ExchangeRateValue))
                .ReturnsAsync(exchangeRate);

            _expenseServiceMock
                .Setup(x => x.UpdateAsync(It.IsAny<Expense>(), It.IsAny<IEnumerable<ParticipantShareDto>>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.Edit(expenseId, viewModel);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Details", redirectResult.ActionName);
            Assert.Equal("Trips", redirectResult.ControllerName);
            Assert.Equal("Expense updated successfully!", _controller.TempData["SuccessMessage"]);
        }

        [Fact]
        public async Task Edit_Post_WithTransferToSamePerson_ReturnsViewWithError()
        {
            // Arrange
            var userId = "user1";
            SetupAuthenticatedUser(userId);
            var expenseId = 1;
            var viewModel = new ExpenseCreateEditViewModel
            {
                Id = expenseId,
                Name = "Transfer",
                Value = 50m,
                PaidById = "user1",
                TransferredToId = "user1", // Same person
                TripId = 1,
                SelectedCurrencyCode = CurrencyCode.USD,
                ExchangeRateValue = 1.0m
            };

            _tripParticipantServiceMock
                .Setup(x => x.UserHasAccessToTripAsync(viewModel.TripId, userId))
                .ReturnsAsync(true);

            SetupListsMocks(viewModel.TripId);

            // Act
            var result = await _controller.Edit(expenseId, viewModel);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.True(_controller.ModelState.ContainsKey("TransferredToId"));
            Assert.Equal("EditTransfer", viewResult.ViewName);
        }

        [Fact]
        public async Task Edit_Post_WithIdMismatch_ReturnsNotFound()
        {
            // Arrange
            SetupAuthenticatedUser();
            var viewModel = new ExpenseCreateEditViewModel { Id = 1 };

            // Act
            var result = await _controller.Edit(2, viewModel);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        #endregion

        #region Delete Tests

        [Fact]
        public async Task Delete_Get_WithNullId_ReturnsNotFound()
        {
            // Arrange
            SetupAuthenticatedUser();

            // Act
            var result = await _controller.Delete(null);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Delete_Get_WithNonExistingId_ReturnsNotFound()
        {
            // Arrange
            SetupAuthenticatedUser();
            var expenseId = 999;

            _expenseServiceMock
                .Setup(x => x.GetByIdAsync(expenseId))
                .ReturnsAsync((Expense)null);

            // Act
            var result = await _controller.Delete(expenseId);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Delete_Get_WithoutAccess_ReturnsForbid()
        {
            // Arrange
            var userId = "test-user";
            SetupAuthenticatedUser(userId);
            var expenseId = 1;
            var expense = new Expense { Id = expenseId, TripId = 1, Name = "test", PaidById = "test" };

            _expenseServiceMock
                .Setup(x => x.GetByIdAsync(expenseId))
                .ReturnsAsync(expense);

            _tripParticipantServiceMock
                .Setup(x => x.UserHasAccessToTripAsync(expense.TripId, userId))
                .ReturnsAsync(false);

            // Act
            var result = await _controller.Delete(expenseId);

            // Assert
            Assert.IsType<ForbidResult>(result);
        }

        [Fact]
        public async Task Delete_Post_WithValidId_DeletesExpense()
        {
            // Arrange
            var userId = "test-user";
            SetupAuthenticatedUser(userId);
            var expenseId = 1;
            var expense = new Expense {Id = expenseId, TripId = 1, Name = "test", PaidById = "test" };

            _expenseServiceMock
                .Setup(x => x.GetByIdAsync(expenseId))
                .ReturnsAsync(expense);

            _tripParticipantServiceMock
                .Setup(x => x.UserHasAccessToTripAsync(expense.TripId, userId))
                .ReturnsAsync(true);

            _expenseServiceMock
                .Setup(x => x.DeleteAsync(expenseId))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.DeleteConfirmed(expenseId);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Details", redirectResult.ActionName);
            Assert.Equal("Trips", redirectResult.ControllerName);
        }

        #endregion

        #region AddToTrip Tests

        [Fact]
        public async Task AddToTrip_Get_WithNonExistingTrip_ReturnsNotFound()
        {
            // Arrange
            SetupAuthenticatedUser();
            var tripId = 999;

            _tripServiceMock
                .Setup(x => x.GetByIdAsync(tripId))
                .ReturnsAsync((Trip)null);

            // Act
            var result = await _controller.AddToTrip(tripId);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task AddToTrip_Get_WithoutAccess_ReturnsForbid()
        {
            // Arrange
            var userId = "test-user";
            SetupAuthenticatedUser(userId);
            var tripId = 1;
            var trip = new Trip {Id = tripId, Name = "test", PersonId = "test" };

            _tripServiceMock
                .Setup(x => x.GetByIdAsync(tripId))
                .ReturnsAsync(trip);

            _tripParticipantServiceMock
                .Setup(x => x.UserHasAccessToTripAsync(tripId, userId))
                .ReturnsAsync(false);

            // Act
            var result = await _controller.AddToTrip(tripId);

            // Assert
            Assert.IsType<ForbidResult>(result);
        }

        [Fact]
        public async Task AddToTrip_Post_WithEstimatedExpense_CreatesEstimatedExpense()
        {
            // Arrange
            var userId = "test-user";
            SetupAuthenticatedUser(userId);
            var viewModel = new ExpenseCreateEditViewModel
            {
                Name = "Estimated Expense",
                EstimatedValue = 200m,
                PaidById = "user1",
                TripId = 1,
                SelectedCurrencyCode = CurrencyCode.USD,
                ExchangeRateValue = 1.0m,
                IsEstimated = true,
                Multiplier = 2
            };

            _tripParticipantServiceMock
                .Setup(x => x.UserHasAccessToTripAsync(viewModel.TripId, userId))
                .ReturnsAsync(true);

            var exchangeRate = new ExchangeRate { Id = 1, CurrencyCodeKey = CurrencyCode.USD };

            _exchangeRateServiceMock
                .Setup(x => x.GetOrCreateExchangeRateAsync(viewModel.TripId, viewModel.SelectedCurrencyCode, viewModel.ExchangeRateValue))
                .ReturnsAsync(exchangeRate);

            _expenseServiceMock
                .Setup(x => x.AddAsync(It.IsAny<Expense>(), It.IsAny<IEnumerable<ParticipantShareDto>>()))
                .ReturnsAsync(new Expense { Id = 1, Name = "test", PaidById = "test" });

            // Act
            var result = await _controller.AddToTrip(viewModel);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Estimated expense created successfully!", _controller.TempData["SuccessMessage"]);
        }

        #endregion

        #region AddTransferToTrip Tests

        [Fact]
        public async Task AddTransferToTrip_Post_WithValidModel_CreatesTransfer()
        {
            // Arrange
            var userId = "test-user";
            SetupAuthenticatedUser(userId);
            var viewModel = new ExpenseCreateEditViewModel
            {
                Name = "Transfer",
                Value = 50m,
                PaidById = "user1",
                TransferredToId = "user2",
                TripId = 1,
                SelectedCurrencyCode = CurrencyCode.USD,
                ExchangeRateValue = 1.0m
            };

            _tripParticipantServiceMock
                .Setup(x => x.UserHasAccessToTripAsync(viewModel.TripId, userId))
                .ReturnsAsync(true);

            var exchangeRate = new ExchangeRate { Id = 1, CurrencyCodeKey = CurrencyCode.USD };

            _exchangeRateServiceMock
                .Setup(x => x.GetOrCreateExchangeRateAsync(viewModel.TripId, viewModel.SelectedCurrencyCode, viewModel.ExchangeRateValue))
                .ReturnsAsync(exchangeRate);

            _expenseServiceMock
                .Setup(x => x.AddAsync(It.IsAny<Expense>(), It.IsAny<IEnumerable<ParticipantShareDto>>()))
                .ReturnsAsync(new Expense { Id = 1, Name = "test", PaidById = "test" });

            // Act
            var result = await _controller.AddTransferToTrip(viewModel);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Transfer added successfully!", _controller.TempData["SuccessMessage"]);
        }

        [Fact]
        public async Task AddTransferToTrip_Post_WithSamePerson_ReturnsViewWithError()
        {
            // Arrange
            var userId = "test-user";
            SetupAuthenticatedUser(userId);
            var viewModel = new ExpenseCreateEditViewModel
            {
                Name = "Transfer",
                Value = 50m,
                PaidById = "user1",
                TransferredToId = "user1", // Same person
                TripId = 1,
                SelectedCurrencyCode = CurrencyCode.USD,
                ExchangeRateValue = 1.0m
            };

            _tripParticipantServiceMock
                .Setup(x => x.UserHasAccessToTripAsync(viewModel.TripId, userId))
                .ReturnsAsync(true);

            SetupListsMocks(viewModel.TripId);

            // Act
            var result = await _controller.AddTransferToTrip(viewModel);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.True(_controller.ModelState.ContainsKey("TransferredToId"));
            Assert.Equal("AddTransferToTrip", viewResult.ViewName);
        }

        #endregion

        #region Balances Tests

        [Fact]
        public async Task Balances_WithNonExistingTrip_ReturnsNotFound()
        {
            // Arrange
            SetupAuthenticatedUser();
            var tripId = 999;

            _tripServiceMock
                .Setup(x => x.GetByIdAsync(tripId))
                .ReturnsAsync((Trip)null);

            // Act
            var result = await _controller.Balances(tripId);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Balances_WithoutAccess_ReturnsForbid()
        {
            // Arrange
            var userId = "test-user";
            SetupAuthenticatedUser(userId);
            var tripId = 1;
            var trip = new Trip {Id = tripId, Name = "test", PersonId = "test" };

            _tripServiceMock
                .Setup(x => x.GetByIdAsync(tripId))
                .ReturnsAsync(trip);

            _tripParticipantServiceMock
                .Setup(x => x.UserHasAccessToTripAsync(tripId, userId))
                .ReturnsAsync(false);

            // Act
            var result = await _controller.Balances(tripId);

            // Assert
            Assert.IsType<ForbidResult>(result);
        }

        [Fact]
        public async Task Balances_WithValidTrip_ReturnsViewWithBalances()
        {
            // Arrange
            var userId = "test-user";
            SetupAuthenticatedUser(userId);
            var tripId = 1;
            var trip = new Trip { Id = tripId, Name = "Test Trip", PersonId = "test" };
            var balanceDto = new BalanceDto
            {
                TripId = tripId,
                TripName = "Test Trip",
                TripCurrency = CurrencyCode.USD,
                ParticipantBalances = new List<ParticipantBalanceDto>(),
                DebtDetails = new List<DebtDetailDto>()
            };

            _tripServiceMock
                .Setup(x => x.GetByIdAsync(tripId))
                .ReturnsAsync(trip);

            _tripParticipantServiceMock
                .Setup(x => x.UserHasAccessToTripAsync(tripId, userId))
                .ReturnsAsync(true);

            _expenseServiceMock
                .Setup(x => x.CalculateBalancesAsync(tripId))
                .ReturnsAsync(balanceDto);

            // Act
            var result = await _controller.Balances(tripId);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<BalanceViewModel>(viewResult.Model);
            Assert.Equal(tripId, model.TripId);
        }

        #endregion

        #region ExchangeRate Tests

        [Fact]
        public async Task ExchangeRate_WithValidCurrencies_ReturnsRate()
        {
            // Arrange
            var from = "USD";
            var to = "EUR";
            var expectedRate = 0.85m;

            _currencyConversionServiceMock
                .Setup(x => x.GetExchangeRate(from, to))
                .ReturnsAsync(expectedRate);

            // Act
            var result = await _controller.ExchangeRate(from, to);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(expectedRate, okResult.Value);
        }

        [Fact]
        public async Task ExchangeRate_WithNullParameters_ReturnsBadRequest()
        {
            // Act
            var result = await _controller.ExchangeRate(null, "EUR");

            // Assert
            Assert.IsType<BadRequestResult>(result);
        }

        [Fact]
        public async Task ExchangeRate_WithHttpRequestException_ReturnsBadRequest()
        {
            // Arrange
            var from = "USD";
            var to = "EUR";

            _currencyConversionServiceMock
                .Setup(x => x.GetExchangeRate(from, to))
                .ThrowsAsync(new HttpRequestException());

            // Act
            var result = await _controller.ExchangeRate(from, to);

            // Assert
            Assert.IsType<BadRequestResult>(result);
        }

        #endregion

        #region Helper Methods Tests

        [Fact]
        public async Task ExpenseExists_WithExistingId_ReturnsTrue()
        {
            // Arrange
            var expenseId = 1;
            var expense = new Expense { Id = expenseId, Name = "test", PaidById = "test" };

            _expenseServiceMock
                .Setup(x => x.GetByIdAsync(expenseId))
                .ReturnsAsync(expense);

            // Act
            var result = await _controller.TestExpenseExists(expenseId);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task ExpenseExists_WithNonExistingId_ReturnsFalse()
        {
            // Arrange
            var expenseId = 999;

            _expenseServiceMock
                .Setup(x => x.GetByIdAsync(expenseId))
                .ReturnsAsync((Expense)null);

            // Act
            var result = await _controller.TestExpenseExists(expenseId);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void GetCurrentUserId_WithAuthenticatedUser_ReturnsUserId()
        {
            // Arrange
            var expectedUserId = "test-user";
            SetupAuthenticatedUser(expectedUserId);

            // Act
            var result = _controller.TestGetCurrentUserId();

            // Assert
            Assert.Equal(expectedUserId, result);
        }

        [Fact]
        public void GetCurrentUserId_WithoutAuthenticatedUser_ThrowsException()
        {
            // Arrange
            var user = new ClaimsPrincipal(new ClaimsIdentity()); // No authentication
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            };

            _userManagerMock
                .Setup(x => x.GetUserId(It.IsAny<ClaimsPrincipal>()))
                .Returns((string)null);

            // Act & Assert
            Assert.Throws<TargetInvocationException>(() => _controller.TestGetCurrentUserId());
        }

        #endregion
    }

    // Test helper extension to access private methods
    public static class ExpensesControllerTestExtensions
    {
        public static async Task<bool> TestExpenseExists(this ExpensesController controller, int id)
        {
            var method = typeof(ExpensesController).GetMethod("ExpenseExists",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            return await (Task<bool>)method.Invoke(controller, new object[] { id });
        }

        public static string TestGetCurrentUserId(this ExpensesController controller)
        {
            var method = typeof(ExpensesController).GetMethod("GetCurrentUserId",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            return (string)method.Invoke(controller, null);
        }
    }
}