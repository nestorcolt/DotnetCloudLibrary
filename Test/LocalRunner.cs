using CloudLibrary.Controllers;
using CloudLibrary.lib;
using CloudLibrary.Lib;
using CloudLibrary.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using System;
using System.Net.Http.Headers;
using System.Threading;

namespace CloudLibrary.Test
{
    public class LocalRunner
    {
        private string _userId = "5";

        public void Run()
        {
            var host = Host.CreateDefaultBuilder()
                .ConfigureServices((context, services) =>
                {
                    // Client factory: Typed Client
                    services.AddHttpClient<IApiHandler, ApiHandler>(c =>
                    {
                        c.BaseAddress = new Uri(Constants.ApiBaseUrl);
                        c.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    });

                    services.AddTransient<ILibConfiguration, LibConfiguration>();
                    services.AddTransient<IAuthenticator, Authenticator>();
                    services.AddTransient<IBlockCatcher, BlockCatcher>();

                })
                .Build();

            // Catcher DI
            var catcher = ActivatorUtilities.CreateInstance<BlockCatcher>(host.Services);

            // Data to parse
            string userData = DynamoHandler.QueryUser(_userId).Result;
            UserDto userDto = JsonConvert.DeserializeObject<UserDto>(userData);
            userDto.TimeZone = "Eastern Standard Time";
            userDto.MinimumPrice = 10000;

            while (true)
            {
                bool result = catcher.LookingForBlocks(userDto).Result;
                Console.WriteLine($"Iteration Result: {result}");

                if (!result)
                {
                    break;
                }

                // Wait!
                Thread.Sleep(2000);
            }
        }

    }
}
