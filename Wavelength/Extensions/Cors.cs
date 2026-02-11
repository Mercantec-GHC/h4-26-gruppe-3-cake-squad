namespace Microsoft.Extensions.DependencyInjection
{
	public static class Cors
	{
		public static IServiceCollection AddCorsPolicy(this IServiceCollection service, IConfiguration configuration)
		{
            // Load allowed origins from configuration
            var origins = configuration["Cors:AllowedOrigins"]
				?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

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

                        if (uri.Host == "localhost" ||
							uri.Host == "127.0.0.1" ||
							uri.Host == "0.0.0.0")
                            return true;

                        foreach (var o in origins)
                        {
                            if (Uri.TryCreate(o, UriKind.Absolute, out var allowedUri))
                            {
                                if (allowedUri.Host.Equals(uri.Host, StringComparison.OrdinalIgnoreCase))
                                    return true;
                            }
                        }

                        return false;
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