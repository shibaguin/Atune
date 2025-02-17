using System;
using System.Collections.Generic;
using System.Linq;
using Atune.Views;

namespace Atune.ViewModels
{
    public class NavigationKeywordProvider : INavigationKeywordProvider
{
    public Dictionary<MainViewModel.SectionType, IEnumerable<string>> GetNavigationKeywords() =>
        new Dictionary<MainViewModel.SectionType, IEnumerable<string>>
        {
            { MainViewModel.SectionType.Settings, new[] { "настройки", "настрой", "настр", "нарстойки", "settings", "setting" } },
            { MainViewModel.SectionType.Media, new[] { "медиатека", "медиатек", "музыка", "меломан", "media", "library" } },
            { MainViewModel.SectionType.History, new[] { "история", "истор", "запрос", "history" } },
            { MainViewModel.SectionType.Home, new[] { "главная", "главн", "атюн", "home", "atune" } }
        };
    }
} 