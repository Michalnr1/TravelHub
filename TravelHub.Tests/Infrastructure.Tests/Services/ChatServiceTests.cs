using Moq;
using TravelHub.Domain.Entities;
using TravelHub.Domain.Interfaces.Repositories;
using TravelHub.Domain.Interfaces.Services;
using TravelHub.Domain.DTOs;
using TravelHub.Infrastructure.Services;

namespace TravelHub.Tests.Infrastructure.Tests.Services
{
    public class ChatServiceTests
    {
        private readonly Mock<IChatRepository> _chatRepositoryMock;
        private readonly Mock<ITripRepository> _tripRepositoryMock;
        private readonly Mock<IGenericRepository<Person>> _personRepositoryMock;
        private readonly ChatService _chatService;
        private readonly ChatService _chatServiceWithoutPersonRepo;

        public ChatServiceTests()
        {
            _chatRepositoryMock = new Mock<IChatRepository>();
            _tripRepositoryMock = new Mock<ITripRepository>();
            _personRepositoryMock = new Mock<IGenericRepository<Person>>();

            _chatService = new ChatService(
                _chatRepositoryMock.Object,
                _tripRepositoryMock.Object,
                _personRepositoryMock.Object
            );

            _chatServiceWithoutPersonRepo = new ChatService(
                _chatRepositoryMock.Object,
                _tripRepositoryMock.Object
            );
        }

        #region GetMessagesForTripAsync Tests

        [Fact]
        public async Task GetMessagesForTripAsync_WithValidTripId_ReturnsMessages()
        {
            // Arrange
            var tripId = 1;
            var expectedMessages = new List<ChatMessage>
            {
                new() { Id = 1, Message = "Hello", TripId = tripId, PersonId = "user1" },
                new() { Id = 2, Message = "Hi there", TripId = tripId, PersonId = "user2" }
            };

            _chatRepositoryMock
                .Setup(x => x.GetByTripIdAsync(tripId))
                .ReturnsAsync(expectedMessages);

            // Act
            var result = await _chatService.GetMessagesForTripAsync(tripId);

            // Assert
            Assert.Equal(expectedMessages, result);
            _chatRepositoryMock.Verify(x => x.GetByTripIdAsync(tripId), Times.Once);
        }

        [Fact]
        public async Task GetMessagesForTripAsync_WithNonExistingTrip_ReturnsEmptyList()
        {
            // Arrange
            var tripId = 999;

            _chatRepositoryMock
                .Setup(x => x.GetByTripIdAsync(tripId))
                .ReturnsAsync(new List<ChatMessage>());

            // Act
            var result = await _chatService.GetMessagesForTripAsync(tripId);

            // Assert
            Assert.Empty(result);
        }

        #endregion

        #region CreateMessageAsync Tests

        [Fact]
        public async Task CreateMessageAsync_WithValidData_CreatesMessage()
        {
            // Arrange
            var tripId = 1;
            var dto = new ChatMessageCreateDto
            {
                Message = "Test message",
                PersonId = "user1"
            };
            var currentPersonId = "user1";
            var trip = new Trip { Id = tripId, Name = "Test Trip", PersonId = "test" };
            var person = new Person { Id = "user1", FirstName = "John", LastName = "Doe", IsPrivate = false, Nationality = "poland" };
            var chatMessage = new ChatMessage
            {
                Id = 1,
                Message = "Test message",
                TripId = tripId,
                PersonId = "user1"
            };

            _tripRepositoryMock
                .Setup(x => x.GetByIdAsync(tripId))
                .ReturnsAsync(trip);

            _personRepositoryMock
                .Setup(x => x.GetByIdAsync(currentPersonId))
                .ReturnsAsync(person);

            _chatRepositoryMock
                .Setup(x => x.AddAsync(It.IsAny<ChatMessage>()))
                .ReturnsAsync(chatMessage);

            _chatRepositoryMock
                .Setup(x => x.GetByIdWithPersonAsync(chatMessage.Id))
                .ReturnsAsync(chatMessage);

            // Act
            var result = await _chatService.CreateMessageAsync(tripId, dto, currentPersonId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Test message", result.Message);
            Assert.Equal(tripId, result.TripId);
            Assert.Equal("user1", result.PersonId);

            _tripRepositoryMock.Verify(x => x.GetByIdAsync(tripId), Times.Once);
            _personRepositoryMock.Verify(x => x.GetByIdAsync(currentPersonId), Times.Once);
            _chatRepositoryMock.Verify(x => x.AddAsync(It.IsAny<ChatMessage>()), Times.Once);
        }

        [Fact]
        public async Task CreateMessageAsync_WithoutPersonRepository_CreatesMessage()
        {
            // Arrange
            var tripId = 1;
            var dto = new ChatMessageCreateDto
            {
                Message = "Test message",
                PersonId = "user1"
            };
            var trip = new Trip { Id = tripId, Name = "Test Trip", PersonId = "test" };
            var chatMessage = new ChatMessage
            {
                Id = 1,
                Message = "Test message",
                TripId = tripId,
                PersonId = "user1"
            };

            _tripRepositoryMock
                .Setup(x => x.GetByIdAsync(tripId))
                .ReturnsAsync(trip);

            _chatRepositoryMock
                .Setup(x => x.AddAsync(It.IsAny<ChatMessage>()))
                .ReturnsAsync(chatMessage);

            _chatRepositoryMock
                .Setup(x => x.GetByIdWithPersonAsync(chatMessage.Id))
                .ReturnsAsync(chatMessage);

            // Act
            var result = await _chatServiceWithoutPersonRepo.CreateMessageAsync(tripId, dto, "user1");

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Test message", result.Message);

            // Person repository should not be used when not provided
            _personRepositoryMock.Verify(x => x.GetByIdAsync(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task CreateMessageAsync_WithEmptyMessage_ThrowsArgumentException()
        {
            // Arrange
            var tripId = 1;
            var dto = new ChatMessageCreateDto { Message = "   " };
            var currentPersonId = "user1";

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() =>
                _chatService.CreateMessageAsync(tripId, dto, currentPersonId));
        }

        [Fact]
        public async Task CreateMessageAsync_WithNullMessage_ThrowsArgumentException()
        {
            // Arrange
            var tripId = 1;
            var dto = new ChatMessageCreateDto { Message = null };
            var currentPersonId = "user1";

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() =>
                _chatService.CreateMessageAsync(tripId, dto, currentPersonId));
        }

        [Fact]
        public async Task CreateMessageAsync_WithNonExistingTrip_ThrowsKeyNotFoundException()
        {
            // Arrange
            var tripId = 999;
            var dto = new ChatMessageCreateDto { Message = "Test message", PersonId = "user1" };
            var currentPersonId = "user1";

            _tripRepositoryMock
                .Setup(x => x.GetByIdAsync(tripId))
                .ReturnsAsync((Trip)null);

            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                _chatService.CreateMessageAsync(tripId, dto, currentPersonId));
        }

        [Fact]
        public async Task CreateMessageAsync_WithoutPersonId_ThrowsInvalidOperationException()
        {
            // Arrange
            var tripId = 1;
            var dto = new ChatMessageCreateDto { Message = "Test message" }; // No PersonId
            string currentPersonId = null; // No current person ID
            var trip = new Trip { Id = tripId, Name = "Test Trip", PersonId = "test" };

            _tripRepositoryMock
                .Setup(x => x.GetByIdAsync(tripId))
                .ReturnsAsync(trip);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _chatService.CreateMessageAsync(tripId, dto, currentPersonId));
        }

        [Fact]
        public async Task CreateMessageAsync_WithNonExistingPerson_ThrowsKeyNotFoundException()
        {
            // Arrange
            var tripId = 1;
            var dto = new ChatMessageCreateDto { Message = "Test message", PersonId = "user1" };
            var currentPersonId = "non-existing-user";
            var trip = new Trip { Id = tripId, Name = "Test Trip", PersonId = "test" };

            _tripRepositoryMock
                .Setup(x => x.GetByIdAsync(tripId))
                .ReturnsAsync(trip);

            _personRepositoryMock
                .Setup(x => x.GetByIdAsync(currentPersonId))
                .ReturnsAsync((Person)null);

            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                _chatService.CreateMessageAsync(tripId, dto, currentPersonId));
        }

        [Fact]
        public async Task CreateMessageAsync_WithCurrentPersonId_OverridesDtoPersonId()
        {
            // Arrange
            var tripId = 1;
            var dto = new ChatMessageCreateDto
            {
                Message = "Test message",
                PersonId = "user-from-dto" // This should be overridden
            };
            var currentPersonId = "user-from-context"; // This should be used
            var trip = new Trip { Id = tripId, Name = "Test Trip" , PersonId = "test" };
            var person = new Person {Id = currentPersonId, FirstName = "John", LastName = "Doe", IsPrivate = false, Nationality = "poland" };
            var chatMessage = new ChatMessage
            {
                Id = 1,
                Message = "Test message",
                TripId = tripId,
                PersonId = currentPersonId
            };

            _tripRepositoryMock
                .Setup(x => x.GetByIdAsync(tripId))
                .ReturnsAsync(trip);

            _personRepositoryMock
                .Setup(x => x.GetByIdAsync(currentPersonId))
                .ReturnsAsync(person);

            _chatRepositoryMock
                .Setup(x => x.AddAsync(It.IsAny<ChatMessage>()))
                .ReturnsAsync(chatMessage);

            _chatRepositoryMock
                .Setup(x => x.GetByIdWithPersonAsync(chatMessage.Id))
                .ReturnsAsync(chatMessage);

            // Act
            var result = await _chatService.CreateMessageAsync(tripId, dto, currentPersonId);

            // Assert
            Assert.Equal(currentPersonId, result.PersonId); // Should use currentPersonId, not dto.PersonId
        }

        [Fact]
        public async Task CreateMessageAsync_WithoutCurrentPersonId_UsesDtoPersonId()
        {
            // Arrange
            var tripId = 1;
            var dtoPersonId = "user-from-dto";
            var dto = new ChatMessageCreateDto
            {
                Message = "Test message",
                PersonId = dtoPersonId
            };
            string currentPersonId = null; // No current person ID
            var trip = new Trip { Id = tripId, Name = "Test Trip" , PersonId = "test" };
            var person = new Person {Id = dtoPersonId, FirstName = "John", LastName = "Doe", IsPrivate = false, Nationality = "poland" };
            var chatMessage = new ChatMessage
            {
                Id = 1,
                Message = "Test message",
                TripId = tripId,
                PersonId = dtoPersonId
            };

            _tripRepositoryMock
                .Setup(x => x.GetByIdAsync(tripId))
                .ReturnsAsync(trip);

            _personRepositoryMock
                .Setup(x => x.GetByIdAsync(dtoPersonId))
                .ReturnsAsync(person);

            _chatRepositoryMock
                .Setup(x => x.AddAsync(It.IsAny<ChatMessage>()))
                .ReturnsAsync(chatMessage);

            _chatRepositoryMock
                .Setup(x => x.GetByIdWithPersonAsync(chatMessage.Id))
                .ReturnsAsync(chatMessage);

            // Act
            var result = await _chatService.CreateMessageAsync(tripId, dto, currentPersonId);

            // Assert
            Assert.Equal(dtoPersonId, result.PersonId); // Should use dto.PersonId when currentPersonId is null
        }

        [Fact]
        public async Task CreateMessageAsync_TrimsMessage()
        {
            // Arrange
            var tripId = 1;
            var dto = new ChatMessageCreateDto
            {
                Message = "  Test message with spaces  ",
                PersonId = "user1"
            };
            var currentPersonId = "user1";
            var trip = new Trip { Id = tripId, Name = "Test Trip" , PersonId = "test" };
            var person = new Person {Id = "user1", FirstName = "John", LastName = "Doe", IsPrivate = false, Nationality = "poland" };
            var chatMessage = new ChatMessage
            {
                Id = 1,
                Message = "Test message with spaces", // Trimmed version
                TripId = tripId,
                PersonId = "user1"
            };

            _tripRepositoryMock
                .Setup(x => x.GetByIdAsync(tripId))
                .ReturnsAsync(trip);

            _personRepositoryMock
                .Setup(x => x.GetByIdAsync(currentPersonId))
                .ReturnsAsync(person);

            _chatRepositoryMock
                .Setup(x => x.AddAsync(It.Is<ChatMessage>(m => m.Message == "Test message with spaces")))
                .ReturnsAsync(chatMessage);

            _chatRepositoryMock
                .Setup(x => x.GetByIdWithPersonAsync(chatMessage.Id))
                .ReturnsAsync(chatMessage);

            // Act
            var result = await _chatService.CreateMessageAsync(tripId, dto, currentPersonId);

            // Assert
            Assert.Equal("Test message with spaces", result.Message); // Should be trimmed
        }

        [Fact]
        public async Task CreateMessageAsync_WhenGetByIdWithPersonReturnsNull_ReturnsAddedEntity()
        {
            // Arrange
            var tripId = 1;
            var dto = new ChatMessageCreateDto
            {
                Message = "Test message",
                PersonId = "user1"
            };
            var currentPersonId = "user1";
            var trip = new Trip { Id = tripId, Name = "Test Trip" , PersonId = "test" };
            var person = new Person {Id = "user1", FirstName = "John", LastName = "Doe", IsPrivate = false, Nationality = "poland" };
            var chatMessage = new ChatMessage
            {
                Id = 1,
                Message = "Test message",
                TripId = tripId,
                PersonId = "user1"
            };

            _tripRepositoryMock
                .Setup(x => x.GetByIdAsync(tripId))
                .ReturnsAsync(trip);

            _personRepositoryMock
                .Setup(x => x.GetByIdAsync(currentPersonId))
                .ReturnsAsync(person);

            _chatRepositoryMock
                .Setup(x => x.AddAsync(It.IsAny<ChatMessage>()))
                .ReturnsAsync(chatMessage);

            _chatRepositoryMock
                .Setup(x => x.GetByIdWithPersonAsync(chatMessage.Id))
                .ReturnsAsync((ChatMessage)null); // Simulate failure to load with person

            // Act
            var result = await _chatService.CreateMessageAsync(tripId, dto, currentPersonId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(chatMessage.Id, result.Id); // Should return the originally added entity
        }

        #endregion

        #region DeleteMessageAsync Tests

        [Fact]
        public async Task DeleteMessageAsync_WithExistingMessage_DeletesMessage()
        {
            // Arrange
            var messageId = 1;
            var currentPersonId = "user1";
            var message = new ChatMessage
            {
                Id = messageId,
                Message = "Test message",
                PersonId = "user1"
            };

            _chatRepositoryMock
                .Setup(x => x.GetByIdWithPersonAsync(messageId))
                .ReturnsAsync(message);

            _chatRepositoryMock
                .Setup(x => x.DeleteAsync(message))
                .Returns(Task.CompletedTask);

            // Act
            await _chatService.DeleteMessageAsync(messageId, currentPersonId);

            // Assert
            _chatRepositoryMock.Verify(x => x.DeleteAsync(message), Times.Once);
        }

        [Fact]
        public async Task DeleteMessageAsync_WithNonExistingMessage_DoesNothing()
        {
            // Arrange
            var messageId = 999;
            var currentPersonId = "user1";

            _chatRepositoryMock
                .Setup(x => x.GetByIdWithPersonAsync(messageId))
                .ReturnsAsync((ChatMessage)null);

            // Act
            await _chatService.DeleteMessageAsync(messageId, currentPersonId);

            // Assert
            _chatRepositoryMock.Verify(x => x.DeleteAsync(It.IsAny<ChatMessage>()), Times.Never);
        }

        [Fact]
        public async Task DeleteMessageAsync_WithoutCurrentPersonId_DeletesMessage()
        {
            // Arrange
            var messageId = 1;
            string currentPersonId = null; // No current person ID
            var message = new ChatMessage
            {
                Id = messageId,
                Message = "Test message",
                PersonId = "user1"
            };

            _chatRepositoryMock
                .Setup(x => x.GetByIdWithPersonAsync(messageId))
                .ReturnsAsync(message);

            _chatRepositoryMock
                .Setup(x => x.DeleteAsync(message))
                .Returns(Task.CompletedTask);

            // Act
            await _chatService.DeleteMessageAsync(messageId, currentPersonId);

            // Assert
            _chatRepositoryMock.Verify(x => x.DeleteAsync(message), Times.Once);
        }

        [Fact]
        public async Task DeleteMessageAsync_WithDifferentPersonId_ThrowsUnauthorizedAccessException()
        {
            // Arrange
            var messageId = 1;
            var currentPersonId = "user2"; // Different from message owner
            var message = new ChatMessage
            {
                Id = messageId,
                Message = "Test message",
                PersonId = "user1" // Message owned by user1
            };

            _chatRepositoryMock
                .Setup(x => x.GetByIdWithPersonAsync(messageId))
                .ReturnsAsync(message);

            // Act & Assert
            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                _chatService.DeleteMessageAsync(messageId, currentPersonId));

            _chatRepositoryMock.Verify(x => x.DeleteAsync(It.IsAny<ChatMessage>()), Times.Never);
        }

        [Fact]
        public async Task DeleteMessageAsync_WithSamePersonId_DeletesMessage()
        {
            // Arrange
            var messageId = 1;
            var currentPersonId = "user1"; // Same as message owner
            var message = new ChatMessage
            {
                Id = messageId,
                Message = "Test message",
                PersonId = "user1"
            };

            _chatRepositoryMock
                .Setup(x => x.GetByIdWithPersonAsync(messageId))
                .ReturnsAsync(message);

            _chatRepositoryMock
                .Setup(x => x.DeleteAsync(message))
                .Returns(Task.CompletedTask);

            // Act
            await _chatService.DeleteMessageAsync(messageId, currentPersonId);

            // Assert
            _chatRepositoryMock.Verify(x => x.DeleteAsync(message), Times.Once);
        }

        #endregion

        #region Generic Service Methods Tests

        [Fact]
        public async Task GetAllAsync_CallsBaseMethod()
        {
            // Arrange
            var messages = new List<ChatMessage>
            {
                new() { Id = 1, Message = "Message 1", PersonId = "test" },
                new() { Id = 2, Message = "Message 2", PersonId = "test"}
            };

            _chatRepositoryMock
                .Setup(x => x.GetAllAsync())
                .ReturnsAsync(messages);

            // Act
            var result = await _chatService.GetAllAsync();

            // Assert
            Assert.Equal(2, result.Count);
            _chatRepositoryMock.Verify(x => x.GetAllAsync(), Times.Once);
        }

        [Fact]
        public async Task GetByIdAsync_CallsBaseMethod()
        {
            // Arrange
            var messageId = 1;
            var message = new ChatMessage { Id = messageId, Message = "Test message", PersonId = "test" };

            _chatRepositoryMock
                .Setup(x => x.GetByIdAsync(messageId))
                .ReturnsAsync(message);

            // Act
            var result = await _chatService.GetByIdAsync(messageId);

            // Assert
            Assert.Equal(message, result);
            _chatRepositoryMock.Verify(x => x.GetByIdAsync(messageId), Times.Once);
        }

        [Fact]
        public async Task UpdateAsync_CallsBaseMethod()
        {
            // Arrange
            var message = new ChatMessage { Id = 1, Message = "Updated message", PersonId = "test" };

            _chatRepositoryMock
                .Setup(x => x.UpdateAsync(message))
                .Returns(Task.CompletedTask);

            // Act
            await _chatService.UpdateAsync(message);

            // Assert
            _chatRepositoryMock.Verify(x => x.UpdateAsync(message), Times.Once);
        }

        #endregion
    }
}