using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using TravelHub.Domain.Entities;

namespace TravelHub.Domain.Interfaces.Services;

public interface IReverseGeocodingService
{
    Task<(string?, string?, string?)> GetCountryAndCity(double lat, double lng);

    //Task TestThrottlingRate();
}
