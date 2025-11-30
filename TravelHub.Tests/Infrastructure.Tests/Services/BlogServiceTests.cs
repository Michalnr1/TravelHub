using Moq;
using Microsoft.AspNetCore.Identity;
using TravelHub.Domain.DTOs;
using TravelHub.Domain.Entities;
using TravelHub.Domain.Interfaces.Repositories;
using TravelHub.Domain.Interfaces.Services;
using TravelHub.Infrastructure.Services;

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
                new Blog { Id = 1, Visibility = BlogVisibility.Public, TripId = 1, Name = "test", OwnerId = "test" },
                new Blog { Id = 2, Visibility = BlogVisibility.Private, TripId = 2, Name = "test", OwnerId = "test" },
                new Blog { Id = 3, Visibility = BlogVisibility.ForMyFriends, TripId = 3, OwnerId = "user2", Name = "test"}
            };

            _blogRepositoryMock
                .Setup(x => x.GetAllAsync())
                .ReturnsAsync(blogs);

            _friendshipServiceMock
                .Setup(x => x.IsFriendAsync("user2", userId))
                .ReturnsAsync(true);

            _spotServiceMock
                .Setup(x => x.GetCountriesByTripAsync(It.IsAny<int>()))
                .ReturnsAsync(new List<Country>());

            _userManagerMock
                .Setup(x => x.FindByIdAsync(It.IsAny<string>()))
                .ReturnsAsync(new Person { FirstName = "Test", LastName = "User", IsPrivate = false, Nationality = "poland" });

            // Act
            var result = await _blogService.GetAccessibleBlogsAsync(userId);

            // Assert
            Assert.Equal(2, result.Count); // Public and ForMyFriends (user is friend)
            Assert.Contains(result, b => b.Id == 1);
            Assert.Contains(result, b => b.Id == 3);
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
                .ReturnsAsync((List<Blog>)null);

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
                new Blog { Id = 1, OwnerId = "user2", Visibility = BlogVisibility.ForMyFriends, TripId = 1, Name = "test" },
                new Blog { Id = 2, OwnerId = "user4", Visibility = BlogVisibility.ForMyFriends, TripId = 2, Name = "test" },
                new Blog { Id = 3, OwnerId = "user5", Visibility = BlogVisibility.Public, TripId = 3, Name = "test" }
            };

            _blogRepositoryMock
                .Setup(x => x.GetAllAsync())
                .ReturnsAsync(blogs);

            _friendshipServiceMock
                .Setup(x => x.GetFriendsAsync(userId))
                .ReturnsAsync(friends);

            _friendshipServiceMock
                .Setup(x => x.IsFriendAsync(It.IsAny<string>(), userId))
                .ReturnsAsync(true);

            _tripParticipantRepositoryMock
                .Setup(x => x.GetByTripIdAsync(It.IsAny<int>()))
                .ReturnsAsync(new List<TripParticipant>());

            _spotServiceMock
                .Setup(x => x.GetCountriesByTripAsync(It.IsAny<int>()))
                .ReturnsAsync(new List<Country>());

            _userManagerMock
                .Setup(x => x.FindByIdAsync(It.IsAny<string>()))
                .ReturnsAsync(new Person { FirstName = "Test", LastName = "User", IsPrivate = false, Nationality = "poland" });

            // Act
            var result = await _blogService.GetFriendsBlogsAsync(userId);

            // Assert
            Assert.Equal(2, result.Count); // Blog 1 (owner is friend) and Blog 3 (public)
            Assert.Contains(result, b => b.Id == 1);
            Assert.Contains(result, b => b.Id == 3);
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
                new Blog { Id = 1, OwnerId = "user3", Visibility = BlogVisibility.ForTripParticipantsFriends, TripId = 1, Name = "test" }
            };
            var participants = new List<TripParticipant>
            {
                new TripParticipant { PersonId = "user2" }
            };

            _blogRepositoryMock
                .Setup(x => x.GetAllAsync())
                .ReturnsAsync(blogs);

            _friendshipServiceMock
                .Setup(x => x.GetFriendsAsync(userId))
                .ReturnsAsync(friends);

            _friendshipServiceMock
                .Setup(x => x.IsFriendAsync(It.IsAny<string>(), userId))
                .ReturnsAsync(true);

            _tripParticipantRepositoryMock
                .Setup(x => x.GetByTripIdAsync(1))
                .ReturnsAsync(participants);

            _spotServiceMock
                .Setup(x => x.GetCountriesByTripAsync(It.IsAny<int>()))
                .ReturnsAsync(new List<Country>());

            _userManagerMock
                .Setup(x => x.FindByIdAsync(It.IsAny<string>()))
                .ReturnsAsync(new Person {FirstName = "Test", LastName = "User", IsPrivate = false, Nationality = "poland" });

            // Act
            var result = await _blogService.GetFriendsBlogsAsync(userId);

            // Assert
            Assert.Single(result);
            Assert.Contains(result, b => b.Id == 1);
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
            var accessibleBlogs = new List<PublicBlogInfoDto>
            {
                new PublicBlogInfoDto
                {
                    Countries = new List<Country>
                    {
                        new Country { Code = "US", Name = "USA" },
                        new Country { Code = "PL", Name = "Poland" }
                    }
                },
                new PublicBlogInfoDto
                {
                    Countries = new List<Country>
                    {
                        new Country { Code = "US", Name = "USA" },
                        new Country { Code = "DE", Name = "Germany" }
                    }
                }
            };

            // Mock the internal call to GetAccessibleBlogsAsync
            var blogServiceMock = new Mock<BlogService>(
                _blogRepositoryMock.Object,
                _tripParticipantRepositoryMock.Object,
                _friendshipServiceMock.Object,
                _spotServiceMock.Object,
                _userManagerMock.Object);

            blogServiceMock
                .Setup(x => x.GetAccessibleBlogsAsync(userId))
                .ReturnsAsync(accessibleBlogs);

            // Act
            var result = await blogServiceMock.Object.GetCountriesWithAccessibleBlogsAsync(userId);

            // Assert
            Assert.Equal(3, result.Count); // USA, Poland, Germany
            Assert.Contains(result, c => c.Code == "US" && c.BlogCount == 2);
            Assert.Contains(result, c => c.Code == "PL" && c.BlogCount == 1);
            Assert.Contains(result, c => c.Code == "DE" && c.BlogCount == 1);
        }

        [Fact]
        public async Task GetCountriesWithAccessibleBlogsAsync_WithoutUserId_ReturnsPublicBlogsCountries()
        {
            // Arrange
            var accessibleBlogs = new List<PublicBlogInfoDto>
            {
                new PublicBlogInfoDto
                {
                    Countries = new List<Country>
                    {
                        new Country { Code = "FR", Name = "France" }
                    }
                }
            };

            var blogServiceMock = new Mock<BlogService>(
                _blogRepositoryMock.Object,
                _tripParticipantRepositoryMock.Object,
                _friendshipServiceMock.Object,
                _spotServiceMock.Object,
                _userManagerMock.Object);

            blogServiceMock
                .Setup(x => x.GetAccessibleBlogsAsync(null))
                .ReturnsAsync(accessibleBlogs);

            // Act
            var result = await blogServiceMock.Object.GetCountriesWithAccessibleBlogsAsync();

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
            var accessibleBlogs = new List<PublicBlogInfoDto>
            {
                new PublicBlogInfoDto
                {
                    Countries = new List<Country>
                    {
                        new Country { Code = "IT", Name = "Italy" }
                    }
                }
            };

            var blogServiceMock = new Mock<BlogService>(
                _blogRepositoryMock.Object,
                _tripParticipantRepositoryMock.Object,
                _friendshipServiceMock.Object,
                _spotServiceMock.Object,
                _userManagerMock.Object);

            blogServiceMock
                .Setup(x => x.GetAccessibleBlogsAsync(null))
                .ReturnsAsync(accessibleBlogs);

            // Act
            var result = await blogServiceMock.Object.GetCountriesWithPublicBlogsAsync();

            // Assert
            Assert.Single(result);
            Assert.Contains(result, c => c.Code == "IT");
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