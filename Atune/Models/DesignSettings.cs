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
            public static double BarPadding { get; }

            static Dimensions()
            {
                // Инициализация значений
                // Initialization values
                HeaderFontSize = 24;
                NavigationDividerWidth = 3;
                BarHeight = 50;
                NavigationDividerHeight = BarHeight;
                TopDockHeight = 50;
                NavigationFontSize = 14;
                BarPadding = 8;

                // Проверка значений
                // Validation values
                ValidateValues();
            }

            private static void ValidateValues()
            {
                if (HeaderFontSize > TopDockHeight)
                {
                    throw new System.InvalidOperationException(
                        $"HeaderFontSize ({HeaderFontSize}) cannot be greater than TopDockHeight ({TopDockHeight})");
                }

                if (NavigationDividerHeight > BarHeight)
                {
                    throw new System.InvalidOperationException(
                        $"NavigationDividerHeight ({NavigationDividerHeight}) cannot be greater than BarHeight ({BarHeight})");
                }
            }
        }
    }
}
