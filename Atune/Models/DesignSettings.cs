namespace Atune.Models
{
    public static class DesignSettings
    {
        public static class Dimensions
        {
            public static double HeaderFontSize { get; }
            public static double NavigationDividerWidth { get; }
            public static double NavigationDividerHeight { get; }
            public static double TopDockHeight { get; }
            public static double BarHeight { get; }
            public static double NavigationFontSize { get; }
            
            static Dimensions()
            {
                // Инициализация значений
                HeaderFontSize = 30;
                NavigationDividerWidth = 3;
                NavigationDividerHeight = 60;
                TopDockHeight = 50;
                BarHeight = 60;
                NavigationFontSize = 14;

                // Валидация значений
                ValidateValues();
            }

            private static void ValidateValues()
            {
                if (HeaderFontSize > TopDockHeight)
                {
                    throw new System.InvalidOperationException(
                        $"HeaderFontSize ({HeaderFontSize}) не может быть больше TopDockHeight ({TopDockHeight})");
                }

                if (NavigationDividerHeight > BarHeight)
                {
                    throw new System.InvalidOperationException(
                        $"NavigationDividerHeight ({NavigationDividerHeight}) не может быть больше BarHeight ({BarHeight})");
                }
            }
        }

        // Пример использования цветов
        /*
        public static class Colors
        {
            public const string PrimaryColor = "#2C3E50";
            public const string SecondaryColor = "#3498DB";
        }
        */
    }
}