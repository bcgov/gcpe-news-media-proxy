using Gov.News.Media.Model;
using Gov.News.Media.Services;
using Gov.News.Media.Website.Attributes;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Core;
using Serilog.Sinks.RollingFile;

namespace Gov.News.Media.Website
{
    public class Startup
    {
        public Startup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
                .AddEnvironmentVariables();
            Configuration = builder.Build();

            var logFile = Configuration.GetSection("Logging").GetSection("LogFile").Value;
            var levelSwitch = new LoggingLevelSwitch(Serilog.Events.LogEventLevel.Warning);
            Log.Logger = new LoggerConfiguration().MinimumLevel.ControlledBy(levelSwitch).WriteTo.RollingFile(logFile).CreateLogger();
        }

        public IConfigurationRoot Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            // Add framework services.
            services.AddMvc();

            services.AddSingleton<CacheService>();
            services.AddSingleton<MediaService>();
            services.AddScoped<ValidateRefererAttribute>();
            // Add the Configuration object so that controllers may use it through dependency injection
            services.AddSingleton<IConfiguration>(Configuration);
            services.Configure<Settings>(options => Configuration.GetSection("Settings").Bind(options));
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            loggerFactory.AddConsole(Configuration.GetSection("Logging"));
            loggerFactory.AddDebug();

            loggerFactory.AddSerilog();
            
            app.UseStaticFiles();

            var staticFiles = Configuration.GetSection("StaticFiles");
            app.UseFileServer(new FileServerOptions()
            {
                RequestPath = new PathString(staticFiles.GetSection("RequestPath").Value),
                EnableDirectoryBrowsing = false
            });

            app.UseMvc();
        }
    }
}
