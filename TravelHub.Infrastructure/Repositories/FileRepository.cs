using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TravelHub.Domain.Entities;
using TravelHub.Domain.Interfaces.Repositories;
using File = TravelHub.Domain.Entities.File;

namespace TravelHub.Infrastructure.Repositories;

public class FileRepository : GenericRepository<File>, IFileRepository
{
    public FileRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<IReadOnlyList<File>> GetFilesBySpotIdAsync(int spotId)
    {
        return await _context.Set<File>()
            .Where(p => p.SpotId == spotId)
            .Include(p => p.Spot)
            .ToListAsync();
    }
}