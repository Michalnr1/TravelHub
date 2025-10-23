﻿using TravelHub.Domain.Entities;

namespace TravelHub.Domain.Interfaces.Services;

public interface IAccommodationService : IGenericService<Accommodation>
{
    Task<Accommodation?> GetByIdWithDetailsAsync(int id);
    Task<IEnumerable<Accommodation>> GetAccommodationByTripAsync(int tripId);
}