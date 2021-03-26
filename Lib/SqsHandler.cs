using Amazon.SQS;
using Amazon.SQS.Model;
using System;
using System.Threading.Tasks;

namespace CloudLibrary.Lib
{
    public static class SqsHandler
    {
        public static IAmazonSQS Client = new AmazonSQSClient();

        public static async Task<string> GetQueueByName(string name)
        {
            ListQueuesResponse responseList = await Client.ListQueuesAsync(name);

            if (responseList.QueueUrls.Count > 0)
            {
                return responseList.QueueUrls[0];
            }

            return null;
        }

        public static async Task SendMessage(string qUrl, string messageBody)
        {
            SendMessageResponse responseSendMsg = await Client.SendMessageAsync(qUrl, messageBody);
        }

        public static async Task DeleteMessage(string receiptHandle, string qUrl)
        {
            try
            {
                await Client.DeleteMessageAsync(qUrl, receiptHandle);
            }
            catch (Exception)
            {
                Console.WriteLine("Message couldn't be deleted. Was null or didn't exist");
            }
        }
    }
}
