using System;

namespace Atune.Utils
{
    /// <summary>
    /// Предоставляет методы для преобразования между языковыми кодами и отображаемыми названиями.
    /// </summary>
    public static class LanguageConverter
    {
        // Преобразует код языка в отображаемое название.
        public static string CodeToDisplay(string code)
        {
            return code switch
            {
                "ru" => "Русская",
                "en" => "English",
                _    => code
            };
        }

        // Преобразует отображаемое название языка в код.
        public static string DisplayToCode(string display)
        {
            return display switch
            {
                "Русская" => "ru",
                "English" => "en",
                _         => display
            };
        }
    }
} 