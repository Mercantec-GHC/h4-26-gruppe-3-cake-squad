using Microsoft.OpenApi;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Provides extension methods for configuring Swagger generation with JWT Bearer authentication support in an
    /// ASP.NET Core application.
    /// </summary>
    public static class SwaggerGen
    {
        /// <summary>
        /// Adds and configures Swagger generation with JWT Bearer authentication support for the API documentation.
        /// </summary>
        /// <remarks>This method configures Swagger to include API documentation and sets up JWT Bearer
        /// authentication in the Swagger UI, allowing users to authorize requests using a JWT token. It also includes
        /// XML comments in the generated documentation if available.</remarks>
        /// <param name="services">The <see cref="IServiceCollection"/> to which the Swagger generation services are added.</param>
        /// <returns>The same <see cref="IServiceCollection"/> instance so that additional calls can be chained.</returns>
        public static IServiceCollection AddSwaggerGenWithAuth(this IServiceCollection services)
        {
            services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v1", new OpenApiInfo
                {
                    Version = "v1",
                    Title = "Wavelength API",
                    Description = "An ASP.NET Core Web API for Wavelength application"
                });

                // Configures the Swagger Login screen.
                options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    In = ParameterLocation.Header,
                    Description = "Insert your token here. A token can be obtained from \"/auth/login\" using a username and a password or from \"/auth/refresh\" using a short lived token issued by the server.",
                    Name = "Authorization",
                    Type = SecuritySchemeType.Http,
                    BearerFormat = "JWT",
                    Scheme = "Bearer"
                });

                // Inkluder XML kommentarer
                var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                if (File.Exists(xmlPath))
                {
                    options.IncludeXmlComments(xmlPath);
                }

                options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
                {
                    [new OpenApiSecuritySchemeReference("Bearer", document)] = []
                });
            });

            return services;
        }
    }
}
