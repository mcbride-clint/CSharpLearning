using BookLibrary.Data;
using BookLibrary.Interfaces;
using BookLibrary.Models;
using Microsoft.EntityFrameworkCore;

namespace BookLibrary.Repositories;

public class CategoryRepository : ICategoryRepository
{
    private readonly LibraryDbContext _db;

    public CategoryRepository(LibraryDbContext db)
    {
        _db = db;
    }

    public async Task<IEnumerable<Category>> GetAllAsync()
    {
        return await _db.Categories.AsNoTracking().OrderBy(c => c.Name).ToListAsync();
    }

    public async Task<Category?> GetByIdAsync(int id)
    {
        return await _db.Categories.FindAsync(id);
    }

    public async Task AddAsync(Category category)
    {
        _db.Categories.Add(category);
        await _db.SaveChangesAsync();
    }

    public async Task UpdateAsync(Category category)
    {
        _db.Update(category);
        await _db.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var category = await _db.Categories.FindAsync(id);
        if (category is not null)
        {
            _db.Categories.Remove(category);
            await _db.SaveChangesAsync();
        }
    }

    public async Task<bool> ExistsAsync(int id)
    {
        return await _db.Categories.AnyAsync(c => c.Id == id);
    }

    public async Task<bool> HasBooksAsync(int categoryId)
    {
        return await _db.Books.AnyAsync(b => b.CategoryId == categoryId);
    }
}
