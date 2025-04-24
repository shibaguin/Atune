using System.Collections.Generic;
using Atune.ViewModels;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Atune.ViewModels
{
    public interface INavigationKeywordProvider
    {
        Dictionary<MainViewModel.SectionType, IEnumerable<string>> GetNavigationKeywords();
    }
}
