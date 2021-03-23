using Amazon.SecurityToken;
using Amazon.SecurityToken.Model;

namespace CloudLibrary.Lib
{
    public static class StsHandler
    {
        public static readonly AmazonSecurityTokenServiceClient Client = new AmazonSecurityTokenServiceClient();

        public static string GetAccountId()
        {
            var getCallerIdentityResponse = Client.GetCallerIdentityAsync(new GetCallerIdentityRequest()).Result;
            return getCallerIdentityResponse.Account;
        }
    }
}
