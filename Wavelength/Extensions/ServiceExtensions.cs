using Wavelength.Services;

namespace Microsoft.Extensions.DependencyInjection
{
	/// <summary>
	/// Provides extension methods for registering and configuring core Wavelength application services and middleware in a
	/// web application.
	/// </summary>
	/// <remarks>This static class centralizes the setup of essential services and middleware required by the
	/// Wavelength application, including authentication, CORS policies, database access, API documentation, email, and
	/// encryption. Its methods should be invoked during application startup to ensure all necessary components are
	/// properly registered and configured for dependency injection and middleware execution.</remarks>
	public static class ServiceExtensions
	{
		/// <summary>
		/// Adds and configures core Wavelength application services, including authentication, CORS policies, database
		/// access, API documentation, email, and encryption services, to the specified service collection.
		/// </summary>
		/// <remarks>This method centralizes the registration of essential services required for the Wavelength
		/// application. It should be called during application startup to ensure that authentication, CORS, database, email,
		/// and encryption services are properly configured and available for dependency injection throughout the
		/// application.</remarks>
		/// <param name="services">The service collection to which the Wavelength services will be added. Must not be null.</param>
		/// <param name="configuration">The application configuration used to set up authentication, CORS, database access, and other services. Must not
		/// be null.</param>
		/// <returns>The same IServiceCollection instance with all required Wavelength services registered.</returns>
		public static IServiceCollection AddWavelengthServices(this IServiceCollection services, IConfiguration configuration)
		{
			// Configure JWT Authentication
			services.AddJwtAuthentication(configuration);

			// Configure CORS policies
			services.AddCorsPolicy(configuration);

			// Add database access services, including DbContext and repositories
			services.AddDatabaseAccess(configuration);

			// Add swagger gen.
			services.AddSwaggerGenWithAuth();

			// Add jwt service
			services.AddScoped<JwtService>();

			// Add email services
			services.AddSingleton<IEmailTemplateLoader, EmailTemplateLoader>();
			services.AddScoped<MailService>();

			// Add application services
			services.AddScoped<AuthService>();
			services.AddScoped<NotificationService>();

			// Register AesEncryptionService.
			services.AddSingleton<AesEncryptionService>();

			return services;
		}

		/// <summary>
		/// Configures the specified web application to use Wavelength services, including Swagger documentation, static file
		/// serving, CORS policies, and Google OAuth integration.
		/// </summary>
		/// <remarks>This method enables Swagger UI for API documentation, serves static files required by Swagger,
		/// configures CORS policies based on the application's environment, and sets up Google OAuth authentication. It
		/// should be called during application startup to ensure all middleware and services are properly
		/// registered.</remarks>
		/// <param name="app">The web application instance to configure with Wavelength services.</param>
		/// <param name="configuration">The configuration settings used to map Google OAuth for the application.</param>
		/// <returns>The configured web application instance with Wavelength services enabled.</returns>
		public static WebApplication UseWavelengthServices(this WebApplication app, IConfiguration configuration)
		{
			// Enable middleware to serve generated Swagger as a JSON endpoint and the Swagger UI
			app.UseSwagger();
			app.UseSwaggerUI(options =>
			{
				options.SwaggerEndpoint("/swagger/v1/swagger.json", "v1");
				options.RoutePrefix = "swagger";
				options.AddSwaggerBootstrap().AddExperimentalFeatures();
				options.InjectJavascript("/swagger/login.js");
			});

			// Enable static files to support swagger bootstrap
			app.UseStaticFiles();

			// Enable CORS
			app.UseCors(app.Environment.IsDevelopment() ? "AllowAllLocalhost" : "AllowFlutterApp");

			// Map google OAuth configuration.
			app.MapGoogleOAuth(configuration);

			return app;
		}

		/// <summary>
		/// Maps an endpoint to the web application that provides Google OAuth configuration details in JSON format.
		/// </summary>
		/// <remarks>The mapped endpoint responds to requests at "/oauth/google.json" with a JSON object containing
		/// the Google OAuth Client ID and Redirect URI. This can be used by client applications to initiate the OAuth flow
		/// without exposing sensitive configuration in client-side code.</remarks>
		/// <param name="app">The web application instance to which the Google OAuth endpoint will be added.</param>
		/// <param name="configuration">The configuration source used to retrieve the Google OAuth Client ID and Redirect URI.</param>
		/// <returns>The web application instance with the Google OAuth endpoint mapped.</returns>
		public static WebApplication MapGoogleOAuth(this WebApplication app, IConfiguration configuration)
		{
			app.Map("/oauth/google.json", appBuilder =>
			{
				appBuilder.Run(async conetxt =>
				{
					var jsonObject = new { ClientId = configuration["Oauth:Google:ClientId"], ReturnUri = configuration["Oauth:Google:RedirectUri"] };
					var jsonString = System.Text.Json.JsonSerializer.Serialize(jsonObject);
					conetxt.Response.ContentType = "application/json";
					await conetxt.Response.WriteAsync(jsonString);
				});
			});

			return app;
		}
	}
}