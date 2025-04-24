namespace Atune.Data.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Atune.Data.Interfaces;
using Atune.Models;

public class PlayHistoryRepository(AppDbContext context) : IPlayHistoryRepository
{
    private readonly AppDbContext _context = context;

    public async Task<PlayHistory?> GetByIdAsync(int id)
    {
        return await _context.PlayHistories.FindAsync(id);
    }

    public async Task<IEnumerable<PlayHistory>> GetAllAsync()
    {
        return await _context.PlayHistories.ToListAsync();
    }

    public async Task AddAsync(PlayHistory entity)
    {
        await _context.PlayHistories.AddAsync(entity);
    }

    public async Task UpdateAsync(PlayHistory entity)
    {
        _context.Entry(entity).State = EntityState.Modified;
        await Task.CompletedTask;
    }

    public async Task DeleteAsync(PlayHistory entity)
    {
        _context.PlayHistories.Remove(entity);
        await Task.CompletedTask;
    }

    public async Task<IEnumerable<PlayHistory>> GetByMediaItemAsync(int mediaItemId)
    {
        return await _context.PlayHistories
            .Where(ph => ph.MediaItemId == mediaItemId)
            .ToListAsync();
    }

    public async Task<IEnumerable<PlayHistory>> GetByMediaItemAndPeriodAsync(int mediaItemId, DateTime from, DateTime to)
    {
        return await _context.PlayHistories
            .Where(ph => ph.MediaItemId == mediaItemId && ph.PlayedAt >= from && ph.PlayedAt <= to)
            .ToListAsync();
    }

    public async Task<IEnumerable<PlayHistory>> GetBySessionAsync(Guid sessionId)
    {
        return await _context.PlayHistories
            .Where(ph => ph.SessionId == sessionId)
            .ToListAsync();
    }
}