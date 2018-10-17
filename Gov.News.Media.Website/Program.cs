using System.IO;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore;

namespace Gov.News.Media.Website
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateWebHostBuilder(args).Build().Run();
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args)
        {
            return WebHost.CreateDefaultBuilder(args)
                .UseKestrel()
                .UseHealthChecks("/hc")
                .UseContentRoot(Directory.GetCurrentDirectory())
                .ConfigureLogging((hostingContext, logging) =>
                {
                    // logging.AddConfiguration(hostingContext.Configuration.GetSection("Logging"));
                    // logging.AddConsole(/*options => options.IncludeScopes = true*/);
                    //logging.SetMinimumLevel(LogLevel.Debug);
                    // logging.AddDebug();
                    // logging.AddEventSourceLogger();
                })
                .UseIISIntegration()
                .UseStartup<Startup>();
        }
    }
}
