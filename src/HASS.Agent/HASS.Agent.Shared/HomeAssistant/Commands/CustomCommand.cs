using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using HASS.Agent.Shared.Enums;
using HASS.Agent.Shared.Managers;
using HASS.Agent.Shared.Models.HomeAssistant;
using Serilog;

namespace HASS.Agent.Shared.HomeAssistant.Commands;

/// <summary>
/// Command to perform an action through a console, either normal or with low integrity
/// </summary>
[SuppressMessage("ReSharper", "AutoPropertyCanBeMadeGetOnly.Global")]
public class CustomCommand(
    string command,
    bool runAsLowIntegrity,
    string? entityName = CustomCommand.DefaultName,
    string? name = CustomCommand.DefaultName,
    CommandEntityType entityType = CommandEntityType.Switch,
    string? id = default)
    : AbstractCommand(entityName ?? DefaultName, name ?? null, entityType, id)
{
    private const string DefaultName = "custom";
        
    public string Command { get; protected set; } = command;
    public string State { get; protected set; } = "OFF";
    public bool RunAsLowIntegrity { get; protected set; } = runAsLowIntegrity;
    public Process? Process { get; set; } = null;

    public override void TurnOn()
    {
        State = "ON";

        if (string.IsNullOrWhiteSpace(Command))
        {
            Log.Warning("[CUSTOMCOMMAND] [{name}] Unable to launch command, it's configured as action-only", EntityName);
            State = "OFF";
            return;
        }

        if (RunAsLowIntegrity) CommandLineManager.LaunchAsLowIntegrity(Command);
        else
        {
            var executed = CommandLineManager.ExecuteHeadless(Command);

            if (!executed) Log.Error("[CUSTOMCOMMAND] [{name}] Launching command failed", EntityName);
        }

        State = "OFF";
    }

    public override void TurnOnWithAction(string action)
    {
        State = "ON";

        // prepare command
        var command = string.IsNullOrWhiteSpace(Command) ? action : $"{Command} {action}";

        if (RunAsLowIntegrity) CommandLineManager.LaunchAsLowIntegrity(command);
        else
        {
            var executed = !string.IsNullOrWhiteSpace(Command)
                ? CommandLineManager.Execute(Command, action)
                : CommandLineManager.ExecuteHeadless(action);

            if (!executed) Log.Error("[CUSTOMCOMMAND] [{name}] Launching command with action '{action}' failed", EntityName, action);
        }

        State = "OFF";
    }

    public override DiscoveryConfigModel? GetAutoDiscoveryConfig()
    {
        if (Variables.MqttManager == null) return null;

        var deviceConfig = Variables.MqttManager.GetDeviceConfigModel();
        if (deviceConfig == null) return null;

        return new CommandDiscoveryConfigModel
        {
            EntityName = EntityName,
            Name = Name,
            Unique_id = Id,
            Availability_topic = $"{Variables.MqttManager.MqttDiscoveryPrefix()}/sensor/{deviceConfig.Name}/availability",
            Command_topic = $"{Variables.MqttManager.MqttDiscoveryPrefix()}/{Domain}/{deviceConfig.Name}/{ObjectId}/set",
            Action_topic = $"{Variables.MqttManager.MqttDiscoveryPrefix()}/{Domain}/{deviceConfig.Name}/{ObjectId}/action",
            State_topic = $"{Variables.MqttManager.MqttDiscoveryPrefix()}/{Domain}/{deviceConfig.Name}/{ObjectId}/state",
            Device = deviceConfig,
        };
    }

    public override string GetState() => State;

    public override void TurnOff() => Process?.Kill();
}