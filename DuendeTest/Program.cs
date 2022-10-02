using Duende.IdentityServer.Models;
using Microsoft.AspNetCore.Authentication.Certificate;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.HttpSys;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Text;


namespace IdentityServer4Test
{
    public class Program
    {
        public static void Main()
        {
            var secretHash = "banana".Sha256();
            CreateHostBuilder().Build().Run();
        }

        public static IHostBuilder CreateHostBuilder()
        {
            var config = new ConfigurationBuilder()
                    .AddJsonFile("appsettings.json")
                    .Build();

            return Host.CreateDefaultBuilder()
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseConfiguration(config);
                    webBuilder.UseStartup<Startup>();

                    webBuilder.ConfigureServices(services =>
                    {
                        services.AddControllers();
                        AddIdentityServerMine(services);

                        #region Local Authorization setup
                        //services.AddLocalApiAuthentication();
                        #endregion
                    });

                    webBuilder.UseHttpSys(options =>
                    {
                        options.ClientCertificateMethod = ClientCertificateMethod.AllowRenegotation;
                        options.AllowSynchronousIO = false;
                        options.Authentication.Schemes = Microsoft.AspNetCore.Server.HttpSys.AuthenticationSchemes.NTLM | Microsoft.AspNetCore.Server.HttpSys.AuthenticationSchemes.Negotiate;
                        options.Authentication.AllowAnonymous = true;
                        options.MaxConnections = 1000;
                        options.MaxRequestBodySize = 30000000;
                        options.UrlPrefixes.Add("https://localhost:5000");
                        options.Http503Verbosity = Http503VerbosityLevel.Full;
                    });
                });
        } 
            
        public static void AddIdentityServerMine(IServiceCollection services)
        {
            var migrationsAssembly = typeof(Startup).GetTypeInfo().Assembly.GetName().Name;
            var connectionString = "Data Source=DESKTOP-P3P2398\\SQLEXPRESS;Initial Catalog=Duende;Integrated Security=True";
            var signingCertThumpbprint = "e8f94a1ed295eaad12833c12fb359708ed694202";
            var signingCertificate = GetCert(signingCertThumpbprint);

            services.AddIdentityServer(options =>
            {
                options.MutualTls.Enabled = true;
                options.MutualTls.ClientCertificateAuthenticationScheme = "Certificate";

                // uses sub-domain hosting
                //options.MutualTls.DomainName = "mtls";
            })
            // DB Context
            // IdentityServerDbContext
            .AddConfigurationStore(options =>
             {
                 options.ConfigureDbContext = b =>
                     b.UseSqlServer(connectionString,
                         sql => sql.MigrationsAssembly(migrationsAssembly));
             })
            // this adds the operational data from DB (codes, tokens, consents)
            .AddOperationalStore(options =>
            {
                options.ConfigureDbContext = b =>
                    b.UseSqlServer(connectionString,
                        sql => sql.MigrationsAssembly(migrationsAssembly));

                // this enables automatic token cleanup. this is optional.
                options.EnableTokenCleanup = false;
                options.TokenCleanupInterval = 30;
            })
            .AddSigningCredential(signingCertificate)
            .AddMutualTlsSecretValidators();


            services.AddAuthentication()
                .AddCertificate(options =>
                {
                    options.AllowedCertificateTypes = CertificateTypes.All;
                    options.RevocationMode = X509RevocationMode.NoCheck;
                });

            services.AddCertificateForwarding(options =>
            {
                options.CertificateHeader = "X-SSL-CERT";

                options.HeaderConverter = (headerValue) =>
                {
                    X509Certificate2 clientCertificate = null;

                    if (!string.IsNullOrWhiteSpace(headerValue))
                    {
                        var bytes = Encoding.UTF8.GetBytes(Uri.UnescapeDataString(headerValue));
                        clientCertificate = new X509Certificate2(bytes);
                    }

                    return clientCertificate;
                };
            });
        }

        #region Helper helper
        public static X509Certificate2 GetCert(string thumbprint)
        {
            using X509Store x509Store = new X509Store(StoreLocation.CurrentUser);
            x509Store.Open(OpenFlags.ReadOnly);
            X509Certificate2Collection x509Certificate2Collection = x509Store.Certificates.Find(X509FindType.FindByThumbprint, thumbprint, validOnly: false);
            if (x509Certificate2Collection == null || x509Certificate2Collection.Count != 1)
            {
                throw new Exception($"Found {x509Certificate2Collection?.Count} certificates in: {StoreLocation.CurrentUser} store with thumbprint: {thumbprint}.");
            }

            x509Store.Close();
            return x509Certificate2Collection[0];
        }
        #endregion
    }
}
 

