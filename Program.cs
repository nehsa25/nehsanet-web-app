using MySqlConnector;
using Serilog;

namespace WebApp
{
    public class AngularMiddleware
    {
        private readonly RequestDelegate _next;

        public AngularMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            Console.WriteLine($"AngularMiddleware Invoke: {context.Request.Path}, IP: {context.Connection.RemoteIpAddress}");
            if (context.Request.Path.StartsWithSegments("/v1"))
            {
                // Forward API requests to Kestrel
                Console.WriteLine("Directing to Kestrel");
                await _next(context);
            }
            else
            {
                // Redirect all other requests to Angular's index.html
                Console.WriteLine("Directing to Angular SPA");
                context.Response.StatusCode = StatusCodes.Status302Found;
                context.Response.Headers.Location = "/";
            }
        }
    }

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
            webApplicationBuilder.Services.AddEndpointsApiExplorer();
            webApplicationBuilder.Services.AddSwaggerGen();
            webApplicationBuilder.Services.AddHealthChecks();
            webApplicationBuilder.Services.AddControllers();
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

            // Logging support
            webApplicationBuilder.Logging.ClearProviders();
            webApplicationBuilder.Logging.AddConsole();
            webApplicationBuilder.Logging.AddDebug();

            // MySQL support
            webApplicationBuilder.Services.AddMySqlDataSource(webApplicationBuilder.Configuration.GetConnectionString("Default")!);
            var app = webApplicationBuilder.Build();

            var logger = app.Services.GetRequiredService<ILogger<Program>>();
            logger.LogInformation("Logging setup");

            // Used to serve the Angular app
            app.UseMiddleware<AngularMiddleware>();

            // Setup the middleware that will handle the SPA routing
            app.Use(async (context, next) =>
            {
                await next();
                if (context.Response.StatusCode == 404 && !Path.HasExtension(context.Request.Path.Value))
                {  
                    logger.LogInformation("404 received, redirecting for Angular SPA: " + context.Request.Path);
                    context.Request.Path = "/index.html";
                    await next();
                }
            });

            // ensure we redirect to index.html any non API requests
            app.UseDefaultFiles(new DefaultFilesOptions
            {
                RequestPath = "/app",
                DefaultFileNames = ["index.html"]
            });

            // set to use CORS
            logger.LogInformation("Setting up CORS for API: " + api);
            app.UseCors(api);

            logger.LogInformation("Setting up CORS for WEBSITE: " + api);
            app.UseCors(website);

            logger.LogInformation("Setting up CORS for LOCAL: " + api);
            app.UseCors(local);

            app.UseHttpsRedirection(); // redirect to https
            app.UseExceptionHandler("/Error"); // handle exceptions
            app.UseSwagger(); // setup swagger, this is different than UseSWaggerUI in that it just sets up the middleware
            app.UseSwaggerUI(); // setup swagger UI, this is the UI that is used to view the API
            app.UseHealthChecks("/health"); // setup health checks using the default health check middleware
            app.UseRouting(); // This configues the routing middleware
            app.UseStaticFiles(); // allow us to serve map images
            app.MapControllers(); // This maps the controllers to the routing middleware. e.g. without this, the controllers will not be called

            // setup the middleware to handle the Content-Security-Policy header
            // app.UseMiddleware<CSPMiddleware>("default-src 'self'; style-src 'self' 'unsafe-inline'; script-src 'self' 'unsafe-inline' 'unsafe-eval'; img-src 'self' data:; font-src 'self';");


            // start app
            logger.LogInformation("Starting Application!");
            app.Run();
        }
    }
}