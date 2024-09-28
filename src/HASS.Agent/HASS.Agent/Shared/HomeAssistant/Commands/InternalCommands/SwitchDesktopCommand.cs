using HASS.Agent.Managers;
using HASS.Agent.Shared.Enums;
using Serilog;

namespace HASS.Agent.Shared.HomeAssistant.Commands.InternalCommands;

/// <summary>
/// Activates provided Virtual Desktop
/// </summary>
public class SwitchDesktopCommand : InternalCommand
{
    private const string DefaultName = "switchdesktop";

    public SwitchDesktopCommand(
        string? entityName = DefaultName,
        string? name = DefaultName,
        string desktopId = "",
        CommandEntityType entityType = CommandEntityType.Switch,
        string? id = default) : base(
        entityName ?? DefaultName,
        name ?? DefaultName,
        desktopId,
        entityType,
        id)
    {
        CommandConfig = desktopId;
        State = "OFF";
    }

    public override void TurnOn()
    {
        TurnOnInternal(CommandConfig);
    }

    public override void TurnOnWithAction(string action)
    {
        if (string.IsNullOrWhiteSpace(CommandConfig))
            TurnOnInternal(action);
        else
            LogIgnoredAction();
    }

    private void TurnOnInternal(string command)
    {
        State = "ON";

        if (string.IsNullOrWhiteSpace(command))
        {
            LogWarning("Unable to launch command, empty action or config provided");
        }
        else
        {
            ActivateVirtualDesktop(command);
        }

        State = "OFF";
    }

    private void LogWarning(string message) => 
        Log.Warning("[SWITCHDESKTOP] [{name}] {message}", EntityName, message);

    private void LogIgnoredAction() => 
        LogWarning("Command launched by action, command-provided process will be ignored");

    private static void ActivateVirtualDesktop(string virtualDesktopId)
    {
        VirtualDesktopManager.ActivateDesktop(virtualDesktopId);
    }
}