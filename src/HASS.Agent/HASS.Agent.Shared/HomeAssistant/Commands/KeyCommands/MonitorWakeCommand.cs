using HASS.Agent.Shared.Enums;
using static HASS.Agent.Shared.Functions.Inputs;

namespace HASS.Agent.Shared.HomeAssistant.Commands.KeyCommands;

/// <summary>
/// Simulates a 'arrow up' key press to wake the monitors
/// https://stackoverflow.com/a/42393472 ?
/// </summary>
public class MonitorWakeCommand(
    string? entityName = MonitorWakeCommand.DefaultName,
    string? name = MonitorWakeCommand.DefaultName,
    CommandEntityType entityType = CommandEntityType.Button,
    string? id = default)
    : KeyCommand(VirtualKeyShort.UP,
        entityName ?? DefaultName, name ?? null, entityType, id)
{
    private const string DefaultName = "monitorwake";
}