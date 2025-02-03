AppDomain.CurrentDomain.UnhandledException += (s, e) => 
{
    File.WriteAllText("crash.log", $"CRITICAL ERROR: {e.ExceptionObject}");
    Environment.Exit(1);
}; 