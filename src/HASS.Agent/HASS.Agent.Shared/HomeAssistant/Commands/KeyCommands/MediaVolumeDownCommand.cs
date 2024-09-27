using HASS.Agent.Shared.Enums;
using static HASS.Agent.Shared.Functions.Inputs;

namespace HASS.Agent.Shared.HomeAssistant.Commands.KeyCommands;

/// <summary>
/// Simulates a 'volume down' mediakey press
/// </summary>
public class MediaVolumeDownCommand(
    string? entityName = MediaVolumeDownCommand.DefaultName,
    string? name = MediaVolumeDownCommand.DefaultName,
    CommandEntityType entityType = CommandEntityType.Switch,
    string? id = default)
    : KeyCommand(VirtualKeyShort.VOLUME_DOWN, entityName ?? DefaultName, name ?? null, entityType, id)
{
    private const string DefaultName = "volumedown";
}