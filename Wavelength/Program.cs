using Microsoft.EntityFrameworkCore;
using Wavelength.Services;

namespace Wavelength
{
	public class Program
    {
		public static void Main(string[] args)
		{
			var builder = WebApplication.CreateBuilder(args);

            // Add controller services
            builder.Services.AddControllers();

			// Add OpenAPI/Swagger support
			builder.Services.AddOpenApi();

			// Add application services and configurations
			builder.Services.AddWavelengthServices(builder.Configuration);

			// Add health checks
			builder.Services.AddHealthChecks();

			var app = builder.Build();

			// Configure the HTTP request pipeline.
			if (app.Environment.IsDevelopment())
			{
				app.MapOpenApi();
			}

			// Use custom application services and middleware
			app.UseWavelengthServices(builder.Configuration);

			// Enable HTTPS redirection
			app.UseHttpsRedirection();

			// Enable authentication and authorization
			app.UseAuthentication();
			app.UseAuthorization();

			// Map controller routes
			app.MapControllers();

			// Add health checks endpoint
			app.MapHealthChecks("/health");

			app.Run();
		}
	}
}