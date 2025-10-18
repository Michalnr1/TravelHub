namespace TravelHub.Domain.Interfaces.Services;

// Generyczny interfejs bazowy dla wszystkich serwisów
public interface IGenericService<T> where T : class
{
    Task<T> GetByIdAsync(object id);
    Task<IReadOnlyList<T>> GetAllAsync();
    Task<T> AddAsync(T entity);
    Task UpdateAsync(T entity);
    Task DeleteAsync(object id); // Zmieniamy na object id, aby ułatwić implementację w serwisie
}
