using System.Threading.Tasks;
using Gov.News.Media.Model;
using Gov.News.Media.Services;
using Gov.News.Media.Website.Attributes;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.HealthChecks;
using Microsoft.Extensions.Logging;

namespace Gov.News.Media.Website
{
    public class Startup
    {
        public Startup(IWebHostEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
                .AddEnvironmentVariables();
            Configuration = builder.Build();

            var logFile = Configuration.GetSection("Logging").GetSection("LogFile").Value;
        }

        public IConfigurationRoot Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            // Add framework services.
            services.AddMvc(opt => opt.EnableEndpointRouting = false)
                .SetCompatibilityVersion(CompatibilityVersion.Latest);

            services.AddSingleton<CacheService>();
            services.AddSingleton<MediaService>();
            services.AddScoped<ValidateRefererAttribute>();
            // Add the Configuration object so that controllers may use it through dependency injection
            services.AddSingleton<IConfiguration>(Configuration);
            services.Configure<Settings>(options => Configuration.GetSection("Settings").Bind(options));

            services.AddHealthChecks(checks =>
            {
                checks.AddValueTaskCheck("HTTP Endpoint", () => new
                    ValueTask<IHealthCheckResult>(HealthCheckResult.Healthy("Ok")));
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, ILoggerFactory loggerFactory)
        {
            // loggerFactory.AddConsole(Configuration.GetSection("Logging"));
            // loggerFactory.AddDebug();
            
            app.UseStaticFiles();

            var staticFiles = Configuration.GetSection("StaticFiles");
            app.UseFileServer(new FileServerOptions()
            {
                RequestPath = new PathString(staticFiles.GetSection("RequestPath").Value),
                EnableDirectoryBrowsing = false
            });
            app.UseFileServer(new FileServerOptions()
            {
                RequestPath = new PathString("translations"),
                EnableDirectoryBrowsing = false
            });

            app.UseMvc();
        }
    }
}
