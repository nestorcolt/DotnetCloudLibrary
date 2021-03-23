using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using System;
using System.Threading.Tasks;

namespace CloudLibrary.Lib
{
    public static class SnsHandler
    {
        public static IAmazonSimpleNotificationService Client = new AmazonSimpleNotificationServiceClient();

        public static async Task PublishToSnsAsync(string message, string subject, string topicArn)
        {
            if (String.IsNullOrEmpty(subject))
            {
                subject = "msg";
            }

            var request = new PublishRequest
            {
                TopicArn = topicArn,
                Message = message,
                Subject = subject,
            };

            // send the message
            await Client.PublishAsync(request);
        }
    }
}
