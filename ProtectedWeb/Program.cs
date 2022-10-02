using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.HttpSys;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.HttpSys;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace ProtectedWeb
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder().Build().Run();
        }

        public static IHostBuilder CreateHostBuilder()
        {
            return Host.CreateDefaultBuilder()
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.ConfigureServices(services =>
                    {
                    });
                    webBuilder.UseStartup<Startup>();

                    webBuilder.UseHttpSys(options =>
                    {
                        options.ClientCertificateMethod = ClientCertificateMethod.AllowCertificate;
                        options.AllowSynchronousIO = false;
                        options.Authentication.Schemes = AuthenticationSchemes.None;
                        options.Authentication.AllowAnonymous = true;
                        options.MaxConnections = 1000;
                        options.MaxRequestBodySize = 30000000;
                        options.UrlPrefixes.Add("https://localhost:5001");
                        options.Http503Verbosity = Http503VerbosityLevel.Full;
                    });
                });
        }

    }

    
}
