using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using TravelHub.Domain.DTOs;
using TravelHub.Domain.Entities;
using TravelHub.Domain.Interfaces;
using TravelHub.Domain.Interfaces.Repositories;
using TravelHub.Domain.Interfaces.Services;
using TravelHub.Infrastructure.Services;

namespace TravelHub.Tests.Infrastructure.Tests.Services
{
    public class FriendRequestServiceTests
    {
        private readonly Mock<IFriendRequestRepository> _friendRequestRepositoryMock;
        private readonly Mock<IPersonFriendsRepository> _personFriendsRepositoryMock;
        private readonly Mock<UserManager<Person>> _userManagerMock;
        private readonly Mock<IEmailSender> _emailSenderMock;
        private readonly Mock<ILogger<FriendRequestService>> _loggerMock;
        private readonly FriendRequestService _friendRequestService;

        public FriendRequestServiceTests()
        {
            _friendRequestRepositoryMock = new Mock<IFriendRequestRepository>();
            _personFriendsRepositoryMock = new Mock<IPersonFriendsRepository>();
            _emailSenderMock = new Mock<IEmailSender>();
            _loggerMock = new Mock<ILogger<FriendRequestService>>();

            var store = new Mock<IUserStore<Person>>();
            _userManagerMock = new Mock<UserManager<Person>>(
                store.Object, null, null, null, null, null, null, null, null);

            _friendRequestService = new FriendRequestService(
                _friendRequestRepositoryMock.Object,
                _personFriendsRepositoryMock.Object,
                _userManagerMock.Object,
                _emailSenderMock.Object,
                _loggerMock.Object
            );
        }

        #region SendFriendRequestAsync Tests

        [Fact]
        public async Task SendFriendRequestAsync_WithValidData_SendsRequest()
        {
            // Arrange
            var requesterId = "user1";
            var addresseeIdentifier = "user2@example.com";
            var message = "Hello, let's be friends!";

            var addressee = new Person { Id = "user2", Email = "user2@example.com", FirstName = "Jane", LastName = "Smith", IsPrivate = false, Nationality = "test" };
            var users = new List<Person> { addressee }.AsQueryable();

            var mockSet = new Mock<DbSet<Person>>();
            mockSet.As<IQueryable<Person>>().Setup(m => m.Provider).Returns(users.Provider);
            mockSet.As<IQueryable<Person>>().Setup(m => m.Expression).Returns(users.Expression);
            mockSet.As<IQueryable<Person>>().Setup(m => m.ElementType).Returns(users.ElementType);
            mockSet.As<IQueryable<Person>>().Setup(m => m.GetEnumerator()).Returns(users.GetEnumerator());

            _userManagerMock.Setup(x => x.Users).Returns(mockSet.Object);
            _personFriendsRepositoryMock
                .Setup(x => x.FriendshipExistsAsync(requesterId, addressee.Id))
                .ReturnsAsync(false);
            _friendRequestRepositoryMock
                .Setup(x => x.HasPendingRequestAsync(requesterId, addressee.Id))
                .ReturnsAsync(false);
            _friendRequestRepositoryMock
                .Setup(x => x.AddAsync(It.IsAny<FriendRequest>()))
                .ReturnsAsync((FriendRequest fr) => fr);

            // Act
            var result = await _friendRequestService.SendFriendRequestAsync(requesterId, addresseeIdentifier, message);

            // Assert
            Assert.Equal("Friend request sent successfully.", result.Message);
            Assert.NotNull(result.FriendRequest);
            Assert.True(result.Success);           

            _friendRequestRepositoryMock.Verify(x => x.AddAsync(It.Is<FriendRequest>(fr =>
                fr.RequesterId == requesterId &&
                fr.AddresseeId == addressee.Id &&
                fr.Status == FriendRequestStatus.Pending &&
                fr.Message == message
            )), Times.Once);

            _emailSenderMock.Verify(x => x.SendEmailAsync(
                addressee.Email,
                "New Friend Request on TravelHub",
                It.IsAny<string>()
            ), Times.Once);
        }

        [Fact]
        public async Task SendFriendRequestAsync_WithNonExistingUser_ReturnsFailure()
        {
            // Arrange
            var requesterId = "user1";
            var addresseeIdentifier = "nonexisting@example.com";
            var users = new List<Person>().AsQueryable();

            var mockSet = new Mock<DbSet<Person>>();
            mockSet.As<IQueryable<Person>>().Setup(m => m.Provider).Returns(users.Provider);
            mockSet.As<IQueryable<Person>>().Setup(m => m.Expression).Returns(users.Expression);
            mockSet.As<IQueryable<Person>>().Setup(m => m.ElementType).Returns(users.ElementType);
            mockSet.As<IQueryable<Person>>().Setup(m => m.GetEnumerator()).Returns(users.GetEnumerator());

            _userManagerMock.Setup(x => x.Users).Returns(mockSet.Object);

            // Act
            var result = await _friendRequestService.SendFriendRequestAsync(requesterId, addresseeIdentifier);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("User not found.", result.Message);

            _friendRequestRepositoryMock.Verify(x => x.AddAsync(It.IsAny<FriendRequest>()), Times.Never);
            _emailSenderMock.Verify(x => x.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task SendFriendRequestAsync_ToSelf_ReturnsFailure()
        {
            // Arrange
            var requesterId = "user1";
            var addresseeIdentifier = "user1";
            var user = new Person { Id = "user1", Email = "user1@example.com", FirstName = "test", LastName = "test", IsPrivate = false, Nationality = "poland" };
            var users = new List<Person> { user }.AsQueryable();

            var mockSet = new Mock<DbSet<Person>>();
            mockSet.As<IQueryable<Person>>().Setup(m => m.Provider).Returns(users.Provider);
            mockSet.As<IQueryable<Person>>().Setup(m => m.Expression).Returns(users.Expression);
            mockSet.As<IQueryable<Person>>().Setup(m => m.ElementType).Returns(users.ElementType);
            mockSet.As<IQueryable<Person>>().Setup(m => m.GetEnumerator()).Returns(users.GetEnumerator());

            _userManagerMock.Setup(x => x.Users).Returns(mockSet.Object);

            // Act
            var result = await _friendRequestService.SendFriendRequestAsync(requesterId, addresseeIdentifier);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("You cannot send friend request to yourself.", result.Message);
        }

        [Fact]
        public async Task SendFriendRequestAsync_WhenAlreadyFriends_ReturnsFailure()
        {
            // Arrange
            var requesterId = "user1";
            var addresseeIdentifier = "user2@example.com";
            var addressee = new Person {Id = "user2", Email = "user2@example.com", FirstName = "test", LastName = "test", IsPrivate = false, Nationality = "poland" };
            var users = new List<Person> { addressee }.AsQueryable();

            var mockSet = new Mock<DbSet<Person>>();
            mockSet.As<IQueryable<Person>>().Setup(m => m.Provider).Returns(users.Provider);
            mockSet.As<IQueryable<Person>>().Setup(m => m.Expression).Returns(users.Expression);
            mockSet.As<IQueryable<Person>>().Setup(m => m.ElementType).Returns(users.ElementType);
            mockSet.As<IQueryable<Person>>().Setup(m => m.GetEnumerator()).Returns(users.GetEnumerator());

            _userManagerMock.Setup(x => x.Users).Returns(mockSet.Object);
            _personFriendsRepositoryMock
                .Setup(x => x.FriendshipExistsAsync(requesterId, addressee.Id))
                .ReturnsAsync(true);

            // Act
            var result = await _friendRequestService.SendFriendRequestAsync(requesterId, addresseeIdentifier);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("You are already friends with this user.", result.Message);
        }

        [Fact]
        public async Task SendFriendRequestAsync_WithPendingRequest_ReturnsFailure()
        {
            // Arrange
            var requesterId = "user1";
            var addresseeIdentifier = "user2@example.com";
            var addressee = new Person {Id = "user2", Email = "user2@example.com", FirstName = "test", LastName = "test", IsPrivate = false, Nationality = "poland" };
            var users = new List<Person> { addressee }.AsQueryable();

            var mockSet = new Mock<DbSet<Person>>();
            mockSet.As<IQueryable<Person>>().Setup(m => m.Provider).Returns(users.Provider);
            mockSet.As<IQueryable<Person>>().Setup(m => m.Expression).Returns(users.Expression);
            mockSet.As<IQueryable<Person>>().Setup(m => m.ElementType).Returns(users.ElementType);
            mockSet.As<IQueryable<Person>>().Setup(m => m.GetEnumerator()).Returns(users.GetEnumerator());

            _userManagerMock.Setup(x => x.Users).Returns(mockSet.Object);
            _personFriendsRepositoryMock
                .Setup(x => x.FriendshipExistsAsync(requesterId, addressee.Id))
                .ReturnsAsync(false);
            _friendRequestRepositoryMock
                .Setup(x => x.HasPendingRequestAsync(requesterId, addressee.Id))
                .ReturnsAsync(true);

            // Act
            var result = await _friendRequestService.SendFriendRequestAsync(requesterId, addresseeIdentifier);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("Friend request already exists.", result.Message);
        }

        [Fact]
        public async Task SendFriendRequestAsync_WithException_ReturnsFailureAndLogsError()
        {
            // Arrange
            var requesterId = "user1";
            var addresseeIdentifier = "user2@example.com";
            var addressee = new Person {Id = "user2", Email = "user2@example.com", FirstName = "test", LastName = "test", IsPrivate = false, Nationality = "poland" };
            var users = new List<Person> { addressee }.AsQueryable();

            var mockSet = new Mock<DbSet<Person>>();
            mockSet.As<IQueryable<Person>>().Setup(m => m.Provider).Returns(users.Provider);
            mockSet.As<IQueryable<Person>>().Setup(m => m.Expression).Returns(users.Expression);
            mockSet.As<IQueryable<Person>>().Setup(m => m.ElementType).Returns(users.ElementType);
            mockSet.As<IQueryable<Person>>().Setup(m => m.GetEnumerator()).Returns(users.GetEnumerator());

            _userManagerMock.Setup(x => x.Users).Returns(mockSet.Object);
            _personFriendsRepositoryMock
                .Setup(x => x.FriendshipExistsAsync(requesterId, addressee.Id))
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _friendRequestService.SendFriendRequestAsync(requesterId, addresseeIdentifier);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("An error occurred while sending friend request.", result.Message);

            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Error sending friend request")),
                    It.IsAny<Exception>(),
                    It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)),
                Times.Once);
        }

        #endregion

        #region AcceptFriendRequestAsync Tests

        [Fact]
        public async Task AcceptFriendRequestAsync_WithValidRequest_AcceptsRequest()
        {
            // Arrange
            var friendRequestId = 1;
            var userId = "user2";
            var friendRequest = new FriendRequest
            {
                Id = friendRequestId,
                RequesterId = "user1",
                AddresseeId = userId,
                Status = FriendRequestStatus.Pending,
                Requester = new Person {Id = "user1", FirstName = "John", LastName = "Doe", Email = "user1@example.com", IsPrivate = false, Nationality = "test" },
                Addressee = new Person {Id = "user2", FirstName = "Jane", LastName = "Smith", Email = "user2@example.com", IsPrivate = false, Nationality = "test" }
            };

            _friendRequestRepositoryMock
                .Setup(x => x.GetByIdAsync(friendRequestId))
                .ReturnsAsync(friendRequest);
            _friendRequestRepositoryMock
                .Setup(x => x.UpdateAsync(friendRequest))
                .Returns(Task.CompletedTask);
            _personFriendsRepositoryMock
                .Setup(x => x.AddAsync(It.IsAny<PersonFriends>()))
                .ReturnsAsync((PersonFriends pf) => pf);
            _userManagerMock
                .Setup(x => x.FindByIdAsync("user1"))
                .ReturnsAsync(friendRequest.Requester);

            // Act
            var result = await _friendRequestService.AcceptFriendRequestAsync(friendRequestId, userId);

            // Assert
            Assert.True(result.Success);
            Assert.Equal("Friend request accepted.", result.Message);
            Assert.Equal(FriendRequestStatus.Accepted, result.FriendRequest.Status);
            Assert.NotNull(result.FriendRequest.RespondedAt);

            _friendRequestRepositoryMock.Verify(x => x.UpdateAsync(friendRequest), Times.Once);
            _personFriendsRepositoryMock.Verify(x => x.AddAsync(It.IsAny<PersonFriends>()), Times.Exactly(2));
            _emailSenderMock.Verify(x => x.SendEmailAsync(
                "user1@example.com",
                "Friend Request Accepted on TravelHub",
                It.IsAny<string>()
            ), Times.Once);
        }

        [Fact]
        public async Task AcceptFriendRequestAsync_WithNonExistingRequest_ReturnsFailure()
        {
            // Arrange
            var friendRequestId = 999;
            var userId = "user2";

            _friendRequestRepositoryMock
                .Setup(x => x.GetByIdAsync(friendRequestId))
                .ReturnsAsync((FriendRequest)null);

            // Act
            var result = await _friendRequestService.AcceptFriendRequestAsync(friendRequestId, userId);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("Friend request not found.", result.Message);
        }

        [Fact]
        public async Task AcceptFriendRequestAsync_WithWrongUser_ReturnsFailure()
        {
            // Arrange
            var friendRequestId = 1;
            var userId = "user3"; // Not the addressee
            var friendRequest = new FriendRequest
            {
                Id = friendRequestId,
                RequesterId = "user1",
                AddresseeId = "user2",
                Status = FriendRequestStatus.Pending
            };

            _friendRequestRepositoryMock
                .Setup(x => x.GetByIdAsync(friendRequestId))
                .ReturnsAsync(friendRequest);

            // Act
            var result = await _friendRequestService.AcceptFriendRequestAsync(friendRequestId, userId);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("Friend request not found.", result.Message);
        }

        [Fact]
        public async Task AcceptFriendRequestAsync_WithNonPendingStatus_ReturnsFailure()
        {
            // Arrange
            var friendRequestId = 1;
            var userId = "user2";
            var friendRequest = new FriendRequest
            {
                Id = friendRequestId,
                RequesterId = "user1",
                AddresseeId = userId,
                Status = FriendRequestStatus.Declined // Already declined
            };

            _friendRequestRepositoryMock
                .Setup(x => x.GetByIdAsync(friendRequestId))
                .ReturnsAsync(friendRequest);

            // Act
            var result = await _friendRequestService.AcceptFriendRequestAsync(friendRequestId, userId);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("Friend request is not pending.", result.Message);
        }

        #endregion

        #region DeclineFriendRequestAsync Tests

        [Fact]
        public async Task DeclineFriendRequestAsync_WithValidRequest_DeclinesRequest()
        {
            // Arrange
            var friendRequestId = 1;
            var userId = "user2";
            var friendRequest = new FriendRequest
            {
                Id = friendRequestId,
                RequesterId = "user1",
                AddresseeId = userId,
                Status = FriendRequestStatus.Pending
            };

            _friendRequestRepositoryMock
                .Setup(x => x.GetByIdAsync(friendRequestId))
                .ReturnsAsync(friendRequest);
            _friendRequestRepositoryMock
                .Setup(x => x.UpdateAsync(friendRequest))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _friendRequestService.DeclineFriendRequestAsync(friendRequestId, userId);

            // Assert
            Assert.True(result.Success);
            Assert.Equal("Friend request declined.", result.Message);
            Assert.Equal(FriendRequestStatus.Declined, result.FriendRequest.Status);
            Assert.NotNull(result.FriendRequest.RespondedAt);

            _friendRequestRepositoryMock.Verify(x => x.UpdateAsync(friendRequest), Times.Once);
        }

        [Fact]
        public async Task DeclineFriendRequestAsync_WithNonExistingRequest_ReturnsFailure()
        {
            // Arrange
            var friendRequestId = 999;
            var userId = "user2";

            _friendRequestRepositoryMock
                .Setup(x => x.GetByIdAsync(friendRequestId))
                .ReturnsAsync((FriendRequest)null);

            // Act
            var result = await _friendRequestService.DeclineFriendRequestAsync(friendRequestId, userId);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("Friend request not found.", result.Message);
        }

        #endregion

        #region CancelFriendRequestAsync Tests

        [Fact]
        public async Task CancelFriendRequestAsync_WithValidRequest_CancelsRequest()
        {
            // Arrange
            var friendRequestId = 1;
            var userId = "user1";
            var friendRequest = new FriendRequest
            {
                Id = friendRequestId,
                RequesterId = userId,
                AddresseeId = "user2",
                Status = FriendRequestStatus.Pending
            };

            _friendRequestRepositoryMock
                .Setup(x => x.GetByIdAsync(friendRequestId))
                .ReturnsAsync(friendRequest);
            _friendRequestRepositoryMock
                .Setup(x => x.DeleteAsync(friendRequest))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _friendRequestService.CancelFriendRequestAsync(friendRequestId, userId);

            // Assert
            Assert.True(result.Success);
            Assert.Equal("Friend request cancelled.", result.Message);

            _friendRequestRepositoryMock.Verify(x => x.DeleteAsync(friendRequest), Times.Once);
        }

        [Fact]
        public async Task CancelFriendRequestAsync_WithNonExistingRequest_ReturnsFailure()
        {
            // Arrange
            var friendRequestId = 999;
            var userId = "user1";

            _friendRequestRepositoryMock
                .Setup(x => x.GetByIdAsync(friendRequestId))
                .ReturnsAsync((FriendRequest)null);

            // Act
            var result = await _friendRequestService.CancelFriendRequestAsync(friendRequestId, userId);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("Friend request not found.", result.Message);
        }

        [Fact]
        public async Task CancelFriendRequestAsync_WithWrongUser_ReturnsFailure()
        {
            // Arrange
            var friendRequestId = 1;
            var userId = "user3"; // Not the requester
            var friendRequest = new FriendRequest
            {
                Id = friendRequestId,
                RequesterId = "user1",
                AddresseeId = "user2",
                Status = FriendRequestStatus.Pending
            };

            _friendRequestRepositoryMock
                .Setup(x => x.GetByIdAsync(friendRequestId))
                .ReturnsAsync(friendRequest);

            // Act
            var result = await _friendRequestService.CancelFriendRequestAsync(friendRequestId, userId);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("Friend request not found.", result.Message);

            _friendRequestRepositoryMock.Verify(x => x.DeleteAsync(It.IsAny<FriendRequest>()), Times.Never);
        }

        #endregion

        #region GetPendingRequestsAsync Tests

        [Fact]
        public async Task GetPendingRequestsAsync_WithUserId_ReturnsPendingRequests()
        {
            // Arrange
            var userId = "user2";
            var pendingRequests = new List<FriendRequest>
            {
                new FriendRequest { Id = 1, RequesterId = "user1", AddresseeId = userId, Status = FriendRequestStatus.Pending },
                new FriendRequest { Id = 2, RequesterId = "user3", AddresseeId = userId, Status = FriendRequestStatus.Pending }
            };

            _friendRequestRepositoryMock
                .Setup(x => x.GetPendingRequestsAsync(userId))
                .ReturnsAsync(pendingRequests);

            // Act
            var result = await _friendRequestService.GetPendingRequestsAsync(userId);

            // Assert
            Assert.Equal(2, result.Count);
            Assert.All(result, r => Assert.Equal(FriendRequestStatus.Pending, r.Status));
            Assert.All(result, r => Assert.Equal(userId, r.AddresseeId));
        }

        #endregion

        #region GetSentRequestsAsync Tests

        [Fact]
        public async Task GetSentRequestsAsync_WithUserId_ReturnsSentRequests()
        {
            // Arrange
            var userId = "user1";
            var sentRequests = new List<FriendRequest>
            {
                new FriendRequest { Id = 1, RequesterId = userId, AddresseeId = "user2", Status = FriendRequestStatus.Pending },
                new FriendRequest { Id = 2, RequesterId = userId, AddresseeId = "user3", Status = FriendRequestStatus.Pending }
            };

            _friendRequestRepositoryMock
                .Setup(x => x.GetSentRequestsAsync(userId))
                .ReturnsAsync(sentRequests);

            // Act
            var result = await _friendRequestService.GetSentRequestsAsync(userId);

            // Assert
            Assert.Equal(2, result.Count);
            Assert.All(result, r => Assert.Equal(userId, r.RequesterId));
        }

        #endregion

        #region HasPendingRequestAsync Tests

        [Fact]
        public async Task HasPendingRequestAsync_WithPendingRequest_ReturnsTrue()
        {
            // Arrange
            var user1Id = "user1";
            var user2Id = "user2";

            _friendRequestRepositoryMock
                .Setup(x => x.HasPendingRequestAsync(user1Id, user2Id))
                .ReturnsAsync(true);

            // Act
            var result = await _friendRequestService.HasPendingRequestAsync(user1Id, user2Id);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task HasPendingRequestAsync_WithoutPendingRequest_ReturnsFalse()
        {
            // Arrange
            var user1Id = "user1";
            var user2Id = "user2";

            _friendRequestRepositoryMock
                .Setup(x => x.HasPendingRequestAsync(user1Id, user2Id))
                .ReturnsAsync(false);

            // Act
            var result = await _friendRequestService.HasPendingRequestAsync(user1Id, user2Id);

            // Assert
            Assert.False(result);
        }

        #endregion

        #region Email Sending Tests

        [Fact]
        public async Task SendFriendRequestAsync_WhenEmailFails_StillReturnsSuccess()
        {
            // Arrange
            var requesterId = "user1";
            var addresseeIdentifier = "user2@example.com";
            var addressee = new Person { Id = "user2", Email = "user2@example.com", FirstName = "Jane", IsPrivate = false, LastName = "test", Nationality = "test" };
            var users = new List<Person> { addressee }.AsQueryable();

            var mockSet = new Mock<DbSet<Person>>();
            mockSet.As<IQueryable<Person>>().Setup(m => m.Provider).Returns(users.Provider);
            mockSet.As<IQueryable<Person>>().Setup(m => m.Expression).Returns(users.Expression);
            mockSet.As<IQueryable<Person>>().Setup(m => m.ElementType).Returns(users.ElementType);
            mockSet.As<IQueryable<Person>>().Setup(m => m.GetEnumerator()).Returns(users.GetEnumerator());

            _userManagerMock.Setup(x => x.Users).Returns(mockSet.Object);
            _personFriendsRepositoryMock
                .Setup(x => x.FriendshipExistsAsync(requesterId, addressee.Id))
                .ReturnsAsync(false);
            _friendRequestRepositoryMock
                .Setup(x => x.HasPendingRequestAsync(requesterId, addressee.Id))
                .ReturnsAsync(false);
            _friendRequestRepositoryMock
                .Setup(x => x.AddAsync(It.IsAny<FriendRequest>()))
                .ReturnsAsync((FriendRequest fr) => fr);
            _emailSenderMock
                .Setup(x => x.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ThrowsAsync(new Exception("Email service unavailable"));

            // Act
            var result = await _friendRequestService.SendFriendRequestAsync(requesterId, addresseeIdentifier);

            // Assert
            Assert.True(result.Success);
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Error sending friend request email")),
                    It.IsAny<Exception>(),
                    It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)),
                Times.Once);
        }

        [Fact]
        public async Task AcceptFriendRequestAsync_WhenRequesterNotFound_StillCompletes()
        {
            // Arrange
            var friendRequestId = 1;
            var userId = "user2";
            var friendRequest = new FriendRequest
            {
                Id = friendRequestId,
                RequesterId = "user1",
                AddresseeId = userId,
                Status = FriendRequestStatus.Pending,
                Addressee = new Person { Id = "user2", FirstName = "Jane", LastName = "Smith", IsPrivate = false, Nationality = "test" }
            };

            _friendRequestRepositoryMock
                .Setup(x => x.GetByIdAsync(friendRequestId))
                .ReturnsAsync(friendRequest);
            _friendRequestRepositoryMock
                .Setup(x => x.UpdateAsync(friendRequest))
                .Returns(Task.CompletedTask);
            _personFriendsRepositoryMock
                .Setup(x => x.AddAsync(It.IsAny<PersonFriends>()))
                .ReturnsAsync((PersonFriends pf) => pf);
            _userManagerMock
                .Setup(x => x.FindByIdAsync("user1"))
                .ReturnsAsync((Person)null); // Requester not found

            // Act
            var result = await _friendRequestService.AcceptFriendRequestAsync(friendRequestId, userId);

            // Assert
            Assert.True(result.Success); // Should still succeed
            _emailSenderMock.Verify(x => x.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        #endregion
    }
}