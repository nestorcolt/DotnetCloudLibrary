using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace CloudLibrary.Models
{
    public interface IApiHandler
    {
        Task<HttpResponseMessage> PostDataAsync(string uri, string content, Dictionary<string, string> headersDictionary = null);
        Task<HttpResponseMessage> GetDataAsync(string uri, Dictionary<string, string> headersDictionary);
        Task<JObject> GetRequestJTokenAsync(HttpResponseMessage requestMessage);
    }
}