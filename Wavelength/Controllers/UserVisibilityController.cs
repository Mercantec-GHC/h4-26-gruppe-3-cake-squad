using Commons.Enums;
using Commons.Models.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Wavelength.Data;

namespace Wavelength.Controllers
{
	[ApiController]
	[Route("[controller]")]
	public class UserVisibilityController : BaseController
	{
		public UserVisibilityController(AppDbContext dbContext) : base(dbContext) { }

		[HttpPost, Authorize]
		public async Task<ActionResult> SetUserVisibilityAsync(UserVisibilityRequestDto dto)
		{
			if (dto == null) return BadRequest("Request body can not be null.");
			if (string.IsNullOrWhiteSpace(dto.TargetUserId)) return BadRequest("Target user id can not be empty.");
			

			return Ok();
		}
	}
}
