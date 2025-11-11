using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TravelHub.Domain.Entities;
using File = TravelHub.Domain.Entities.File;

namespace TravelHub.Domain.Interfaces.Services;

public interface IFileService : IGenericService<File>
{
    Task<IReadOnlyList<File>> GetBySpotIdAsync(int spotId);
    Task<File> AddFileAsync(File file, Stream fileStream, string fileName, string webRootPath);

    Task DeleteFileAsync(int id, string webRootPath);
}
