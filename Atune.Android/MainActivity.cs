using Android.App;
using Android.Content.PM;
using Avalonia;
using Avalonia.Android;
using Android.OS;
using Android;
using Android.Runtime;

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

    protected override void OnCreate(Bundle savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        if ((int)Build.VERSION.SdkInt >= 23)
        {
            RequestStoragePermissions();
        }
    }

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
