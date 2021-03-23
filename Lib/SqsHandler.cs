using Amazon.SQS;
using Amazon.SQS.Model;
using System;
using System.Threading.Tasks;

namespace CloudLibrary.Lib
{
    public static class SqsHandler
    {
        public static IAmazonSQS Client = new AmazonSQSClient();

        public static async Task<string> GetQueueByName(IAmazonSQS sqsClient, string name)
        {
            ListQueuesResponse responseList = await sqsClient.ListQueuesAsync(name);

            if (responseList.QueueUrls.Count > 0)
            {
                return responseList.QueueUrls[0];
            }

            return null;
        }

        public static async Task SendMessage(IAmazonSQS sqsClient, string qUrl, string messageBody)
        {
            SendMessageResponse responseSendMsg = await sqsClient.SendMessageAsync(qUrl, messageBody);
        }

        public static async Task DeleteMessage(IAmazonSQS sqsClient, string receiptHandle, string qUrl)
        {
            try
            {
                await sqsClient.DeleteMessageAsync(qUrl, receiptHandle);
            }
            catch (Exception)
            {
                Console.WriteLine("Message couldn't be deleted. Was null or didn't exist");
            }
        }
    }
}
