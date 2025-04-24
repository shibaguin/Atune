namespace Atune.Services.Search;

using Atune.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

public interface ISearchProvider
{
    string Name { get; }
    Task<IEnumerable<SearchResult>> SearchAsync(string query);
}
