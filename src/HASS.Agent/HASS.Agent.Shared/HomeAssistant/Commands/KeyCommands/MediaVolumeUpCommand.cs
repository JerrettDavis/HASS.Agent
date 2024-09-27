using HASS.Agent.Shared.Enums;
using static HASS.Agent.Shared.Functions.Inputs;

namespace HASS.Agent.Shared.HomeAssistant.Commands.KeyCommands;

/// <summary>
/// Simulates a 'volume up' mediakey press
/// </summary>
public class MediaVolumeUpCommand(
    string? entityName = MediaVolumeUpCommand.DefaultName,
    string? name = MediaVolumeUpCommand.DefaultName,
    CommandEntityType entityType = CommandEntityType.Switch,
    string? id = default)
    : KeyCommand(VirtualKeyShort.VOLUME_UP,
        entityName ?? DefaultName, name ?? null, entityType, id)
{
    private const string DefaultName = "volumeup";
}