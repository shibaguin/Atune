namespace Atune.Models;
using System;

public class HistoryItem
{
    public DateTime Timestamp { get; set; }
    public string Query { get; set; } = string.Empty;
    public string Result { get; set; } = string.Empty;
}
