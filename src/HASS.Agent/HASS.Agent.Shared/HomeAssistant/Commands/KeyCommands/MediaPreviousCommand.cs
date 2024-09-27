using HASS.Agent.Shared.Enums;
using static HASS.Agent.Shared.Functions.Inputs;

namespace HASS.Agent.Shared.HomeAssistant.Commands.KeyCommands;

/// <summary>
/// Simulates a 'previous' mediakey press
/// </summary>
public class MediaPreviousCommand(
    string? entityName = MediaPreviousCommand.DefaultName,
    string? name = MediaPreviousCommand.DefaultName,
    CommandEntityType entityType = CommandEntityType.Switch,
    string? id = default)
    : KeyCommand(VirtualKeyShort.MEDIA_PREV_TRACK, entityName ?? DefaultName, name ?? null, entityType, id)
{
    private const string DefaultName = "previous";
}