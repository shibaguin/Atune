// Подавление предупреждений об обрезке для свойств Track.Title
// Suppressing trimming warnings for Track.Title properties
[assembly: SuppressMessage("Trimming", "IL2026", 
    Scope = "member", 
    Target = "T:Atune.Models.Media.Track", 
    MessageId = "get_Title")]
[assembly: SuppressMessage("Trimming", "IL2026", 
    Scope = "member", 
    Target = "T:Atune.Models.Media.Track", 
    MessageId = "set_Title")]

// Подавление для DbContext и его членов - EF Core требует полной доступности типов
// Suppression for DbContext and members - EF Core requires full type accessibility
[assembly: SuppressMessage("Trimming", "IL2026", 
    Scope = "type", 
    Target = "T:Atune.Data.AppDbContext",
    Justification = "EF Core требует всех членов")]
[assembly: SuppressMessage("Trimming", "IL2026", 
    Scope = "member", 
    Target = "M:Atune.Data.AppDbContext.#ctor",
    Justification = "EF Core Design-time context")]
[assembly: SuppressMessage("Trimming", "IL2026", 
    Scope = "member", 
    Target = "M:Atune.Data.AppDbContext.get_MediaItems",
    Justification = "EF Core requires unreferenced code")]
[assembly: SuppressMessage("Trimming", "IL2026", 
    Scope = "member", 
    Target = "M:Atune.Data.AppDbContext.set_MediaItems(Microsoft.EntityFrameworkCore.DbSet{Atune.Models.MediaItem})",
    Justification = "EF Core requires unreferenced code")]

// Подавление для DbContextOptionsBuilder - инфраструктура EF Core
// Suppression for DbContextOptionsBuilder - EF Core infrastructure
[assembly: SuppressMessage("Trimming", "IL2026", 
    Scope = "type", 
    Target = "T:Microsoft.EntityFrameworkCore.DbContextOptionsBuilder`1",
    Justification = "EF Core requires unreferenced code")]

// Подавление проверки совместимости платформ для Android-специфичных методов
// Suppressing platform compatibility checks for Android-specific methods
[assembly: SuppressMessage("Interoperability", "CA1416:Validate platform compatibility", 
    Scope = "member", 
    Target = "~M:Atune.Android.MainActivity.RequestStoragePermissions", 
    Justification = "Android version checked programmatically")]
[assembly: SuppressMessage("Interoperability", "CA1416:Validate platform compatibility", 
    Scope = "member", 
    Target = "~M:Atune.Android.MainActivity.OnRequestPermissionsResult(System.Int32,System.String[],Android.Content.PM.Permission[])", 
    Justification = "Base call guarded by version check")] 