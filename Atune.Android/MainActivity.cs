using Android.App;
using Android.Content.PM;
using Avalonia;
using Avalonia.Android;
using Android.OS;
using Android;
using Android.Runtime;
using System.Diagnostics.CodeAnalysis;
using LibVLCSharp.Platforms.Android;
using LibVLCSharp.Shared;
using System.Reflection;
using System.Linq;

namespace Atune.Android;

[Activity(
    Label = "Atune",
    Theme = "@style/MyTheme.NoActionBar",
    Icon = "@drawable/icon",
    MainLauncher = true,
    ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.UiMode)]
public class MainActivity : AvaloniaMainActivity<App>
{
    private const int RequestStoragePermission = 1;

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        // Правильная инициализация через Core
        Core.Initialize();
        
        base.OnCreate(savedInstanceState);
        
        if ((int)Build.VERSION.SdkInt >= 23)
        {
            RequestStoragePermissions();
        }

        // Добавляем после инициализации Core
        AppDomain.CurrentDomain.AssemblyResolve += (sender, args) =>
        {
            var asmName = new AssemblyName(args.Name);
            return PluginLoader.LoadedAssemblies.FirstOrDefault(a => a.GetName().Name == asmName.Name);
        };
    }

    [SuppressMessage("Interoperability", "CA1416:Validate platform compatibility", 
        Justification = "Android version checked programmatically")]
    private void RequestStoragePermissions()
    {
        if ((int)Build.VERSION.SdkInt < 23) return;

        if (CheckSelfPermission(Manifest.Permission.ReadExternalStorage) 
            != Permission.Granted)
        {
            RequestPermissions(
                new[] { Manifest.Permission.ReadExternalStorage }, 
                RequestStoragePermission);
        }
    }

    [SuppressMessage("Interoperability", "CA1416:Validate platform compatibility", 
        Justification = "Base call guarded by version check")]
    public override void OnRequestPermissionsResult(
        int requestCode, 
        string[] permissions, 
        [GeneratedEnum] Permission[] grantResults)
    {
        if ((int)Build.VERSION.SdkInt < 23) return;

        if (requestCode == RequestStoragePermission)
        {
            if (grantResults.Length > 0 && 
                grantResults[0] == Permission.Granted)
            {
                // Permissions granted
            }
        }
        else
        {
            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }
    }

    protected override AppBuilder CustomizeAppBuilder(AppBuilder builder)
    {
        return base.CustomizeAppBuilder(builder)
            .WithInterFont();
    }
}
