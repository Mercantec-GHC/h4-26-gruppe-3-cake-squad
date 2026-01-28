using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;
using Wavelength.Data;
using Wavelength.Extensions.DependencyInjection;
using Wavelength.Services;

namespace Wavelength
{
	public class Program
    {
		public static void Main(string[] args)
		{
			var builder = WebApplication.CreateBuilder(args);

			// Add services to the container.
			builder.Services.AddControllers();

			// Add OpenAPI/Swagger support
			builder.Services.AddOpenApi();

            // Configure JWT Authentication
            builder.Services.AddJwtAuthentication(builder.Configuration);

			// Configure CORS policies
			builder.Services.AddCorsPolicy(builder.Configuration);

			// Configure DbContext with PostgreSQL
			builder.Services.AddDbContext<AppDbContext>(
				options => options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

			// Add swagger gen.
			//builder.Services.AddEndpointsApiExplorer();
			builder.Services.AddSwaggerGen(options =>
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

            });

            // Register JwtService
            builder.Services.AddScoped<JwtService>();

            // Add health checks
            builder.Services.AddHealthChecks();

			var app = builder.Build();

			// Configure the HTTP request pipeline.
			if (app.Environment.IsDevelopment())
			{
				app.MapOpenApi();
			}

			// Enable middleware to serve generated Swagger as a JSON endpoint and the Swagger UI
			app.UseSwagger();
			app.UseSwaggerUI(options =>
			{
				options.SwaggerEndpoint("/swagger/v1/swagger.json", "v1");
				options.RoutePrefix = "swagger";
				options.AddSwaggerBootstrap().AddExperimentalFeatures();
				options.InjectJavascript("/swagger/auth.js");
            });

			// Enable static files to support swagger bootstrap
			app.UseStaticFiles();

			// Enable HTTPS redirection
			app.UseHttpsRedirection();

			// Enable CORS
			app.UseCors(app.Environment.IsDevelopment() ? "AllowAllLocalhost" : "AllowFlutterApp");

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