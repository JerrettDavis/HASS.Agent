﻿using System.Linq;
using HASS.Agent.Shared.Managers;
using HASS.Agent.Shared.Models.HomeAssistant;

namespace HASS.Agent.Shared.HomeAssistant.Sensors.GeneralSensors.SingleValue;

public class LoggedUserSensor(
    int? updateInterval = null,
    string? entityName = LoggedUserSensor.DefaultName,
    string? name = LoggedUserSensor.DefaultName,
    string? id = default,
    string? advancedSettings = default)
    : AbstractSingleValueSensor(entityName ?? DefaultName,
        name ?? null,
        updateInterval ?? 10,
        id,
        advancedSettings: advancedSettings)
{
    private const string DefaultName = "loggeduser";

    public override DiscoveryConfigModel? GetAutoDiscoveryConfig()
    {
        if (Variables.MqttManager == null) return null;

        var deviceConfig = Variables.MqttManager.GetDeviceConfigModel();
        if (deviceConfig == null) return null;

        return AutoDiscoveryConfigModel ?? SetAutoDiscoveryConfigModel(
            new SensorDiscoveryConfigModel
            {
                EntityName = EntityName,
                Name = Name,
                Unique_id = Id,
                Device = deviceConfig,
                State_topic =
                    $"{Variables.MqttManager.MqttDiscoveryPrefix()}/{Domain}/{deviceConfig.Name}/{ObjectId}/state",
                Icon = "mdi:account-group",
                Availability_topic =
                    $"{Variables.MqttManager.MqttDiscoveryPrefix()}/{Domain}/{deviceConfig.Name}/availability"
            });
    }

    public override string GetState()
    {
        // get the active users
        var loggedUsers = SessionsManager.GetLoggedUsers(true);
        var loggedUsersList = loggedUsers as string[] ?? loggedUsers.ToArray();

        // select the first on the list (if any)
        var username = string.Empty;
        if (loggedUsersList.Any()) username = loggedUsersList.First();

        // set empty as none
        if (string.IsNullOrWhiteSpace(username)) username = "None";

        // send the result
        return username;
    }

    public override string GetAttributes() => string.Empty;
}