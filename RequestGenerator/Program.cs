using IdentityModel;
using IdentityModel.Client;
using System;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

namespace RequestGenerator
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("Request Generator started!");
            Thread.Sleep(2000);

            //await MakeRequestSecret();
            await MakeRequestMtls();

        }

        static async Task MakeRequestSecret()
        {
            Console.WriteLine("Initiating request secret");

            var client = new HttpClient();
            var disco = await client.GetDiscoveryDocumentAsync("https://localhost:5000");
            if (disco.IsError)
            {
                throw new Exception(disco.Error);
            }

            var tokenResponse = await client.RequestClientCredentialsTokenAsync(new ClientCredentialsTokenRequest
            {
                Address = disco.TokenEndpoint,
                ClientId = "secret",
                ClientSecret = "banana",

                Scope = "protected"
            });

            if (tokenResponse.IsError)
            {
                throw new Exception(tokenResponse.Error);
            }

            // call api
            var apiClient = new HttpClient();
            apiClient.SetBearerToken(tokenResponse.AccessToken);

            var response = await apiClient.GetAsync("https://localhost:5001/home/index");
            if (!response.IsSuccessStatusCode)
            {
                System.Console.WriteLine(response.StatusCode);
            }
            else
            {
                var content = await response.Content.ReadAsStringAsync();
                System.Console.WriteLine(content);
            }
        }

        static async Task MakeRequestMtls()
        {
            Console.WriteLine("Initiating request mtls");

            var handler = new SocketsHttpHandler();

            var cert = new X509Certificate2("testmtls.pfx", "changeme");

            handler.SslOptions.ClientCertificates = new X509CertificateCollection { cert };

            var client = new HttpClient(handler);

            var disco = await client.GetDiscoveryDocumentAsync("https://localhost:5000");
            if (disco.IsError) throw new Exception(disco.Error);

            var response = await client.RequestClientCredentialsTokenAsync(new ClientCredentialsTokenRequest
            {
                Address = disco
                    .TryGetValue(OidcConstants.Discovery.MtlsEndpointAliases)
                    .Value<string>(OidcConstants.Discovery.TokenEndpoint)
                    .ToString(),

                ClientId = "mtls",
                Scope = "protected"
            });

            if (response.IsError) throw new Exception(response.Error);

            var newHandler = new SocketsHttpHandler();
            newHandler.SslOptions.ClientCertificates = new X509CertificateCollection { cert };

            // call api
            var apiClient = new HttpClient(handler);
            apiClient.SetBearerToken(response.AccessToken);

            var apiResponse = await apiClient.GetAsync("https://localhost:5001/home/index");
            if (!apiResponse.IsSuccessStatusCode)
            {
                System.Console.WriteLine(apiResponse.StatusCode);
            }
            else
            {
                var content = await apiResponse.Content.ReadAsStringAsync();
                System.Console.WriteLine(content);
            }
        }
    }
}
