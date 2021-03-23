using System.Threading.Tasks;

namespace CloudLibrary.Models
{
    public interface IAuthenticator
    {
        Task<string> GetServiceArea(string accessToken);
        Task Authenticate(string refreshToken, string userId);
    }
}