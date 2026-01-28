namespace Wavelength.Extensions.DependencyInjection
{
	public static class Cors
	{
		public static IServiceCollection AddCorsPolicy(this IServiceCollection service, IConfiguration configuration)
		{
			var origins = configuration
				.GetSection("Cors:AllowedOrigins")
				.Get<string[]>();

			if (origins == null || origins.Length == 0)
			{
				throw new InvalidOperationException(
					"CORS configuration missing: Cors:AllowedOrigins must contain at least one origin.");
			}

			service.AddCors(options =>
			{
				options.AddPolicy("AllowFlutterApp", policy =>
				{
					policy.WithOrigins(origins)
						.AllowAnyHeader()
						.AllowAnyMethod()
						.AllowCredentials();
				});

				options.AddPolicy("AllowAllLocalhost", policy =>
				{
					policy.SetIsOriginAllowed(origin =>
					{
						var uri = new Uri(origin);
						return uri.Host == "localhost" ||
							uri.Host == "127.0.0.1" ||
							uri.Host == "0.0.0.0";
					})
					.AllowAnyHeader()
					.AllowAnyMethod()
					.AllowCredentials();
				});
			});
			return service;
		}
	}
}