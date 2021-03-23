using CloudLibrary.Models;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace CloudLibrary.Controllers
{
    public class ApiHandler : IApiHandler
    {
        private readonly HttpClient _httpClient;

        public ApiHandler(HttpClient client)
        {
            _httpClient = client;
        }

        public async Task<HttpResponseMessage> GetDataAsync(string uri, Dictionary<string, string> headers = null)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, uri);

            if (headers != null)
                request = AddRequestHeaders(headers, request);

            try
            {
                HttpResponseMessage response = await _httpClient.SendAsync(request);
                return response;
            }
            catch (HttpRequestException e)
            {
                Console.WriteLine("\nException Caught!");
                Console.WriteLine("Message :{0} ", e.Message);
            }

            return null;
        }

        public async Task<HttpResponseMessage> PostDataAsync(string uri, string content, Dictionary<string, string> headers = null)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, uri)
            {
                Content = new StringContent(content, Encoding.UTF8, "application/json")
            };

            if (headers != null)
                request = AddRequestHeaders(headers, request);

            try
            {
                HttpResponseMessage response = await _httpClient.SendAsync(request);
                return response;
            }
            catch (HttpRequestException e)
            {
                Console.WriteLine("\nException Caught!");
                Console.WriteLine("Message :{0} ", e.Message);
            }

            return null;
        }

        public async Task<JObject> GetRequestJTokenAsync(HttpResponseMessage requestMessage)
        {
            string content = await requestMessage.Content.ReadAsStringAsync();
            return await Task.Run(() => JObject.Parse(content));
        }

        private HttpRequestMessage AddRequestHeaders(Dictionary<string, string> headersDictionary, HttpRequestMessage request)
        {
            foreach (var data in headersDictionary)
            {
                request.Headers.Add(data.Key, data.Value);
            }

            return request;
        }
    }
}