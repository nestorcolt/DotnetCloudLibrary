using CloudLibrary.lib;
using CloudLibrary.Lib;
using CloudLibrary.Models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace CloudLibrary.Controllers
{
    public class BlockCatcher : IBlockCatcher
    {
        private readonly ILogger<BlockCatcher> _log;
        private readonly IApiHandler _apiHandler;
        //private Stopwatch SpeedCounter;

        public BlockCatcher(ILogger<BlockCatcher> log, IApiHandler apiHandler)
        {
            _log = log;
            _apiHandler = apiHandler;
        }

        public async Task DeactivateUser(string userId)
        {
            await SnsHandler.PublishToSnsAsync(new JObject(new JProperty(Constants.UserPk, userId)).ToString(), "msg", Constants.StopSnsTopic);
        }

        public bool ScheduleHasData(JToken searchSchedule)
        {
            if (searchSchedule != null && searchSchedule.HasValues)
                return true;

            return false;
        }

        public Dictionary<string, string> SignRequestHeaders(string url, string accessToken, Dictionary<string, string> requestHeaders)
        {
            SortedDictionary<string, string> signatureHeaders = SignatureObject.CreateSignature(url, accessToken);

            requestHeaders["X-Amz-Date"] = signatureHeaders["X-Amz-Date"];
            requestHeaders["X-Flex-Client-Time"] = GetTimestamp().ToString();
            requestHeaders["X-Amzn-RequestId"] = signatureHeaders["X-Amzn-RequestId"];
            requestHeaders["Authorization"] = signatureHeaders["Authorization"];

            return requestHeaders;
        }

        public int GetTimestamp()
        {
            TimeSpan time = (DateTime.UtcNow - DateTime.UnixEpoch);
            int timestamp = (int)time.TotalSeconds;
            return timestamp;
        }

        public Dictionary<string, string> EmulateDevice(Dictionary<string, string> requestDictionary)
        {
            string instanceId = Guid.NewGuid().ToString().Replace("-", "");
            string androidVersion = Constants.OSVersion;
            string deviceModel = Constants.DeviceModel;
            string build = Constants.BuildVersion;

            var offerAcceptHeaders = new Dictionary<string, string>
            {
                ["x-flex-instance-id"] = $"{instanceId.Substring(0, 8)}-{instanceId.Substring(8, 4)}-" +
                                         $"{instanceId.Substring(12, 4)}-{instanceId.Substring(16, 4)}-{instanceId.Substring(20, 12)}",
                ["User-Agent"] = $"Dalvik/2.1.0 (Linux; U; Android {androidVersion}; {deviceModel} Build/{build}) RabbitAndroid/{Constants.AppVersion}",
                ["Connection"] = "Keep-Alive",
                ["Accept-Encoding"] = "gzip"
            };

            // Set the class field with the new offer headers
            foreach (var header in offerAcceptHeaders)
            {
                requestDictionary[header.Key] = header.Value;
            }

            return requestDictionary;
        }

        public async Task AcceptSingleOfferAsync(JToken block, UserDto userDto, Dictionary<string, string> requestHeaders)
        {
            bool isValidated = false;
            string status = block["status"].ToString();
            long offerTime = (long)block["startTime"];
            string serviceAreaId = (string)block["serviceAreaId"];
            float offerPrice = (float)block["rateInfo"]["priceAmount"];

            // Validates the calendar schedule for this user
            bool scheduleValidation = false;
            bool areaValidation = false;

            Parallel.Invoke(() => scheduleValidation = ScheduleValidator.ValidateSchedule(userDto.SearchSchedule, offerTime, userDto.TimeZone),
                () => areaValidation = userDto.Areas.Contains(serviceAreaId));

            // RESERVED blocks validation
            var reservedBlocksValidationList = new List<bool>() { areaValidation, status == "RESERVED" };
            bool reservedBlocksValidation = reservedBlocksValidationList.All(element => element);
            bool arrivalTimeCheck = Math.Abs(offerTime - GetTimestamp()) > userDto.ArrivalTime;

            if (scheduleValidation && offerPrice >= userDto.MinimumPrice && areaValidation && arrivalTimeCheck || reservedBlocksValidation)
            {
                JObject acceptHeader = new JObject(
                    new JProperty("__type", $"AcceptOfferInput:{Constants.AcceptInputUrl}"),
                    new JProperty("offerId", block["offerId"].ToString())
                );

                HttpResponseMessage response = await _apiHandler.PostDataAsync(Constants.AcceptUri, acceptHeader.ToString(), requestHeaders);

                if (response.IsSuccessStatusCode)
                {
                    // send to owner endpoint accept data to log and send to the user the notification
                    JObject data = new JObject(
                        new JProperty(Constants.UserPk, userDto.UserId),
                        new JProperty("data", block)
                        );

                    // LOGS FOR ACCEPTED OFFERS
                    await SnsHandler.PublishToSnsAsync(data.ToString(), "msg", Constants.AcceptedSnsTopic);
                    await SqsHandler.SendMessage(Constants.UpdateBlocksTableQueue, data.ToString());
                }

                // to track in offers table as validated offer (passed the filters)
                isValidated = true;

                // test to log in cloud watch (Removed later)
                var msg = $"\nUser: {userDto.UserId} >> Accept Block Operation Status >> Code >> {response.StatusCode}\n";
                Console.WriteLine(msg);
            }

            // the code that you want to measure comes here
            //SpeedCounter.Stop();
            //Console.WriteLine(SpeedCounter.ElapsedMilliseconds);

            // send the offer seen to the offers table for further data processing or analytic
            JObject offerSeen = new JObject(
                new JProperty(Constants.UserPk, userDto.UserId),
                new JProperty("validated", isValidated),
                new JProperty("data", block)
            );

            // LOGS FOR SEEN OFFERS
            await SqsHandler.SendMessage(Constants.UpdateOffersTableQueue, offerSeen.ToString());
        }

        public void AcceptOffers(JToken offerList, UserDto userDto, Dictionary<string, string> requestHeaders)
        {
            Parallel.For(0, offerList.Count(), n =>
            {
                Thread accept = new Thread(async task => await AcceptSingleOfferAsync(offerList[n], userDto, requestHeaders));
                accept.Start();
            });
        }

        public async Task<HttpStatusCode> GetOffersAsyncHandle(UserDto userDto, string serviceAreaId, Dictionary<string, string> requestHeaders)
        {
            //var signedHeaders = SignRequestHeaders($"{Constants.ApiBaseUrl}{Constants.OffersUri}", userDto.AccessToken, requestHeaders);
            var response = await _apiHandler.PostDataAsync(Constants.OffersUri, serviceAreaId, requestHeaders);
            var msg = $"\nUser: {userDto.UserId} >> Blocks: {response.StatusCode} >> Content: {response.Content}\n";
            Console.WriteLine(msg);
            //SpeedCounter = Stopwatch.StartNew();

            if (response.IsSuccessStatusCode)
            {
                JObject requestToken = await _apiHandler.GetRequestJTokenAsync(response);
                JToken offerList = requestToken.GetValue("offerList");

                if (offerList.HasValues)
                {
                    Thread acceptThread = new Thread(task => AcceptOffers(offerList, userDto, requestHeaders));
                    acceptThread.Start();
                }

                // If log debug
                _log.LogDebug(offerList.ToString());
            }

            return response.StatusCode;
        }

        public async Task<bool> LookingForBlocks(UserDto userDto)
        {
            // validator of weekly schedule
            if (!ScheduleHasData(userDto.SearchSchedule))
            {
                await DeactivateUser(userDto.UserId);
                _log.LogWarning("User Is being Deactivated");
                return false;
            }

            // Start filling the headers
            var requestHeaders = new Dictionary<string, string>();

            // Set token in request dictionary
            requestHeaders[Constants.TokenKeyConstant] = userDto.AccessToken;

            // Primary methods resolution to get access to the request headers
            requestHeaders = EmulateDevice(requestHeaders);

            // validation before continue
            if (String.IsNullOrEmpty(userDto.ServiceAreaHeader))
            {
                await CloudLogger.Log("Service area ID was empty or null. Re-trying authentication...", userDto.UserId);
                await Authenticator.RequestNewAccessToken(userDto);
                return false;
            }

            // start logic here main request
            HttpStatusCode statusCode = await GetOffersAsyncHandle(userDto, userDto.ServiceAreaHeader, requestHeaders);

            if (statusCode is HttpStatusCode.OK)
            {
                return true;
            }

            if (statusCode is HttpStatusCode.Unauthorized || statusCode is HttpStatusCode.Forbidden)
            {
                // Re-authenticate after the access token has expired
                await Authenticator.RequestNewAccessToken(userDto);
                _log.LogWarning("Requesting New Access Token");
            }

            else if (statusCode is HttpStatusCode.BadRequest || statusCode is HttpStatusCode.TooManyRequests)
            {
                // Request exceed. Send to SNS topic to terminate the instance. Put to sleep for 30 minutes
                await SnsHandler.PublishToSnsAsync(new JObject(new JProperty(Constants.UserPk, userDto.UserId)).ToString(), "msg", Constants.SleepSnsTopic);

                // Stream Logs
                string responseStatus = $"\nRequest Status >> Reason >> {statusCode} | The system will pause for 30 minutes\n";
                await CloudLogger.Log(responseStatus, userDto.UserId);
                _log.LogWarning(responseStatus);
            }

            return false;
        }
    }
}