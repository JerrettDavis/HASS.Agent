using System.Diagnostics.CodeAnalysis;

namespace HASS.Agent.Shared.Managers.Audio;

[SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")]
public class AudioSession
{
    public string? Id { get; set; }
    public string Application { get; set; } = string.Empty;
    public string PlaybackDevice { get; set; } = string.Empty;
    public bool Muted { get; set; }
    public bool Active { get; set; }
    public int MasterVolume { get; set; }
    public double PeakVolume { get; set; }
}