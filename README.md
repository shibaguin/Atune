# Atune – Crossplatform Audio Player

Atune is a modern cross-platform audio player developed using .NET and AvaloniaUI. The project is designed to work in environments such as Linux, Windows, and Android, providing an intuitive and responsive interface for managing and listening to your media library.

## Features

- **Crossplatform:** Equal support for Linux, Windows and Android.
- **Adaptive interface:** Using AvaloniaUI allows creating a modern and customizable design that works on all supported platforms.
- **MVVM architecture:** Clear separation of view logic and business logic ensures ease of support and expansion of the project.
- **Localization:** Available multiple language packages (for now, Russian and English), which simplifies the adaptation of the application for the international community.
- **Customizable themes:** Support for switching between light, dark and system themes.
- **Media functions:** Functions for adding music, organizing playlists, managing favorites, playback history and more.
- **Plugins:** Support for plugins allows you to extend the functionality of the application.
- **Extensibility:** Modular code organization and use of Dependency Injection allows easy integration of third-party services and plugins.

## Requirements

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Avalonia](https://avaloniaui.net) — cross-platform UI framework
- [Entity Framework Core](https://docs.microsoft.com/en-us/ef/) — for working with local databases (SQLite)
- Additional: Android SDK and tools for building Android version

## Installation and build

1. **Clone the repository:**

   ```bash
   git clone https://github.com/shibaguin/Atune.git
   cd Atune
   ```

2. **Restore dependencies:**

   ```bash
   dotnet restore
   ```

3. **Build the project:**

   - **Linux (Desktop):** In the `Atune.Desktop` directory, run:
     ```bash
     dotnet publish -c Release --runtime linux-x64
     ```
     (Note: 32-bit builds (e.g. `linux-x86`) are supported "as is" but not actively tested.)

   - **Windows (Desktop):** In the `Atune.Desktop` directory, run:
     ```bash
     dotnet publish -c Release --runtime win-x64
     ```
     (Note: 32-bit builds (e.g. `win-x86`) are supported "as is" but not actively tested.)
     
   - **macOS (Desktop):** In the `Atune.Desktop` directory, run:
     ```bash
     dotnet publish -c Release --runtime osx-x64
     ```
     (For Apple Silicon, use `--runtime osx-arm64`; builds are not actively tested, and there are currently no dedicated contributors maintaining macOS or iOS support.)

   - **Android:** In the `Atune.Android` directory, ensure you have .NET SDK 8.0.405 installed (many systems default to 8.0.100, which may not support Android targets; check with `dotnet --list-sdks`). Use the TFM pattern `net8.0-android{API}`, replacing `{API}` with the desired Android API level. For example:
     - Android 15 (API 35): `net8.0-android35.0`
     - Android 14 (API 34): `net8.0-android34.0`
     - Android 13 (API 33): `net8.0-android33.0`
     - For other API levels, adjust accordingly.
     Then run:
     ```bash
     dotnet publish -c Release -f net8.0-android{API}
     ```

   - **iOS & Browser:** See [Building for iOS and browser](#building-for-ios-and-browser) for detailed commands and requirements.

4. **Run the application:**

   - For Desktop version:

      - Linux:
         In the Atune.Desktop/bin/Release/net8.0/linux-x64/publish directory, run the application **Atune.Desktop**:

         ```bash
         ./Atune.Desktop
         ```

      - Windows:
         In the Atune.Desktop/bin/Release/net8.0/win-x64/publish directory, run the application **Atune.Desktop.exe**

   - For Android version:
      In the Atune.Android/bin/Release/net8.0-android34.0/publish directory, find the file **Atune.Android.apk** and install it on the device.
      Or use suitable emulators and tools for building to run and test the application.

## Usage

Atune offers a simple and intuitive interface:
- **Navigation through sections:** Main, Media Library, History, Settings.
- **Music management:** Adding audio files, organizing folders, updating library.
- **Localization and themes:** Switching language and changing the theme of the application is done through the application settings.

Details of working with local resources and database can be found in the source code, where technologies Microsoft.EntityFrameworkCore and local .resx files for localization are used.

## Localization

Atune supports multiple languages, including Russian and English. Localization is performed through resource files, such as `en.resx` and `ru.resx`, which are located in the `Atune/Resources/Localization` directory. The community can add its localization files and make changes through GitHub. Language switching is done through the application settings menu.

## Plugins

Plugin support remains under active development. While the plugin architecture resides in the `Atune.Plugins` directory, loading and execution of plugins are not yet supported in current releases. Official plugin support is scheduled for a future major release; contributions to accelerate this effort are welcome.

Each plugin consists of:
- Logic implemented in files with the `.cs` extension.
- Identifier described in the `plugin.json` file, which contains metadata about the plugin, such as name and version.

## Building for iOS and browser

The solution includes `Atune.iOS` and `Atune.Browser` directories for additional targets:
- **iOS (`Atune.iOS`):** Requires macOS with .NET 8 SDK and Xcode command-line tools. In the `Atune.iOS` directory, run:
  ```bash
  dotnet publish -c Release -f net8.0-ios
  ```
  (This target is not actively tested and has no dedicated maintainers.)
- **Browser (`Atune.Browser`):** Avalonia WebAssembly project planned for near-future support. In the `Atune.Browser` directory, you can experiment by running:
  ```bash
  dotnet publish -c Release -f net8.0-webassembly
  ```
  (Experimental and not production-ready; contributions are welcome.)

Contributions to implement and maintain these platforms are highly encouraged.

## Testing

For testing, the xUnit framework is used. The `Atune.Tests` project already contains some tests that check the functionality of different application components. Examples of tests can be found in the following files:
- `HomeViewModelTests.cs`
- `LocalizationServiceTests.cs`
- `NavigationKeywordProviderTests.cs`
- `SettingsViewModelTests.cs`

### How to run tests

1. Go to the `Atune.Tests` directory:
  ```bash
  cd Atune.Tests
  ```

2. Restore dependencies:
  ```bash
  dotnet restore
  ```

3. Run tests:
  ```bash
  dotnet test
  ```

After executing these commands, you will see the test results in the console. Ensure that all tests pass to guarantee that changes do not break existing functionality.