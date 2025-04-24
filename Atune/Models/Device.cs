namespace Atune.Models;
using System;
using System.Collections.Generic;

// TODO: Future Feature - Cross-Device Synchronization
// Represents a user's device for syncing playback history across devices
// via a central account (e.g., Google Account + Google Drive).
public class Device
{
    // Primary key
    public int Id { get; set; }

    // Globally unique device identifier
    public Guid DeviceIdentifier { get; set; }

    // Display name for the device
    public string Name { get; set; } = string.Empty;

    // Manufacturer and model information
    public string Manufacturer { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;

    // Operating system details
    public string OSName { get; set; } = string.Empty;
    public string OSVersion { get; set; } = string.Empty;

    // Navigation: playback histories recorded on this device
    public ICollection<PlayHistory> PlayHistories { get; set; } = new List<PlayHistory>();
} 