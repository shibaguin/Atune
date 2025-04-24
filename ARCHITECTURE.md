# Application Architecture of Atune

This document describes the main layers of the application:

- ViewModels
- Services
- Repositories
- DbContext
- Plugins

## 1. ViewModels
Folder: `Atune/ViewModels`
Description:
Contains presentation models (ViewModels) for the MVVM pattern, providing a link between views and data models.
Key classes:
- **MainViewModel.cs** – the central ViewModel managing application navigation and state.
- **MediaViewModel.cs** – handles logic for displaying and managing media content.
- **AlbumViewModel.cs**, **ArtistViewModel.cs**, **PlaylistViewModel.cs**, etc. – specific ViewModels for corresponding entities.
- **SettingsViewModel.cs** – manages application settings.
- **ViewModelBase.cs** – base class implementing INotifyPropertyChanged.

## 2. Services
Folder: `Atune/Services`
Description:
Business logic and services layer; performs data operations, coordinating repositories and external APIs.
Key classes and interfaces:
- **MediaLibraryService.cs**, **MediaPlayerService.cs**, **MusicPlaybackService.cs** – services for media library and playback.
- **SettingsService.cs**, **LocalizationService.cs** – services for handling settings and localization.
- **PluginLoader.cs** – loads and initializes plugins.
- Interfaces: `ISettingsService`, `IMediaStorageService`, `IPlatformPathService`, etc.

## 3. Repositories
Folder: `Atune/Data/Repositories`
Description:
Data access layer implementing the Repository pattern for database operations via DbContext.
Key classes:
- **MediaRepository.cs**, **PlaylistRepository.cs**, **AlbumRepository.cs**, **ArtistRepository.cs** – access to tables corresponding to entities.
- **PlayHistoryRepository.cs**, **FoldersRepository.cs** – specialized repositories for playback history and folders.
- Interfaces: `IMediaRepository`, `IPlaylistRepository`, `IAlbumRepository`, `IArtistRepository`, etc.

## 4. DbContext
File: `Atune/Data/AppDbContext.cs`
Description:
The primary EF Core database context containing DbSet<> for all application entities and configuring relationships and migrations.

## 5. Plugins
Folders: `Atune/Plugins` and `Atune/Plugins/Abstractions`
Description:
Extension layer that allows integrating external modules.
- **IPlugin.cs** – interface for plugin implementations.
- Plugins are dynamically loaded by the `PluginLoader` service. 