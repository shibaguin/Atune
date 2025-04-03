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

   - For Desktop:
   In the Atune.Desktop directory, run the command:

     ```bash
     dotnet publish -c Release --runtime {platform}

     ```
     Where {platform} is the platform you want to build the application on.
     For example:
     win-x64
     win-x86
     linux-x64
     
   - For Android:
      In the Atune.Android directory, run the command:
     ```bash
     dotnet publish -c Release -f net8.0-android34.0
     ```
     Or use suitable tools for building and running the application, for example Rider IDE.

   - For iOS and browser version, there are separate projects that require implementation and refinement, as well as tools for building.

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

Atune supports a plugin system that allows extending the functionality of the application. Plugins are loaded from the `Atune.Plugins` directory, for example, `Atune.Plugin.SpectrumVisualizer`.

Each plugin consists of:
- Logic implemented in files with the `.cs` extension.
- Identifier described in the `plugin.json` file, which contains metadata about the plugin, such as name and version.

Example of a plugin:
- `Atune.Plugin.SpectrumVisualizer` contains the `Plugin.cs` file, which implements the `IAudioVisualizerPlugin` interface.

Currently, the functionality of plugins is under development, and they cannot be executable or loaded into the program itself. In the future, we plan to complete the implementation of this system, which will allow users to add and use plugins to expand the capabilities of Atune.

## Building for iOS and browser

Although iOS and browser are not target platforms for Atune now, Avalonia supports them. The project contains corresponding directories `Atune.iOS` and `Atune.Browser`, however, at the moment they are not supported. The community can take on their implementation, as they are not significantly different from `Atune.Desktop` and `Atune.Android`.

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


## Contribution

We welcome contributions from the community and external developers! If you have ideas, improvements or found errors:
- **GitHub Issues:** Create tasks for discussion and tracking.
- **Pull Requests:** Send pull requests with your suggestions.

Please refer to [CONTRIBUTING.md](CONTRIBUTING.md) for detailed information on the contribution process. We recommend following the code standards and writing tests for new features or corrections.

## Community

Join the discussions and share your ideas:
- **GitHub Discussions/Issues:** For questions and suggestions.
- **Chats/forums:** (If available, provide links to Discord, Telegram or other platforms).

## License

The project is distributed under the **GNU Lesser General Public License v3.0 (LGPL-3.0)**. This guarantees:
- Free use, modification and distribution of software
- The obligation to preserve copyright and license notices
- The requirement to open the source code of derivatives when distributing

The full text of the license is available in the file [LICENSE](LICENSE).

## Contacts

If you have any questions or suggestions, please contact us through:
- [GitHub](https://github.com/shibaguin/Atune)
- [Telegram](https://t.me/dazabrzezinski)
- [Email](mailto:mizuguinalt@gmail.com)

---

Thank you for using Atune! We hope that our audio player will become your reliable assistant in the world of high-quality sound.