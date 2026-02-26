using Commons.Enums;
using Commons.Models.Database;
using Commons.Models.Dtos;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Moq;
using System.Runtime.CompilerServices;
using Wavelength.Data;
using Wavelength.Services;

namespace WavelengthUnitTest
{
	public class ChatServiceTests
	{
		private AppDbContext CreateInMemoryDbContext()
		{
			var options = new DbContextOptionsBuilder<AppDbContext>()
				.UseInMemoryDatabase(Guid.NewGuid().ToString())
				.Options;

			return new AppDbContext(options);
		}

		#region Helper functions for tests

		private User CreateUser(string id, bool isAdmin = false)
		{
			return new User
			{
				Id = id,
				FirstName = "Test",
				LastName = "Test",
				Birthday = new DateOnly(1990, 1, 1),
				Email = $"{id}@test.com",
				HashedPassword = "hashedpassword",
				UserRoles = isAdmin ? new List<UserRole> { new UserRole { Role = RoleEnum.Admin } } : new List<UserRole>(),
				UserVisibilities = new List<UserVisibility>()
			};
		}

		private UserVisibility CreateUserVisibility(string sourceUserId, string targetUserId, UserVisibilityEnum visibility)
		{
			return new UserVisibility
			{
				SourceUserId = sourceUserId,
				TargetUserId = targetUserId,
				Visibility = visibility
			};
		}

		private ChatRoom CreateChatRoom(string id, string name)
		{
			return new ChatRoom
			{
				Id = id,
				Name = name
			};
		}

		private Participant CreateParticipant(string userId, string chatRoomId)
		{
			return new Participant
			{
				UserId = userId,
				ChatRoomId = chatRoomId
			};
		}

		#endregion

		#region CreateChatRoomAsync service function tests

		[Fact]
		public async Task CreateChatRoomAsync_WhenValidInput()
		{
			// Arrange
			var context = CreateInMemoryDbContext();
			var aesMock = new Mock<IAesEncryptionService>();
			var notificationMock = new Mock<INotificationService>();

			var creator = CreateUser("creator");
			var creatorUserVisibility1 = CreateUserVisibility(creator.Id, "user1", UserVisibilityEnum.Visible);
			var creatorUserVisibility2 = CreateUserVisibility(creator.Id, "user2", UserVisibilityEnum.Dismissed);

			var user1 = CreateUser("user1");
			var user2 = CreateUser("user2");

			context.Users.AddRange(creator, user1, user2);
			context.UserVisibilities.AddRange(creatorUserVisibility1, creatorUserVisibility2);
			context.SaveChanges();

			var service = new ChatService(context, aesMock.Object, notificationMock.Object);

			var dto = new ChatRoomCreateDto
			{
				RoomName = "Test Chat Room",
				ParticipantIds = new List<string> { "user1", "user2" }
			};

			// Act
			await service.CreateChatRoomAsync(dto, creator);

			// Assert
			var chatRoom = context.ChatRooms
				.Include(cr => cr.Participants)
				.FirstOrDefault();

			Assert.NotNull(chatRoom);
			Assert.Equal("Test Chat Room", chatRoom.Name);

			var participantIds = chatRoom.Participants.Select(p => p.UserId).ToList();

			Assert.Contains(creator.Id, participantIds);
			Assert.Contains(user1.Id, participantIds);

			Assert.Equal(2, participantIds.Count());
		}

		[Fact]
		public async Task CreateChatRoomAsync_ThrowKeysNotFound_WhenUserDoesNotExist()
		{
			// Arrange
			var context = CreateInMemoryDbContext();
			var aesMock = new Mock<IAesEncryptionService>();
			var notificationMock = new Mock<INotificationService>();

			var creator = CreateUser("creator");

			context.Users.Add(creator);
			context.SaveChanges();

			var service = new ChatService(context, aesMock.Object, notificationMock.Object);

			var dto = new ChatRoomCreateDto
			{
				RoomName = "Test Chat Room",
				ParticipantIds = new List<string> { "nonexistentuser" }
			};

			// Act & Assert
			await Assert.ThrowsAsync<KeyNotFoundException>(() => service.CreateChatRoomAsync(dto, creator));
		}

		[Fact]
		public async Task CreateChatRoomAsync_ThrowInvalidOperationException_WhenNoVisibleUser()
		{
			// Arrange
			var context = CreateInMemoryDbContext();
			var aesMock = new Mock<IAesEncryptionService>();
			var notificationMock = new Mock<INotificationService>();

			var creator = CreateUser("creator");
			var creatorUserVisibility = CreateUserVisibility(creator.Id, "user1", UserVisibilityEnum.Dismissed);

			var user1 = CreateUser("user1");

			context.Users.AddRange(creator, user1);
			context.UserVisibilities.Add(creatorUserVisibility);
			context.SaveChanges();

			var service = new ChatService(context, aesMock.Object, notificationMock.Object);

			var dto = new ChatRoomCreateDto
			{
				RoomName = "Test Chat Room",
				ParticipantIds = new List<string> { "user1" }
			};

			// Act & Assert
			await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateChatRoomAsync(dto, creator));
		}
		#endregion

		#region GetAllAsync service function tests

		[Fact]
		public async Task GetAllAsync_ReturnsChatRooms_WhenTheyExist()
		{
			// Arrange
			var context = CreateInMemoryDbContext();
			var aesMock = new Mock<IAesEncryptionService>();
			var notificationMock = new Mock<INotificationService>();

			var user1 = CreateUser("user1");
			var user2 = CreateUser("user2");
			context.Users.AddRange(user1, user2);

			var chatRoom = CreateChatRoom("chatroom1", "Test Chat Room");
			context.ChatRooms.Add(chatRoom);

			var participant1 = CreateParticipant(user1.Id, chatRoom.Id);
			var participant2 = CreateParticipant(user2.Id, chatRoom.Id);
			context.Participants.AddRange(participant1, participant2);

			context.SaveChanges();

			var service = new ChatService(context, aesMock.Object, notificationMock.Object);

			// Act 
			var result = await service.GetAllAsync();

			// Assert
			Assert.NotNull(result);
			Assert.Single(result);
			Assert.Equal("chatroom1", result[0].Id);
			Assert.Equal("Test Chat Room", result[0].Name);
			Assert.Equal(new List<string> { "user1", "user2" }, result[0].Participants);
		}

		[Fact]
		public async Task GetAllAsync_ThrowsKeyNotFound_WhenNoChatRoomsExist()
		{
			// Arrange
			var context = CreateInMemoryDbContext();
			var aesMock = new Mock<IAesEncryptionService>();
			var notificationMock = new Mock<INotificationService>();

			var service = new ChatService(context, aesMock.Object, notificationMock.Object);

			// Act & Assert
			await Assert.ThrowsAsync<KeyNotFoundException>(() => service.GetAllAsync());
		}

		#endregion

		#region GetChatRoomByIdAsync service function tests

		[Fact]
		public async Task GetChatRoomByIdAsync_ReturnsChatRoomResponseDto_WhenUserIsAdmin()
		{
			// Arrange
			var context = CreateInMemoryDbContext();
			var aesMock = new Mock<IAesEncryptionService>();
			var notificationMock = new Mock<INotificationService>();

			var admin = CreateUser("admin", isAdmin: true);
			var user1 = CreateUser("user1");
			context.Users.AddRange(admin,user1);

			var chatRoom = CreateChatRoom("chatroom1", "Test Chat Room");
			context.ChatRooms.Add(chatRoom);

			var participant1 = CreateParticipant(user1.Id, chatRoom.Id);
			context.Participants.Add(participant1);

			context.SaveChanges();

			var service = new ChatService(context, aesMock.Object, notificationMock.Object);

			// Act
			var result = await service.GetChatRoomByIdAsync(chatRoom.Id, admin);

			// Assert
			Assert.NotNull(result);
			Assert.Equal(chatRoom.Id, result.Id);
			Assert.Equal(chatRoom.Name, result.Name);
			Assert.Equal(new List<string> { user1.Id }, result.Participants);
		}

		[Fact]
		public async Task GetChatRoomByIdAsync_ReturnsChatRoomResponseDto_WhenUserIsParticipant()
		{
			// Arrange
			var context = CreateInMemoryDbContext();
			var aesMock = new Mock<IAesEncryptionService>();
			var notificationMock = new Mock<INotificationService>();

			var user1 = CreateUser("user1");
			context.Users.Add(user1);

			var chatRoom = CreateChatRoom("chatroom1", "Test Chat Room");
			context.ChatRooms.Add(chatRoom);

			var participant1 = CreateParticipant(user1.Id, chatRoom.Id);
			context.Participants.Add(participant1);

			context.SaveChanges();

			var service = new ChatService(context, aesMock.Object, notificationMock.Object);

			// Act
			var result = await service.GetChatRoomByIdAsync(chatRoom.Id, user1);

			// Assert
			Assert.NotNull(result);
			Assert.Equal(chatRoom.Id, result.Id);
			Assert.Equal(chatRoom.Name, result.Name);
			Assert.Equal(new List<string> { user1.Id }, result.Participants);
		}

		[Fact]
		public async Task GetChatRoomByIdAsync_ThrowsUnauthorizedAccessException_WhenUserIsNotParticipant()
		{
			// Arrange
			var context = CreateInMemoryDbContext();
			var aesMock = new Mock<IAesEncryptionService>();
			var notificationMock = new Mock<INotificationService>();

			var user1 = CreateUser("user1");
			context.Users.Add(user1);

			var chatRoom = CreateChatRoom("chatroom1", "Test Chat Room");
			context.ChatRooms.Add(chatRoom);

			context.SaveChanges();

			var service = new ChatService(context, aesMock.Object, notificationMock.Object);

			// Act & Assert
			await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.GetChatRoomByIdAsync(chatRoom.Id, user1));
		}

		[Fact]
		public async Task GetChatRoomByIdAsync_ThrowsKeyNotFoundException_WhenChatRoomDoesNotExist()
		{
			// Arrange
			var context = CreateInMemoryDbContext();
			var aesMock = new Mock<IAesEncryptionService>();
			var notificationMock = new Mock<INotificationService>();

			var admin = CreateUser("admin", isAdmin: true);
			context.Users.Add(admin);
			context.SaveChanges();

			var service = new ChatService(context, aesMock.Object, notificationMock.Object);

			// Act & Assert
			await Assert.ThrowsAsync<KeyNotFoundException>(() => service.GetChatRoomByIdAsync("nonexistentchatroom", admin));
		}

		#endregion
	}
}