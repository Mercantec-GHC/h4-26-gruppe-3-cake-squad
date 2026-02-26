using Commons.Models.Database;
using Commons.Models.Dtos;

namespace Wavelength.Services
{
	public interface INotificationService
	{
		public Task AdminCreateNotificationAsync(NotificationRequestDto request);
		public Task CreateMessageNotificationAsync(MessageNotificationRequestDto request);
		public Task<List<NotificationResponseDto>> GetNotificationsAsync(string userId);
		public Task<int> GetNotificationCountAsync(string userId);
		public Task RemoveNotificationAsync(string notificationId, User user);
	}
}