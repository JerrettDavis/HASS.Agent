using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using HASS.Agent.Shared.Enums;
using HASS.Agent.Shared.Models.HomeAssistant;
using Serilog;

namespace HASS.Agent.Shared.HomeAssistant.Commands;

[SuppressMessage("ReSharper", "InconsistentNaming")]
public class MultipleKeysCommand(
    List<string> keys,
    string? entityName = MultipleKeysCommand.DefaultName,
    string? name = MultipleKeysCommand.DefaultName,
    CommandEntityType entityType = CommandEntityType.Switch,
    string? id = default)
    : AbstractCommand(entityName ?? DefaultName, name ?? null, entityType, id)
{
    private const string DefaultName = "multiplekeys";

    public string State { get; protected set; } = "OFF";
    public List<string> Keys { get; set; } = keys;

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
            Availability_topic =
                $"{Variables.MqttManager.MqttDiscoveryPrefix()}/sensor/{deviceConfig.Name}/availability",
            Command_topic =
                $"{Variables.MqttManager.MqttDiscoveryPrefix()}/{Domain}/{deviceConfig.Name}/{ObjectId}/set",
            Action_topic =
                $"{Variables.MqttManager.MqttDiscoveryPrefix()}/{Domain}/{deviceConfig.Name}/{ObjectId}/action",
            State_topic =
                $"{Variables.MqttManager.MqttDiscoveryPrefix()}/{Domain}/{deviceConfig.Name}/{ObjectId}/state",
            Device = deviceConfig
        };
    }

    public override string GetState() => State;

    public override void TurnOff()
    {
        //
    }

    public override async void TurnOn()
    {
        try
        {
            State = "ON";

            foreach (var key in Keys)
            {
                SendKeys.SendWait(key);
                SendKeys.Flush();
                await Task.Delay(50);
            }
        }
        catch (Exception ex)
        {
            Log.Error("[MULTIPLEKEYS] [{name}] Executing command failed: {ex}", EntityName, ex.Message);
        }
        finally
        {
            State = "OFF";
        }
    }

    public override async void TurnOnWithAction(string action)
    {
        var keys = ParseMultipleKeys(action);
        if (keys.Count == 0)
            return;

        foreach (var key in keys)
        {
            SendKeys.SendWait(key);
            SendKeys.Flush();
            await Task.Delay(50);
        }
    }

    private List<string> ParseMultipleKeys(string keyString)
    {
        var keys = new List<string>();

        try
        {
            if (string.IsNullOrWhiteSpace(keyString))
                return keys;

            // Match balanced brackets allowing for escaped brackets within
            const string pattern = @"(?<!\\)\[(.*?)(?<!\\)\]";
            var matches = Regex.Matches(keyString, pattern);

            foreach (Match match in matches)
            {
                var key = match.Groups[1].Value;
                key = key.Replace("left_bracket", "[").Replace("right_bracket", "]");
                keys.Add(key);
            }
        }
        catch (Exception ex)
        {
            Log.Error("[MULTIPLEKEYS] [{name}] Error parsing multiple keys: {msg}",
                EntityName, ex.Message);
        }

        return keys;
    }
}