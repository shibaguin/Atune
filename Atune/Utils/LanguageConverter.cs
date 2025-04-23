using System;

namespace Atune.Utils
{
    // Предоставляет методы для преобразования между языковыми кодами и отображаемыми названиями.
    // Provides methods for converting between language codes and display names.
    public static class LanguageConverter
    {
        // Преобразует код языка в отображаемое название.
        // Converts a language code to a display name.
        public static string CodeToDisplay(string code)
        {
            return code switch
            {
                "ru" => "Русский",
                "en" => "English",
                _    => code
            };
        }

        // Преобразует отображаемое название языка в код.
        // Converts the display name of a language to a code.
        public static string DisplayToCode(string display)
        {
            return display switch
            {
                "Русский" => "ru",
                "English" => "en",
                _         => display
            };
        }
    }
} 
