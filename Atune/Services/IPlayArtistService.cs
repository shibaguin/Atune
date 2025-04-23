using System.Threading.Tasks;
using Atune.Models;

namespace Atune.Services
{
    public interface IPlayArtistService
    {
        Task PlayArtistAsync(ArtistInfo artist);
    }
} 