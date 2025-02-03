[assembly: SuppressMessage("Trimming", "IL2026", 
    Scope = "member", 
    Target = "T:Atune.Models.Media.Track", 
    MessageId = "get_Title")]
[assembly: SuppressMessage("Trimming", "IL2026", 
    Scope = "member", 
    Target = "T:Atune.Models.Media.Track", 
    MessageId = "set_Title")]
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
[assembly: SuppressMessage("Trimming", "IL2026", 
    Scope = "type", 
    Target = "T:Microsoft.EntityFrameworkCore.DbContextOptionsBuilder`1",
    Justification = "EF Core requires unreferenced code")] 