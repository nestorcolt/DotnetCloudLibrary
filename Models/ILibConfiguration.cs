using Microsoft.Extensions.Configuration;

namespace CloudLibrary.Models
{
    public interface ILibConfiguration
    {
        IConfigurationRoot Configuration { get; }
    }
}
