using TeleICU.API.DTOs.SpokeDtos;

namespace TeleICU.API.Repository.Interface
{
    public interface IBedsRepository
    {
        Task<IEnumerable<BedsDto>> GetBeds(int spokeId);
    }
}
