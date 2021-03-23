using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DocumentModel;
using Amazon.DynamoDBv2.Model;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CloudLibrary.Lib
{
    public static class DynamoHandler
    {
        private static readonly AmazonDynamoDBClient Client = new AmazonDynamoDBClient();

        private static string _lastIteration = "last_iteration";
        private static string _accessToken = "access_token";
        private static string _serviceArea = "service_area";
        private static string _blockTable = "Blocks";
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

        public static async Task DeleteBlocksTable()
        {
            Table table = Table.LoadTable(Client, _blockTable);

            while (true)
            {
                ScanFilter scanFilter = new ScanFilter();
                Search search = table.Scan(scanFilter);
                List<Document> documentSet = search.GetNextSetAsync().Result;

                // This counter is the total of items found in the table. Will break the loop
                var tableTotalItemCount = documentSet.Count;

                // Start to handle the WriteRequest (the method to batch process many elements at once)
                var writeRequestList = new List<WriteRequest>();

                Parallel.ForEach(documentSet, item =>
                {
                    writeRequestList.Add(new WriteRequest
                    {
                        DeleteRequest = new DeleteRequest
                        {
                            Key = new Dictionary<string, AttributeValue>()
                            {
                                {_tablePk, new AttributeValue {S = item.ToAttributeMap()[_tablePk].S}},
                                {_blockId, new AttributeValue {N = item.ToAttributeMap()[_blockId].N}}
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
                catch (Exception)
                {
                    Console.WriteLine("No More Items To Delete");
                }

                // If the counter is 0 will break the loop. This because the batch can only process a fixed amount of
                // items or size per call because dynamoDB technology.
                if (tableTotalItemCount == 0)
                    break;
            }
        }
    }
}