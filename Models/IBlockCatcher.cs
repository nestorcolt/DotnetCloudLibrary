using CloudLibrary.Lib;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;

namespace CloudLibrary.Models
{
    public interface IBlockCatcher
    {
        bool ValidateArea(string serviceAreaId, List<string> areas);
        Task<bool> LookingForBlocks(UserDto userDto);
        bool ScheduleHasData(JToken searchSchedule);
        Task DeactivateUser(string userId);
        int GetTimestamp();

        Task<HttpStatusCode> GetOffersAsyncHandle(UserDto userDto, string serviceAreaId, Dictionary<string, string> requestHeaders);
        Task AcceptSingleOfferAsync(JToken block, UserDto userDto, Dictionary<string, string> requestHeaders);
        void AcceptOffers(JToken offerList, UserDto userDto, Dictionary<string, string> requestHeaders);
        Dictionary<string, string> EmulateDevice(Dictionary<string, string> requestDictionary);
    }
}