using HASS.Agent.Shared.Enums;
using static HASS.Agent.Shared.Functions.Inputs;

namespace HASS.Agent.Shared.HomeAssistant.Commands.KeyCommands;

/// <summary>
/// Simulates a 'playpause' mediakey press
/// </summary>
public class MediaPlayPauseCommand(
    string? entityName = MediaPlayPauseCommand.DefaultName,
    string? name = MediaPlayPauseCommand.DefaultName,
    CommandEntityType entityType = CommandEntityType.Switch,
    string? id = default)
    : KeyCommand(VirtualKeyShort.MEDIA_PLAY_PAUSE, entityName ?? DefaultName, name ?? null, entityType, id)
{
    private const string DefaultName = "playpause";
}