using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TravelHub.Domain.Entities;
using File = TravelHub.Domain.Entities.File;

namespace TravelHub.Domain.Interfaces.Repositories;

public interface IFileRepository : IGenericRepository<File>
{
    Task<IReadOnlyList<File>> GetFilesBySpotIdAsync(int spotId);
}
