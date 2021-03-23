using CloudLibrary.lib;
using CloudLibrary.Lib;
using CloudLibrary.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace CloudLibrary.Controllers
{
    public class Authenticator : IAuthenticator
    {
        private readonly IApiHandler _apiHandler;

        public Authenticator(IApiHandler apiHandler)
        {
            _apiHandler = apiHandler;
        }

        private async Task<string> GetAmazonAccessToken(string refreshToken, string userId)
        {
            var authenticationHeader = new Dictionary<string, string>
            {
                {"app_name", "com.amazon.rabbit"},
                {"source_token_type", "refresh_token"},
                {"source_token", refreshToken},
                {"requested_token_type", "access_token"}
            };

            HttpRequestMessage httpRequest = new HttpRequestMessage(HttpMethod.Post, Constants.AuthTokenUrl)
            {
                Content = new StringContent(JsonConvert.SerializeObject(authenticationHeader), Encoding.UTF8, "application/json")
            };

            HttpClient client = new HttpClient();
            HttpResponseMessage response = await client.SendAsync(httpRequest);

            if (response.IsSuccessStatusCode)
            {
                JObject requestToken = await _apiHandler.GetRequestJTokenAsync(response);
                return requestToken["access_token"].ToString();
            }

            await SnsHandler.PublishToSnsAsync(new JObject(new JProperty(Constants.UserPk, userId)).ToString(), "msg", Constants.ErrorSnsTopic);
            throw new UnauthorizedAccessException($"There is a problem with the authentication.\nReason: {response.Content}");
        }

        public static async Task RequestNewAccessToken(UserDto userDto)
        {
            await SnsHandler.PublishToSnsAsync(JsonConvert.SerializeObject(userDto), "msg", Constants.AuthenticationSnsTopic);
        }

        public async Task<string> GetServiceArea(string accessToken)
        {
            var headers = new Dictionary<string, string>() { [Constants.TokenKeyConstant] = accessToken };
            HttpResponseMessage content = await _apiHandler.GetDataAsync(Constants.ServiceAreaUri, headers);

            if (content.IsSuccessStatusCode)
            {
                // declare place holder variable
                string serviceAreaId = "";

                // continue if the validation succeed
                JObject requestToken = await _apiHandler.GetRequestJTokenAsync(content);
                JToken result = requestToken.GetValue("serviceAreaIds");

                if (result.HasValues)
                {
                    serviceAreaId = (string)result[0];
                }

                var filtersDict = new Dictionary<string, object>
                {
                    ["serviceAreaFilter"] = new List<string>(),
                    ["timeFilter"] = new Dictionary<string, string>(),
                };

                // Id Dictionary to parse to offer headers later
                var serviceDataDictionary = new Dictionary<string, object>
                {
                    ["serviceAreaIds"] = new[] { serviceAreaId },
                    ["filters"] = filtersDict,
                };

                // MERGE THE HEADERS OFFERS AND SERVICE DATA IN ONE MAIN HEADER DICTIONARY
                string serviceAreaFilterData = JsonConvert.SerializeObject(serviceDataDictionary).Replace("\\", "");
                return serviceAreaFilterData;
            }

            return null;
        }

        public async Task Authenticate(string refreshToken, string userId)
        {
            // authenticated for new access token
            string accessToken = Task.Run(() => GetAmazonAccessToken(refreshToken, userId)).Result;
            string serviceArea = GetServiceArea(accessToken).Result;

            // save credentials and user meta data
            await DynamoHandler.SetUserData(userId, accessToken, serviceArea);
        }
    }
}