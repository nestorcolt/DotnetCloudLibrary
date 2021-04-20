using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;

namespace CloudLibrary.Lib
{
    public static class SignatureObject
    {
        private const string SignaturePrefix = "RABBIT3-HMAC-SHA256";

        public static SortedDictionary<string, string> CreateSignature(string url, string token)
        {
            // 0. Prepare request message.
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, url);

            string amzLongDate = DateTimeOffset.UtcNow.ToString("yyyyMMddTHHmmssZ");
            string randomSecret = "krY14bGw2xQ60jnT";
            string canonicalUrl = request.RequestUri.AbsolutePath;
            string requestId = Guid.NewGuid().ToString();
            string hostUrl = request.RequestUri.Host;

            var headers = new SortedDictionary<string, string>()

            {
                ["host"] = hostUrl,
                ["x-amz-access-token"] = token,
                ["X-Amzn-RequestId"] = requestId,
                ["X-Amz-Date"] = amzLongDate
            };


            // **************************************************** SIGNING PORTION ****************************************************

            var canonicalRequest = GetCanonicalRequest(headers, canonicalUrl);
            string stringToSign = GetStringToSign(canonicalRequest[0], amzLongDate);
            var secret = ReverseString(token);

            var sequence = new List<string>() { randomSecret + secret, amzLongDate.Substring(0, 8), "rabbit_request", stringToSign };
            var signedHexString = SignSequenceString(sequence);

            string authHeader = $"{SignaturePrefix} SignedHeaders={canonicalRequest[1]},Signature={signedHexString}";
            headers["Authorization"] = authHeader;
            return headers;

            // **************************************************** END SIGNING PORTION ****************************************************

        }


        // --------------------------------- Utilities ----------------------------------------------------------------


        public static string ReverseString(string stringToReverse)
        {
            char[] charArray = stringToReverse.ToCharArray();
            Array.Reverse(charArray);
            return new string(charArray);
        }

        private static List<string> GetCanonicalRequest(SortedDictionary<string, string> headers, string canonicalPath)
        {
            string canonicalHeaderString = "";

            foreach (var key in headers)
            {
                canonicalHeaderString += key.Key.ToLower() + ":" + key.Value.ToString() + "\n";
            }

            // Put in order - this is important.
            var queryParams = headers.Select(header => header.Key.ToLower());
            string signHeader = string.Join(";", queryParams);
            var stringRequest = $"POST\n{canonicalPath}\n{canonicalHeaderString}\n{signHeader}";
            var returnInfo = new List<string>() { stringRequest, signHeader };
            return returnInfo;
        }


        private static string SignSequenceString(List<string> inputStringSequence)
        {

            byte[] key = new byte[] { };

            foreach (var value in inputStringSequence)
            {

                if (key.Length == 0)
                {
                    key = Encoding.Default.GetBytes(value);
                }
                else
                {
                    byte[] bytesData = Encoding.Default.GetBytes(value);
                    key = HmacSha256(key, bytesData);
                }
            }

            var hexKey = ToHex(key, false);
            return hexKey;
        }

        private static string ToHex(byte[] bytes, bool upperCase)
        {
            StringBuilder result = new StringBuilder(bytes.Length * 2);

            foreach (var n in bytes)
                result.Append(n.ToString(upperCase ? "X2" : "x2"));

            return result.ToString();
        }

        private static string SHA256HexHashString(string stringIn)
        {
            string hashString;

            using (var sha256 = SHA256Managed.Create())
            {
                var hash = sha256.ComputeHash(Encoding.Default.GetBytes(stringIn));
                hashString = ToHex(hash, false);
            }

            return hashString;

        }

        private static byte[] HmacSha256(byte[] key, byte[] data)
        {
            return new HMACSHA256(key).ComputeHash(data);
        }

        private static string GetStringToSign(string canonicalRequest, string time)
        {
            return $"{SignaturePrefix}\n{time}\n{SHA256HexHashString(canonicalRequest)}";
        }
    }

}