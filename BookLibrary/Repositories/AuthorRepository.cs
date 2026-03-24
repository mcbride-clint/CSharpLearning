using BookLibrary.Data;
using BookLibrary.Interfaces;
using BookLibrary.Models;
using Microsoft.EntityFrameworkCore;

namespace BookLibrary.Repositories;

public class AuthorRepository : IAuthorRepository
{
    private readonly LibraryDbContext _db;

    public AuthorRepository(LibraryDbContext db)
    {
        _db = db;
    }

    public async Task<IEnumerable<Author>> GetAllAsync()
    {
        return await _db.Authors.AsNoTracking().OrderBy(a => a.LastName).ToListAsync();
    }

    public async Task<Author?> GetByIdAsync(int id)
    {
        return await _db.Authors.FindAsync(id);
    }

    public async Task<Author?> GetByIdWithBooksAsync(int id)
    {
        // ThenInclude: nested eager loading. Here we load:
        //   Author → Books → Category (for each book)
        // This generates a single SQL query with two JOINs.
        return await _db.Authors
            .Include(a => a.Books)
                .ThenInclude(b => b.Category)
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == id);
    }

    public async Task AddAsync(Author author)
    {
        _db.Authors.Add(author);
        await _db.SaveChangesAsync();
    }

    public async Task UpdateAsync(Author author)
    {
        _db.Update(author);
        await _db.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var author = await _db.Authors.FindAsync(id);
        if (author is not null)
        {
            _db.Authors.Remove(author);
            await _db.SaveChangesAsync();
        }
    }

    public async Task<bool> ExistsAsync(int id)
    {
        return await _db.Authors.AnyAsync(a => a.Id == id);
    }

    public async Task<bool> HasBooksAsync(int authorId)
    {
        // Efficient existence check: doesn't load any book data, just checks
        // if at least one book with this AuthorId exists.
        return await _db.Books.AnyAsync(b => b.AuthorId == authorId);
    }
}
