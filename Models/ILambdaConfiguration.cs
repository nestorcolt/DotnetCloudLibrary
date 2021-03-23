using Microsoft.Extensions.Configuration;

namespace CloudLibrary.Models
{
    public interface ILambdaConfiguration
    {
        IConfigurationRoot Configuration { get; }
    }
}
