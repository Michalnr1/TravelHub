using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TravelHub.Domain.DTOs;

namespace TravelHub.Domain.Interfaces.Services;

public interface IRecommendationService
{
    Task<List<AttractionDto>> FindRecommendationsByCoords(double lat, double lng, int distanceMeters);
    Task<string> GetPhotoUrl(string photoName);
}
