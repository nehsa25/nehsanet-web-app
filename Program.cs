using MySqlConnector;
using Serilog;

namespace WebApp
{
    public class CSPMiddleware
    {
        private readonly string _cspPolicy;

        public CSPMiddleware(string cspPolicy)
        {
            _cspPolicy = cspPolicy;
        }

        public async Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
            context.Response.Headers.Append("Content-Security-Policy", _cspPolicy);
            await next(context);
        }
    }

    internal class Program
    {
        private static void Main(string[] args)
        {
            var api = "apiOrigin";
            var local = "localOrigin";
            var website = "websiteOrgin";

            WebApplicationBuilder webApplicationBuilder = WebApplication.CreateBuilder(args);

            // health check
            webApplicationBuilder.Services.AddHealthChecks();

            // Logging support, t
            webApplicationBuilder.Services.AddLogging();

            // CORS support
            webApplicationBuilder.Services.AddCors(options =>
            {
                options.AddPolicy(name: website,
                                  policy =>
                                  {
                                      policy.WithOrigins("https://www.nehsa.net").AllowAnyMethod().AllowAnyHeader().AllowCredentials();
                                  });
                options.AddPolicy(name: api,
                                  policy =>
                                  {
                                      policy.WithOrigins("https://api.nehsa.net").AllowAnyMethod().AllowAnyHeader().AllowCredentials();
                                  });
                options.AddPolicy(name: local,
                                  policy =>
                                  {
                                      policy.WithOrigins("http://localhost:4200").AllowAnyMethod().AllowAnyHeader().AllowCredentials();
                                  });
            });

            // allows serving our SPA app
            webApplicationBuilder.Services.AddSpaStaticFiles(configuration =>
            {
                configuration.RootPath = "app/wwwrooot";
            });         

            // Logging support
            webApplicationBuilder.Logging.ClearProviders();
            webApplicationBuilder.Logging.AddConsole();
            webApplicationBuilder.Logging.AddDebug();

            // Add the controllers
            webApplicationBuilder.Services.AddControllers();

            // Build the app
            var app = webApplicationBuilder.Build();

            var logger = app.Services.GetRequiredService<ILogger<Program>>();
            logger.LogInformation("Logging setup");

            // Used to serve the Angular app
            logger.LogInformation("Setting up Angular Middleware");

            //redirect to index.html if the request is not an API request
            app.Use(async (context, next) =>
            {
                await next();
                if (context.Response.StatusCode == 404 && !Path.HasExtension(context.Request.Path.Value))
                {  
                    context.Response.StatusCode = StatusCodes.Status302Found;
                    context.Request.Path = "/index.html";
                    await next();
                }
            });

            // ensure we redirect to index.html any non API requests
            logger.LogInformation("Setting up Default Files");
            app.UseDefaultFiles(new DefaultFilesOptions
            {
                DefaultFileNames = ["index.html"]
            });

            // set to use CORS
            logger.LogInformation("Setting up CORS for API: " + api);
            app.UseCors(api);

            logger.LogInformation("Setting up CORS for WEBSITE: " + api);
            app.UseCors(website);

            logger.LogInformation("Setting up CORS for LOCAL: " + api);
            app.UseCors(local);

            logger.LogInformation("Setting up UseHttpsRedirection");
            app.UseStaticFiles(); // this is also required to actually serve the static files of the Angular app
            app.UseHttpsRedirection(); // redirect to https
            app.UseExceptionHandler("/Error"); // handle exceptions
            app.UseHealthChecks("/health"); // setup health checks using the default health check middleware
            app.UseRouting(); // This configues the routing middleware
            app.MapControllers(); // This maps the controllers to the routing middleware. e.g. without this, the controllers will not be called

            // setup the middleware to handle the Content-Security-Policy header
            // app.UseMiddleware<CSPMiddleware>("default-src 'self'; style-src 'self' 'unsafe-inline'; script-src 'self' 'unsafe-inline' 'unsafe-eval'; img-src 'self' data:; font-src 'self';");

            // start app
            logger.LogInformation("Starting Application!");
            app.Run();
        }
    }
}