using CloudLibrary.Lib;
using System.Threading.Tasks;

namespace CloudLibrary.Models
{
    public interface IBlockCatcher
    {
        Task<bool> LookingForBlocks(UserDto userDto);
        int GetTimestamp();
    }
}