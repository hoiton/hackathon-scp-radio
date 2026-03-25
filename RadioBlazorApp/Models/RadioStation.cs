namespace RadioBlazorApp.Models;

/// <summary>
/// Response-Wrapper für die MQTT Senderliste
/// </summary>
public class StationsResponse
{
    public List<RadioStation>? Stations { get; set; }
}

/// <summary>
/// Repräsentiert einen DAB+ Radio-Sender
/// </summary>
public class RadioStation
{
    /// <summary>
    /// Sendername (Label)
    /// </summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>
    /// Frequenz in MHz
    /// </summary>
    public double Mhz { get; set; }

    /// <summary>
    /// Eindeutige Service-ID des Senders
    /// </summary>
    public int ServiceId { get; set; }

    /// <summary>
    /// Sub-Channel ID
    /// </summary>
    public int SubChannelId { get; set; }

    /// <summary>
    /// Signalstärke (0-100)
    /// </summary>
    public int Strength { get; set; }

    // Computed properties für Kompatibilität mit der UI

    /// <summary>
    /// Alias für Label (UI-Kompatibilität)
    /// </summary>
    public string? Name => Label;

    /// <summary>
    /// Formatierte Frequenz-Anzeige
    /// </summary>
    public string? Frequency => $"{Mhz:F3} MHz";

    /// <summary>
    /// Alias für Strength (UI-Kompatibilität)
    /// </summary>
    public int SignalStrength => Strength;

    /// <summary>
    /// Genre (immer DAB+ für diese Implementierung)
    /// </summary>
    public string? Genre => "DAB+";
}
