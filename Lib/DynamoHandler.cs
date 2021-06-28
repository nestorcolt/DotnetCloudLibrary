using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DocumentModel;
using Amazon.DynamoDBv2.Model;
using CloudLibrary.lib;
using CloudLibrary.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CloudLibrary.Lib
{
    public static class DynamoHandler
    {
        public static readonly AmazonDynamoDBClient Client = new AmazonDynamoDBClient();

        private static string _lastIteration = "last_iteration";
        private static string _accessToken = "access_token";
        private static string _serviceArea = "service_area";
        private static string _blockTable = "Blocks";
        private static string _OffersTable = "Offers";
        private static string _tableName = "Users";
        private static string _tablePk = "user_id";
        private static string _blockId = "block_id";

        public static async Task UpdateUserTimestamp(string userId, int timeStamp)
        {
            Table table = Table.LoadTable(Client, _tableName);
            Document document = new Document { [_lastIteration] = timeStamp };
            await table.UpdateItemAsync(document, userId);
        }

        public static async Task<string> QueryUser(string userId)
        {
            QueryFilter scanFilter = new QueryFilter();
            Table usersTable = Table.LoadTable(Client, _tableName);
            scanFilter.AddCondition(_tablePk, ScanOperator.Equal, userId);

            Search search = usersTable.Query(scanFilter);
            List<Document> documentSet = await search.GetNextSetAsync();

            if (documentSet.Count > 0)
            {
                return documentSet[0].ToJson();
            }

            return null;
        }

        public static async Task SetUserData(string userId, string accessToken, string serviceArea)
        {
            Table usersTable = Table.LoadTable(Client, _tableName);

            Document order = new Document
            {
                [_serviceArea] = serviceArea,
                [_accessToken] = accessToken
            };

            await usersTable.UpdateItemAsync(order, userId);
        }

        public static async Task PutNewOffer(string userId, bool validated, JObject data)
        {
            Table offersTable = Table.LoadTable(Client, _OffersTable);

            long currentTime = Utils.GetUnixTimestamp();
            long hoursToMinutes = Constants.CleanUpOffersTimeThreshold * 60;
            long expirationDate = Utils.GetFutureTimeStamp(hoursToMinutes);

            // Create offer from model
            OfferModel offerModel = new OfferModel
            {
                UserId = userId,
                OfferId = data["offerId"].ToString(),
                Validated = validated,
                OfferAreaId = data["serviceAreaId"].ToString(),
                OfferTime = currentTime,
                OfferTimeToLive = expirationDate,
                OfferData = data
            };

            string jsonText = JsonConvert.SerializeObject(offerModel, Formatting.Indented);
            Document offerDocument = Document.FromJson(jsonText);

            await offersTable.UpdateItemAsync(offerDocument, userId, offerModel.OfferId);
        }

        public static async Task PutNewBlock(string userId, JObject data)
        {
            Table blocksTable = Table.LoadTable(Client, _blockTable);

            long currentTime = Utils.GetUnixTimestamp();
            long hoursToMinutes = Constants.CleanUpOffersTimeThreshold * 60;
            long expirationDate = Utils.GetFutureTimeStamp(hoursToMinutes);

            string startTime = data["startTime"].ToString();

            // Create block from model
            BlockModel blockModel = new BlockModel()
            {
                UserId = userId,
                BlockId = GetNewBlockId(currentTime, startTime),
                BlockAreaId = data["serviceAreaId"].ToString(),
                BlockTime = currentTime,
                BlockTimeToLive = expirationDate,
                BlockData = data
            };

            string jsonText = JsonConvert.SerializeObject(blockModel, Formatting.Indented);
            Document blockDocument = Document.FromJson(jsonText);

            await blocksTable.UpdateItemAsync(blockDocument, userId, blockModel.BlockId);
        }

        private static long GetNewBlockId(long capturedTime, string blockTime)
        {
            Random rnd = new Random();
            int randInt = rnd.Next(0, Int32.Parse(blockTime));
            long result = capturedTime + randInt + Int32.Parse(blockTime);
            return result;
        }

        public static async Task DeleteBlocksTable()
        {
            Table table = Table.LoadTable(Client, _blockTable);

            while (true)
            {
                ScanFilter scanFilter = new ScanFilter();
                Search search = table.Scan(scanFilter);
                List<Document> documentSet = search.GetNextSetAsync().Result;

                // If the counter is 0 will break the loop. This because the batch can only process a fixed amount of
                // items or size per call because dynamoDB technology.
                if (documentSet.Count == 0)
                    break;

                // Start to handle the WriteRequest (the method to batch process many elements at once)
                var writeRequestList = new List<WriteRequest>();
                int iterationCount = documentSet.Count > 24 ? 24 : documentSet.Count;

                Parallel.For(0, iterationCount, index =>
                {
                    writeRequestList.Add(new WriteRequest
                    {
                        DeleteRequest = new DeleteRequest
                        {
                            Key = new Dictionary<string, AttributeValue>()
                            {
                                {_tablePk, new AttributeValue {S = documentSet[index].ToAttributeMap()[_tablePk].S}},
                                {_blockId, new AttributeValue {N = documentSet[index].ToAttributeMap()[_blockId].N}}
                            }
                        }
                    });
                });

                // Fill the request with the items retrieved in the parallel loop
                var requestItems = new Dictionary<string, List<WriteRequest>>() { [_blockTable] = writeRequestList };
                var request = new BatchWriteItemRequest { RequestItems = requestItems };

                try
                {
                    // Try and catch in case the table is empty
                    await Client.BatchWriteItemAsync(request);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    break;
                }
            }
        }
    }
}