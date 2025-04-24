namespace Atune.Data.Interfaces
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Atune.Models;

    public interface IPlayHistoryRepository : IRepository<PlayHistory>
    {
        Task<IEnumerable<PlayHistory>> GetByMediaItemAsync(int mediaItemId);
        Task<IEnumerable<PlayHistory>> GetByMediaItemAndPeriodAsync(int mediaItemId, DateTime from, DateTime to);
        Task<IEnumerable<PlayHistory>> GetBySessionAsync(Guid sessionId);
    }
}