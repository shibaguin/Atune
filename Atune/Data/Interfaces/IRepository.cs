using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using System;

public interface IRepository<T> where T : class
{
    Task<T?> GetByIdAsync(int id);
    Task<IEnumerable<T>> GetAllAsync();
    Task AddAsync(T entity);
    Task UpdateAsync(T entity);
    Task DeleteAsync(T entity);
}

public class Repository<T>(AppDbContext context) : IRepository<T> where T : class
{
    private readonly AppDbContext _context = context;

    public async Task<T?> GetByIdAsync(int id)
    {
        return await _context.Set<T>().FindAsync(id);
    }

    public async Task<IEnumerable<T>> GetAllAsync()
    {
        return await _context.Set<T>().ToListAsync();
    }

    public async Task AddAsync(T entity)
    {
        await _context.Set<T>().AddAsync(entity);
    }

    public async Task UpdateAsync(T entity)
    {
        try
        {
            _context.Entry(entity).State = EntityState.Modified;
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            // Логируем ошибку
            throw new InvalidOperationException("Error updating entity", ex);
        }
    }

    public async Task DeleteAsync(T entity)
    {
        try
        {
            _context.Set<T>().Remove(entity);
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            // Логируем ошибку
            throw new InvalidOperationException("Error deleting entity", ex);
        }
    }
}
