using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TravelHub.Domain.Entities;

namespace TravelHub.Domain.Interfaces.Services;

public interface IPhotoService : IGenericService<Photo>
{
    Task<IReadOnlyList<Photo>> GetBySpotIdAsync(int spotId);
    Task<Photo> AddPhotoAsync(Photo photo, Stream fileStream, string fileName, string webRootPath);

    Task DeletePhotoAsync(int id, string webRootPath);
}
