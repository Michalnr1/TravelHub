﻿using TravelHub.Domain.Entities;
using TravelHub.Domain.Interfaces.Repositories;
using TravelHub.Domain.Interfaces.Services;

namespace TravelHub.Infrastructure.Services;

public class CategoryService : GenericService<Category>, ICategoryService
{
    private readonly ICategoryRepository _categoryRepository;

    public CategoryService(ICategoryRepository categoryRepository) : base(categoryRepository)
    {
        _categoryRepository = categoryRepository;
    }

    public async Task<bool> ExistsByNameAsync(string name)
    {
        return await _categoryRepository.ExistsByNameAsync(name);
    }

    public async Task<bool> IsInUseAsync(int categoryId)
    {
        return await _categoryRepository.IsInUseAsync(categoryId);
    }
}
