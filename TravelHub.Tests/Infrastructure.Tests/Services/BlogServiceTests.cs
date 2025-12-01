using Moq;
using Microsoft.AspNetCore.Identity;
using TravelHub.Domain.DTOs;
using TravelHub.Domain.Entities;
using TravelHub.Domain.Interfaces.Repositories;
using TravelHub.Domain.Interfaces.Services;
using TravelHub.Infrastructure.Services;
using Xunit.Sdk;

namespace TravelHub.Tests.Infrastructure.Tests.Services
{
    public class BlogServiceTests
    {
        private readonly Mock<IBlogRepository> _blogRepositoryMock;
        private readonly Mock<ITripParticipantRepository> _tripParticipantRepositoryMock;
        private readonly Mock<IFriendshipService> _friendshipServiceMock;
        private readonly Mock<ISpotService> _spotServiceMock;
        private readonly Mock<UserManager<Person>> _userManagerMock;
        private readonly BlogService _blogService;

        public BlogServiceTests()
        {
            _blogRepositoryMock = new Mock<IBlogRepository>();
            _tripParticipantRepositoryMock = new Mock<ITripParticipantRepository>();
            _friendshipServiceMock = new Mock<IFriendshipService>();
            _spotServiceMock = new Mock<ISpotService>();

            var store = new Mock<IUserStore<Person>>();
            _userManagerMock = new Mock<UserManager<Person>>(
                store.Object, null, null, null, null, null, null, null, null);

            _blogService = new BlogService(
                _blogRepositoryMock.Object,
                _tripParticipantRepositoryMock.Object,
                _friendshipServiceMock.Object,
                _spotServiceMock.Object,
                _userManagerMock.Object
            );
        }

        #region GetByTripIdAsync Tests

        [Fact]
        public async Task GetByTripIdAsync_WithExistingTripId_ReturnsBlog()
        {
            // Arrange
            var tripId = 1;
            var expectedBlog = new Blog { Id = 1, TripId = tripId, Name = "test", OwnerId = "test" };

            _blogRepositoryMock
                .Setup(x => x.GetByTripIdAsync(tripId))
                .ReturnsAsync(expectedBlog);

            // Act
            var result = await _blogService.GetByTripIdAsync(tripId);

            // Assert
            Assert.Equal(expectedBlog, result);
            _blogRepositoryMock.Verify(x => x.GetByTripIdAsync(tripId), Times.Once);
        }

        [Fact]
        public async Task GetByTripIdAsync_WithNonExistingTripId_ReturnsNull()
        {
            // Arrange
            var tripId = 999;

            _blogRepositoryMock
                .Setup(x => x.GetByTripIdAsync(tripId))
                .ReturnsAsync((Blog)null);

            // Act
            var result = await _blogService.GetByTripIdAsync(tripId);

            // Assert
            Assert.Null(result);
        }

        #endregion

        #region GetWithPostsAsync Tests

        [Fact]
        public async Task GetWithPostsAsync_WithExistingBlogId_ReturnsBlogWithPosts()
        {
            // Arrange
            var blogId = 1;
            var expectedBlog = new Blog
            {
                Id = blogId,
                Posts = new List<Post>
                {
                    new Post { Id = 1, Content = "Test Post", AuthorId = "test", Title = "test" }
                },
                Name = "test",
                OwnerId = "test"
            };

            _blogRepositoryMock
                .Setup(x => x.GetWithPostsAsync(blogId))
                .ReturnsAsync(expectedBlog);

            // Act
            var result = await _blogService.GetWithPostsAsync(blogId);

            // Assert
            Assert.Equal(expectedBlog, result);
            Assert.Single(result.Posts);
        }

        [Fact]
        public async Task GetWithPostsAsync_WithNonExistingBlogId_ReturnsNull()
        {
            // Arrange
            var blogId = 999;

            _blogRepositoryMock
                .Setup(x => x.GetWithPostsAsync(blogId))
                .ReturnsAsync((Blog)null);

            // Act
            var result = await _blogService.GetWithPostsAsync(blogId);

            // Assert
            Assert.Null(result);
        }

        #endregion

        #region GetByOwnerIdAsync Tests

        [Fact]
        public async Task GetByOwnerIdAsync_WithExistingOwnerId_ReturnsBlogs()
        {
            // Arrange
            var ownerId = "user1";
            var expectedBlogs = new List<Blog>
            {
                new Blog { Id = 1, OwnerId = ownerId, Name = "test" },
                new Blog { Id = 2, OwnerId = ownerId, Name = "test" }
            };

            _blogRepositoryMock
                .Setup(x => x.GetByOwnerIdAsync(ownerId))
                .ReturnsAsync(expectedBlogs);

            // Act
            var result = await _blogService.GetByOwnerIdAsync(ownerId);

            // Assert
            Assert.Equal(2, result.Count);
            Assert.All(result, blog => Assert.Equal(ownerId, blog.OwnerId));
        }

        [Fact]
        public async Task GetByOwnerIdAsync_WithNonExistingOwnerId_ReturnsEmptyList()
        {
            // Arrange
            var ownerId = "non-existing-user";

            _blogRepositoryMock
                .Setup(x => x.GetByOwnerIdAsync(ownerId))
                .ReturnsAsync(new List<Blog>());

            // Act
            var result = await _blogService.GetByOwnerIdAsync(ownerId);

            // Assert
            Assert.Empty(result);
        }

        #endregion

        #region CanUserAccessBlogAsync Tests

        [Fact]
        public async Task CanUserAccessBlogAsync_WithNonExistingBlog_ReturnsFalse()
        {
            // Arrange
            var blogId = 999;
            var userId = "user1";

            _blogRepositoryMock
                .Setup(x => x.GetByIdAsync(blogId))
                .ReturnsAsync((Blog)null);

            // Act
            var result = await _blogService.CanUserAccessBlogAsync(blogId, userId);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task CanUserAccessBlogAsync_WithPublicBlog_ReturnsTrue()
        {
            // Arrange
            var blogId = 1;
            var userId = "user1";
            var blog = new Blog
            {
                Id = blogId,
                Visibility = BlogVisibility.Public,
                TripId = 1,
                Name = "test",
                OwnerId = "test"
            };

            _blogRepositoryMock
                .Setup(x => x.GetByIdAsync(blogId))
                .ReturnsAsync(blog);

            // Act
            var result = await _blogService.CanUserAccessBlogAsync(blogId, userId);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task CanUserAccessBlogAsync_WithPrivateBlog_ReturnsFalse()
        {
            // Arrange
            var blogId = 1;
            var userId = "user1";
            var blog = new Blog
            {
                Id = blogId,
                Visibility = BlogVisibility.Private,
                TripId = 1,
                Name = "test",
                OwnerId = "test"
            };

            _blogRepositoryMock
                .Setup(x => x.GetByIdAsync(blogId))
                .ReturnsAsync(blog);

            // Act
            var result = await _blogService.CanUserAccessBlogAsync(blogId, userId);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task CanUserAccessBlogAsync_WithForTripParticipants_AndUserIsParticipant_ReturnsTrue()
        {
            // Arrange
            var blogId = 1;
            var userId = "user1";
            var blog = new Blog
            {
                Id = blogId,
                Visibility = BlogVisibility.ForTripParticipantsFriends,
                TripId = 1,
                Name = "test",
                OwnerId = "test"
            };

            _blogRepositoryMock
                .Setup(x => x.GetByIdAsync(blogId))
                .ReturnsAsync(blog);

            _tripParticipantRepositoryMock
                .Setup(x => x.ExistsAsync(blog.TripId, userId))
                .ReturnsAsync(true);

            // Act
            var result = await _blogService.CanUserAccessBlogAsync(blogId, userId);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task CanUserAccessBlogAsync_WithForTripParticipants_AndUserIsNotParticipant_ReturnsFalse()
        {
            // Arrange
            var blogId = 1;
            var userId = "user1";
            var blog = new Blog
            {
                Id = blogId,
                Visibility = BlogVisibility.ForTripParticipantsFriends,
                TripId = 1,
                Name = "test",
                OwnerId = "test"
            };

            _blogRepositoryMock
                .Setup(x => x.GetByIdAsync(blogId))
                .ReturnsAsync(blog);

            _tripParticipantRepositoryMock
                .Setup(x => x.ExistsAsync(blog.TripId, userId))
                .ReturnsAsync(false);

            // Act
            var result = await _blogService.CanUserAccessBlogAsync(blogId, userId);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task CanUserAccessBlogAsync_WithForMyFriends_AndUserIsFriend_ReturnsTrue()
        {
            // Arrange
            var blogId = 1;
            var userId = "user2";
            var blog = new Blog
            {
                Id = blogId,
                Visibility = BlogVisibility.ForMyFriends,
                TripId = 1,
                OwnerId = "user1",
                Name = "test"
            };

            _blogRepositoryMock
                .Setup(x => x.GetByIdAsync(blogId))
                .ReturnsAsync(blog);

            _tripParticipantRepositoryMock
                .Setup(x => x.ExistsAsync(blog.TripId, userId))
                .ReturnsAsync(false);

            _friendshipServiceMock
                .Setup(x => x.IsFriendAsync(blog.OwnerId, userId))
                .ReturnsAsync(true);

            // Act
            var result = await _blogService.CanUserAccessBlogAsync(blogId, userId);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task CanUserAccessBlogAsync_WithForMyFriends_AndUserIsNotFriend_ReturnsFalse()
        {
            // Arrange
            var blogId = 1;
            var userId = "user2";
            var blog = new Blog
            {
                Id = blogId,
                Visibility = BlogVisibility.ForMyFriends,
                TripId = 1,
                OwnerId = "user1", Name = "test"
            };

            _blogRepositoryMock
                .Setup(x => x.GetByIdAsync(blogId))
                .ReturnsAsync(blog);

            _tripParticipantRepositoryMock
                .Setup(x => x.ExistsAsync(blog.TripId, userId))
                .ReturnsAsync(false);

            _friendshipServiceMock
                .Setup(x => x.IsFriendAsync(blog.OwnerId, userId))
                .ReturnsAsync(false);

            // Act
            var result = await _blogService.CanUserAccessBlogAsync(blogId, userId);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task CanUserAccessBlogAsync_WithForTripParticipantsFriends_AndUserIsFriendOfParticipant_ReturnsTrue()
        {
            // Arrange
            var blogId = 1;
            var userId = "user3";
            var blog = new Blog
            {
                Id = blogId,
                Visibility = BlogVisibility.ForTripParticipantsFriends,
                TripId = 1,
                OwnerId = "user1", Name = "test"
            };
            var participants = new List<TripParticipant>
            {
                new TripParticipant { PersonId = "user1" },
                new TripParticipant { PersonId = "user2" }
            };

            _blogRepositoryMock
                .Setup(x => x.GetByIdAsync(blogId))
                .ReturnsAsync(blog);

            _tripParticipantRepositoryMock
                .Setup(x => x.ExistsAsync(blog.TripId, userId))
                .ReturnsAsync(false);

            _tripParticipantRepositoryMock
                .Setup(x => x.GetByTripIdAsync(blog.TripId))
                .ReturnsAsync(participants);

            _friendshipServiceMock
                .Setup(x => x.IsFriendAsync("user2", userId))
                .ReturnsAsync(true);

            // Act
            var result = await _blogService.CanUserAccessBlogAsync(blogId, userId);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task CanUserAccessBlogAsync_WithForTripParticipantsFriends_AndUserIsNotFriend_ReturnsFalse()
        {
            // Arrange
            var blogId = 1;
            var userId = "user3";
            var blog = new Blog
            {
                Id = blogId,
                Visibility = BlogVisibility.ForTripParticipantsFriends,
                TripId = 1,
                OwnerId = "user1", Name = "test"
            };
            var participants = new List<TripParticipant>
            {
                new TripParticipant { PersonId = "user1" },
                new TripParticipant { PersonId = "user2" }
            };

            _blogRepositoryMock
                .Setup(x => x.GetByIdAsync(blogId))
                .ReturnsAsync(blog);

            _tripParticipantRepositoryMock
                .Setup(x => x.ExistsAsync(blog.TripId, userId))
                .ReturnsAsync(false);

            _tripParticipantRepositoryMock
                .Setup(x => x.GetByTripIdAsync(blog.TripId))
                .ReturnsAsync(participants);

            _friendshipServiceMock
                .Setup(x => x.IsFriendAsync(It.IsAny<string>(), userId))
                .ReturnsAsync(false);

            // Act
            var result = await _blogService.CanUserAccessBlogAsync(blogId, userId);

            // Assert
            Assert.False(result);
        }

        #endregion

        #region CanUserCommentOnBlogAsync Tests

        [Fact]
        public async Task CanUserCommentOnBlogAsync_WithAccessibleBlog_ReturnsTrue()
        {
            // Arrange
            var blogId = 1;
            var userId = "user1";
            var blog = new Blog
            {
                Id = blogId,
                Visibility = BlogVisibility.Public,
                TripId = 1,
                Name = "test",
                OwnerId = "test"
            };

            _blogRepositoryMock
                .Setup(x => x.GetByIdAsync(blogId))
                .ReturnsAsync(blog);

            // Act
            var result = await _blogService.CanUserCommentOnBlogAsync(blogId, userId);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task CanUserCommentOnBlogAsync_WithInaccessibleBlog_ReturnsFalse()
        {
            // Arrange
            var blogId = 1;
            var userId = "user1";
            var blog = new Blog
            {
                Id = blogId,
                Visibility = BlogVisibility.Private,
                TripId = 1,
                Name = "test",
                OwnerId = "test"
            };

            _blogRepositoryMock
                .Setup(x => x.GetByIdAsync(blogId))
                .ReturnsAsync(blog);

            // Act
            var result = await _blogService.CanUserCommentOnBlogAsync(blogId, userId);

            // Assert
            Assert.False(result);
        }

        #endregion

        #region GetAccessibleBlogsAsync Tests

        [Fact]
        public async Task GetAccessibleBlogsAsync_WithUserId_ReturnsAccessibleBlogs()
        {
            // Arrange
            var userId = "user1";

            var blogs = new List<Blog>
                {
                    new Blog
                    {
                        Id = 1,
                        Visibility = BlogVisibility.Public,
                        TripId = 1,
                        Name = "Public Blog",
                        Description = "Test",
                        Catalog = "test",
                        OwnerId = "owner1",
                        Owner = new Person
                        {
                            Id = "owner1",
                            FirstName = "John",
                            LastName = "Doe",
                            IsPrivate = false,
                            Nationality = "poland"
                        },
                        Posts = new List<Post>(),
                        Trip = new Trip { Id = 1, Name = "Trip 1", PersonId = "test" }
                    },
                    new Blog
                    {
                        Id = 2,
                        Visibility = BlogVisibility.Private,
                        TripId = 2,
                        Name = "Private Blog",
                        Description = "Test",
                        Catalog = "test",
                        OwnerId = "owner2",
                        Owner = new Person
                        {
                            Id = "owner2",
                            FirstName = "Jane",
                            LastName = "Smith",
                            IsPrivate = false,
                            Nationality = "poland"
                        },
                        Posts = new List<Post>(),
                        Trip = new Trip { Id = 2, Name = "Trip 2", PersonId = "test" }
                    },
                    new Blog
                    {
                        Id = 3,
                        Visibility = BlogVisibility.ForMyFriends,
                        TripId = 3,
                        Name = "Friends Blog",
                        Description = "Test",
                        Catalog = "test",
                        OwnerId = "user2",
                        Owner = new Person
                        {
                            Id = "user2",
                            FirstName = "Friend",
                            LastName = "Owner",
                            IsPrivate = false,
                            Nationality = "poland"
                        },
                        Posts = new List<Post>(),
                        Trip = new Trip { Id = 3, Name = "Trip 3", PersonId = "user2" }
                    }
                };

            _blogRepositoryMock
                .Setup(x => x.GetAllAsync())
                .ReturnsAsync(blogs);

            _blogRepositoryMock
                .Setup(x => x.GetByIdAsync(It.IsAny<int>()))
                .ReturnsAsync((int id) => blogs.FirstOrDefault(b => b.Id == id));

            _tripParticipantRepositoryMock
                .Setup(x => x.ExistsAsync(It.IsAny<int>(), It.IsAny<string>()))
                .ReturnsAsync(false);

            _friendshipServiceMock
                .Setup(x => x.IsFriendAsync("user2", userId))
                .ReturnsAsync(true);

            _spotServiceMock
                .Setup(x => x.GetCountriesByTripAsync(It.IsAny<int>()))
                .ReturnsAsync(new List<Country>());

            _friendshipServiceMock
                .Setup(x => x.GetFriendsAsync(userId))
                .ReturnsAsync(new List<Person>());

            _tripParticipantRepositoryMock
                .Setup(x => x.GetByTripIdAsync(It.IsAny<int>()))
                .ReturnsAsync(new List<TripParticipant>());

            _userManagerMock
                .Setup(x => x.FindByIdAsync(It.IsAny<string>()))
                .ReturnsAsync((string id) => new Person
                {
                    Id = id,
                    FirstName = "Test",
                    LastName = "User",
                    IsPrivate = false,
                    Nationality = "poland"
                });

            // Act
            var result = await _blogService.GetAccessibleBlogsAsync(userId);

            // Assert
            Assert.Equal(2, result.Count); // Public and ForMyFriends (user is friend)
            Assert.Contains(result, b => b.Id == 1);
            Assert.Contains(result, b => b.Id == 3);

            var publicBlog = result.First(b => b.Id == 1);
            Assert.Equal("Public Blog", publicBlog.Name);
            Assert.Equal("John Doe", publicBlog.OwnerName);

            var friendsBlog = result.First(b => b.Id == 3);
            Assert.Equal("Friends Blog", friendsBlog.Name);
            Assert.Equal("Friend Owner", friendsBlog.OwnerName);
        }

        [Fact]
        public async Task GetAccessibleBlogsAsync_WithoutUserId_ReturnsOnlyPublicBlogs()
        {
            // Arrange
            var blogs = new List<Blog>
            {
                new Blog { Id = 1, Visibility = BlogVisibility.Public, TripId = 1, Name = "test", OwnerId = "test" },
                new Blog { Id = 2, Visibility = BlogVisibility.Private, TripId = 2, Name = "test", OwnerId = "test" },
                new Blog { Id = 3, Visibility = BlogVisibility.ForMyFriends, TripId = 3, Name = "test", OwnerId = "test" }
            };

            _blogRepositoryMock
                .Setup(x => x.GetAllAsync())
                .ReturnsAsync(blogs);

            _spotServiceMock
                .Setup(x => x.GetCountriesByTripAsync(It.IsAny<int>()))
                .ReturnsAsync(new List<Country>());

            _userManagerMock
                .Setup(x => x.FindByIdAsync(It.IsAny<string>()))
                .ReturnsAsync(new Person {FirstName = "Test", LastName = "User", IsPrivate = false, Nationality = "poland" });

            // Act
            var result = await _blogService.GetAccessibleBlogsAsync();

            // Assert
            Assert.Single(result);
            Assert.Contains(result, b => b.Id == 1);
        }

        [Fact]
        public async Task GetAccessibleBlogsAsync_WithNullBlogList_ReturnsEmptyList()
        {
            // Arrange
            _blogRepositoryMock
                .Setup(x => x.GetAllAsync())
                .ReturnsAsync(new List<Blog>());

            _friendshipServiceMock
                .Setup(x => x.GetFriendsAsync("user1"))
                .ReturnsAsync(new List<Person>());

            // Act
            var result = await _blogService.GetAccessibleBlogsAsync("user1");

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        #endregion

        #region GetFriendsBlogsAsync Tests

        [Fact]
        public async Task GetFriendsBlogsAsync_WithUserId_ReturnsFriendsBlogs()
        {
            // Arrange
            var userId = "user1";
            var friends = new List<Person>
                {
                    new Person {Id = "user2", FirstName = "test", LastName = "test", IsPrivate = false, Nationality = "poland"},
                    new Person {Id = "user3", FirstName = "test", LastName = "test", IsPrivate = false, Nationality = "poland"}
                };

            var blogs = new List<Blog>
                {
                    new Blog
                    {
                        Id = 1,
                        OwnerId = "user2",
                        Owner = new Person {Id = "user2", FirstName = "Owner2", LastName = "Test", IsPrivate = false, Nationality = "test"},
                        Visibility = BlogVisibility.ForMyFriends,
                        TripId = 1,
                        Name = "test",
                        Description = "Test description",
                        Catalog = "test",
                        Trip = new Trip { Id = 1, Name = "Trip1", PersonId = "user2" },
                        Posts = new List<Post>()
                    },
                    new Blog
                    {
                        Id = 2,
                        OwnerId = "user4",
                        Owner = new Person {Id = "user4", FirstName = "Owner4", LastName = "Test", IsPrivate = false, Nationality = "test"},
                        Visibility = BlogVisibility.ForMyFriends,
                        TripId = 2,
                        Name = "test",
                        Description = "Test description",
                        Catalog = "test",
                        Trip = new Trip { Id = 2, Name = "Trip2", PersonId = "user4" },
                        Posts = new List<Post>()
                    },
                    new Blog
                    {
                        Id = 3,
                        OwnerId = "user5",
                        Owner = new Person {Id = "user5", FirstName = "Owner5", LastName = "Test", IsPrivate = false, Nationality = "test"},
                        Visibility = BlogVisibility.Public,
                        TripId = 3,
                        Name = "test",
                        Description = "Test description",
                        Catalog = "test",
                        Trip = new Trip { Id = 3, Name = "Trip3", PersonId = "user5" },
                        Posts = new List<Post>()
                    }
                };

            _blogRepositoryMock
                .Setup(x => x.GetAllAsync())
                .ReturnsAsync(blogs);

            _blogRepositoryMock
                .Setup(x => x.GetByIdAsync(It.IsAny<int>()))
                .ReturnsAsync((int id) => blogs.FirstOrDefault(b => b.Id == id));

            _friendshipServiceMock
                .Setup(x => x.GetFriendsAsync(userId))
                .ReturnsAsync(friends);

            _friendshipServiceMock
                .Setup(x => x.IsFriendAsync(It.IsAny<string>(), userId))
                .ReturnsAsync(true);

            _tripParticipantRepositoryMock
                .Setup(x => x.ExistsAsync(It.IsAny<int>(), It.IsAny<string>()))
                .ReturnsAsync(false);

            _tripParticipantRepositoryMock
                .Setup(x => x.GetByTripIdAsync(It.IsAny<int>()))
                .ReturnsAsync(new List<TripParticipant>());

            _spotServiceMock
                .Setup(x => x.GetCountriesByTripAsync(It.IsAny<int>()))
                .ReturnsAsync(new List<Country>());

            _userManagerMock
                .Setup(x => x.FindByIdAsync(It.IsAny<string>()))
                .ReturnsAsync((string id) => new Person
                {
                    Id = id,
                    FirstName = "Test",
                    LastName = "User",
                    IsPrivate = false,
                    Nationality = "poland"
                });

            // Act
            var result = await _blogService.GetFriendsBlogsAsync(userId);

            // Assert
            // Oczekuje tylko bloga 1, bo tylko on spełnia warunki:
            // 1. Blog 1: właściciel (user2) jest na liście znajomych
            // 2. Blog 2: właściciel (user4) nie jest znajomym, więc nie spełnia warunku (isFriendOwner || isFriendParticipant)
            // 3. Blog 3: jest publiczny, ale właściciel nie jest znajomym i nie ma znajomych uczestników
            Assert.Single(result);
            Assert.Contains(result, b => b.Id == 1);
        }

        [Fact]
        public async Task GetFriendsBlogsAsync_WithFriendParticipant_IncludesBlog()
        {
            // Arrange
            var userId = "user1";
            var friends = new List<Person>
                {
                    new Person { Id = "user2", FirstName = "test", LastName = "test", IsPrivate = false, Nationality = "poland" }
                };

            var blogs = new List<Blog>
                {
                    new Blog
                    {
                        Id = 1,
                        OwnerId = "user3",
                        Owner = new Person { Id = "user3", FirstName = "Owner", LastName = "Test", IsPrivate = false, Nationality = "test" },
                        Visibility = BlogVisibility.ForTripParticipantsFriends,
                        TripId = 1,
                        Name = "test",
                        Description = "Test description",
                        Catalog = "test",
                        Trip = new Trip { Id = 1, Name = "Trip 1", PersonId = "user3" },
                        Posts = new List<Post>()
                    }
                };

            var participants = new List<TripParticipant>
                {
                    new TripParticipant { PersonId = "user2" }
                };

            _blogRepositoryMock
                .Setup(x => x.GetAllAsync())
                .ReturnsAsync(blogs);

            _blogRepositoryMock
                .Setup(x => x.GetByIdAsync(It.IsAny<int>()))
                .ReturnsAsync((int id) => blogs.FirstOrDefault(b => b.Id == id));

            _friendshipServiceMock
                .Setup(x => x.GetFriendsAsync(userId))
                .ReturnsAsync(friends);

            // Użytkownik jest znajomym uczestnika (user2)
            _friendshipServiceMock
                .Setup(x => x.IsFriendAsync("user2", userId))
                .ReturnsAsync(true);

            _tripParticipantRepositoryMock
                .Setup(x => x.GetByTripIdAsync(1))
                .ReturnsAsync(participants);

            _tripParticipantRepositoryMock
                .Setup(x => x.ExistsAsync(1, userId))
                .ReturnsAsync(false);

            _spotServiceMock
                .Setup(x => x.GetCountriesByTripAsync(It.IsAny<int>()))
                .ReturnsAsync(new List<Country>());

            _userManagerMock
                .Setup(x => x.FindByIdAsync(It.IsAny<string>()))
                .ReturnsAsync((string id) => new Person
                {
                    Id = id,
                    FirstName = "Test",
                    LastName = "User",
                    IsPrivate = false,
                    Nationality = "poland"
                });

            // Act
            var result = await _blogService.GetFriendsBlogsAsync(userId);

            // Assert
            Assert.Single(result);
            Assert.Contains(result, b => b.Id == 1);

            var blogDto = result[0];
            Assert.Equal(1, blogDto.Id);
            Assert.Equal(BlogVisibility.ForTripParticipantsFriends, blogDto.Visibility);
        }

        #endregion

        #region GetPublicBlogsAsync Tests

        [Fact]
        public async Task GetPublicBlogsAsync_ReturnsOnlyPublicBlogs()
        {
            // Arrange
            var publicBlogs = new List<Blog>
            {
                new Blog { Id = 1, Visibility = BlogVisibility.Public, TripId = 1, Name = "test", OwnerId = "test" },
                new Blog { Id = 2, Visibility = BlogVisibility.Public, TripId = 2, Name = "test", OwnerId = "test" }
            };

            _blogRepositoryMock
                .Setup(x => x.GetPublicBlogsWithDetailsAsync())
                .ReturnsAsync(publicBlogs);

            _spotServiceMock
                .Setup(x => x.GetCountriesByTripAsync(It.IsAny<int>()))
                .ReturnsAsync(new List<Country>());

            _userManagerMock
                .Setup(x => x.FindByIdAsync(It.IsAny<string>()))
                .ReturnsAsync(new Person {FirstName = "Test", LastName = "User", IsPrivate = false, Nationality = "poland" });

            // Act
            var result = await _blogService.GetPublicBlogsAsync();

            // Assert
            Assert.Equal(2, result.Count);
            Assert.All(result, blog => Assert.Equal(BlogVisibility.Public, blog.Visibility));
        }

        #endregion

        #region GetCountriesWithAccessibleBlogsAsync Tests

        [Fact]
        public async Task GetCountriesWithAccessibleBlogsAsync_WithUserId_ReturnsCountries()
        {
            // Arrange
            var userId = "user1";

            var blogs = new List<Blog>
            {
                new Blog
                {
                    Id = 1,
                    Name = "Blog 1",
                    Visibility = BlogVisibility.Public,
                    TripId = 1,
                    Trip = new Trip { Id = 1, Name = "Trip 1", PersonId = "test" },
                    OwnerId = "owner1",
                    Owner = new Person { Id = "owner1", FirstName = "John", LastName = "Doe", IsPrivate = false, Nationality = "test" },
                    Posts = new List<Post>()
                },
                new Blog
                {
                    Id = 2,
                    Name = "Blog 2",
                    Visibility = BlogVisibility.Public,
                    TripId = 2,
                    Trip = new Trip { Id = 2, Name = "Trip 2", PersonId = "test" },
                    OwnerId = "owner2",
                    Owner = new Person { Id = "owner2", FirstName = "Jane", LastName = "Smith", IsPrivate = false, Nationality = "test" },
                    Posts = new List<Post>()
                }
            };

            _blogRepositoryMock
                .Setup(x => x.GetAllAsync())
                .ReturnsAsync(blogs);

            var blogService = new BlogService(
                _blogRepositoryMock.Object,
                _tripParticipantRepositoryMock.Object,
                _friendshipServiceMock.Object,
                _spotServiceMock.Object,
                _userManagerMock.Object);

            _spotServiceMock
                .Setup(x => x.GetCountriesByTripAsync(1))
                .ReturnsAsync(new List<Country>
                {
                    new Country { Code = "US", Name = "USA" },
                    new Country { Code = "PL", Name = "Poland" }
                });

            _spotServiceMock
                .Setup(x => x.GetCountriesByTripAsync(2))
                .ReturnsAsync(new List<Country>
                {
                    new Country { Code = "US", Name = "USA" },
                    new Country { Code = "DE", Name = "Germany" }
                });

            _friendshipServiceMock
                .Setup(x => x.GetFriendsAsync(userId))
                .ReturnsAsync(new List<Person>());

            _tripParticipantRepositoryMock
                .Setup(x => x.GetByTripIdAsync(It.IsAny<int>()))
                .ReturnsAsync(new List<TripParticipant>());

            // Act
            var result = await blogService.GetCountriesWithAccessibleBlogsAsync(userId);

            // Assert
            Assert.Equal(3, result.Count);
            Assert.Contains(result, c => c.Code == "US" && c.BlogCount == 2);
            Assert.Contains(result, c => c.Code == "PL" && c.BlogCount == 1);
            Assert.Contains(result, c => c.Code == "DE" && c.BlogCount == 1);
        }

        [Fact]
        public async Task GetCountriesWithAccessibleBlogsAsync_WithoutUserId_ReturnsPublicBlogsCountries()
        {
            // Arrange
            var blogs = new List<Blog>
            {
                new Blog
                {
                    Id = 1,
                    Name = "Blog 1",
                    Visibility = BlogVisibility.Public,
                    TripId = 1,
                    Trip = new Trip { Id = 1, Name = "Trip 1", PersonId = "test" },
                    OwnerId = "owner1",
                    Owner = new Person { Id = "owner1", FirstName = "John", LastName = "Doe", IsPrivate = false, Nationality = "test" },
                    Posts = new List<Post>()
                }
            };

            _blogRepositoryMock
                .Setup(x => x.GetAllAsync())
                .ReturnsAsync(blogs);

            _spotServiceMock
                .Setup(x => x.GetCountriesByTripAsync(1))
                .ReturnsAsync(new List<Country>
                {
            new Country { Code = "FR", Name = "France" }
                });

            _friendshipServiceMock
                .Setup(x => x.GetFriendsAsync(It.IsAny<string>()))
                .ReturnsAsync(new List<Person>());

            _tripParticipantRepositoryMock
                .Setup(x => x.GetByTripIdAsync(It.IsAny<int>()))
                .ReturnsAsync(new List<TripParticipant>());

            var blogService = new BlogService(
                _blogRepositoryMock.Object,
                _tripParticipantRepositoryMock.Object,
                _friendshipServiceMock.Object,
                _spotServiceMock.Object,
                _userManagerMock.Object);

            // Act
            var result = await blogService.GetCountriesWithAccessibleBlogsAsync();

            // Assert
            Assert.Single(result);
            Assert.Contains(result, c => c.Code == "FR");
        }

        #endregion

        #region GetCountriesWithPublicBlogsAsync Tests

        [Fact]
        public async Task GetCountriesWithPublicBlogsAsync_ReturnsPublicBlogsCountries()
        {
            // Arrange
            var blogs = new List<Blog>
            {
                new Blog
                {
                    Id = 1,
                    Name = "Blog Italy",
                    Visibility = BlogVisibility.Public,
                    TripId = 1,
                    Trip = new Trip { Id = 1, Name = "Italian Trip", PersonId = "test" },
                    OwnerId = "owner1",
                    Owner = new Person { Id = "owner1", FirstName = "John", LastName = "Doe", IsPrivate = false, Nationality = "test" },
                    Posts = new List<Post>()
                }
            };

            _blogRepositoryMock
                .Setup(x => x.GetAllAsync())
                .ReturnsAsync(blogs);

            _spotServiceMock
                .Setup(x => x.GetCountriesByTripAsync(1))
                .ReturnsAsync(new List<Country>
                {
                    new Country { Code = "IT", Name = "Italy" }
                });

            // Mockowanie GetFriendsAsync - zwraca pustą listę (nie ma zalogowanego użytkownika)
            _friendshipServiceMock
                .Setup(x => x.GetFriendsAsync(It.IsAny<string>()))
                .ReturnsAsync(new List<Person>());

            _tripParticipantRepositoryMock
                .Setup(x => x.GetByTripIdAsync(It.IsAny<int>()))
                .ReturnsAsync(new List<TripParticipant>());

            var blogService = new BlogService(
                _blogRepositoryMock.Object,
                _tripParticipantRepositoryMock.Object,
                _friendshipServiceMock.Object,
                _spotServiceMock.Object,
                _userManagerMock.Object);

            // Act
            var result = await blogService.GetCountriesWithPublicBlogsAsync();

            // Assert
            Assert.Single(result);
            Assert.Contains(result, c => c.Code == "IT" && c.Name == "Italy");
            Assert.Equal(1, result[0].BlogCount);
        }

        #endregion

        #region Private Methods Tests (via reflection if needed)

        [Fact]
        public async Task IsFriendParticipantInTripAsync_WithFriendParticipant_ReturnsTrue()
        {
            // Arrange
            var tripId = 1;
            var friendIds = new List<string> { "user2", "user3" };
            var participants = new List<TripParticipant>
            {
                new TripParticipant { PersonId = "user1" },
                new TripParticipant { PersonId = "user2" }
            };

            _tripParticipantRepositoryMock
                .Setup(x => x.GetByTripIdAsync(tripId))
                .ReturnsAsync(participants);

            // Use reflection to test private method
            var method = typeof(BlogService).GetMethod("IsFriendParticipantInTripAsync",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            var task = (Task<bool>)method.Invoke(_blogService, new object[] { tripId, friendIds });
            var result = await task;

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task IsFriendParticipantInTripAsync_WithoutFriendParticipant_ReturnsFalse()
        {
            // Arrange
            var tripId = 1;
            var friendIds = new List<string> { "user4", "user5" };
            var participants = new List<TripParticipant>
            {
                new TripParticipant { PersonId = "user1" },
                new TripParticipant { PersonId = "user2" }
            };

            _tripParticipantRepositoryMock
                .Setup(x => x.GetByTripIdAsync(tripId))
                .ReturnsAsync(participants);

            // Use reflection to test private method
            var method = typeof(BlogService).GetMethod("IsFriendParticipantInTripAsync",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            var task = (Task<bool>)method.Invoke(_blogService, new object[] { tripId, friendIds });
            var result = await task;

            // Assert
            Assert.False(result);
        }

        #endregion
    }
}