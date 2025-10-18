using System.Collections.Generic;
using System.Threading.Tasks;

namespace TravelHub.Domain.Interfaces.Repositories;

// Generyczny interfejs bazowy dla wszystkich repozytoriów
public interface IGenericRepository<T> where T : class
{
    Task<T?> GetByIdAsync(object id);
    Task<IReadOnlyList<T>> GetAllAsync();
    Task<T> AddAsync(T entity);
    Task UpdateAsync(T entity);
    Task DeleteAsync(T entity);
}