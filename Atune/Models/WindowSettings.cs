using System;

namespace Atune.Models;

public class WindowSettings
{
    public double X { get; set; }
    public double Y { get; set; }
    public double Width { get; set; }
    public double Height { get; set; }
    public bool IsMaximized { get; set; }
    public string CurrentPage { get; set; } = "MediaView";
}