using TeleICU.API.DTOs.CoeDtos;
using TeleICU.API.DTOs.SpokeDtos;

namespace TeleICU.API.Services.Interface
{
    public interface IBedsService
    {
        Task<IEnumerable<BedsDto>> GetBeds(int spokeId);
    }
}
