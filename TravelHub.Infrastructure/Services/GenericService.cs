using System.Collections.Generic;
using System.Threading.Tasks;
using TravelHub.Domain.Interfaces.Repositories;
using TravelHub.Domain.Interfaces.Services;

namespace TravelHub.Infrastructure.Services
{
    public class GenericService<T> : IGenericService<T> where T : class
    {
        protected readonly IGenericRepository<T> _repository;

        public GenericService(IGenericRepository<T> repository)
        {
            _repository = repository;
        }

        public async Task<T> GetByIdAsync(object id)
        {
            return await _repository.GetByIdAsync(id);
        }

        public async Task<IReadOnlyList<T>> GetAllAsync()
        {
            return await _repository.GetAllAsync();
        }

        public async Task<T> AddAsync(T entity)
        {
            // Można tu dodać generyczną logikę biznesową/walidację
            return await _repository.AddAsync(entity);
        }

        public async Task UpdateAsync(T entity)
        {
            // Można tu dodać generyczną logikę biznesową/walidację
            await _repository.UpdateAsync(entity);
        }

        public async Task DeleteAsync(object id)
        {
            var entityToDelete = await _repository.GetByIdAsync(id);
            if (entityToDelete != null)
            {
                await _repository.DeleteAsync(entityToDelete);
            }
        }
    }
}
