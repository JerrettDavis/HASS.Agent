﻿using System.Runtime.InteropServices;
using HASS.Agent.Shared.Enums;
using HASS.Agent.Shared.Models.HomeAssistant;

namespace HASS.Agent.Shared.HomeAssistant.Sensors.GeneralSensors.SingleValue;

/// <summary>
/// Sensor indicating the current Windows notifications state
/// </summary>
public class UserNotificationStateSensor(
    int? updateInterval = null,
    string? entityName = UserNotificationStateSensor.DefaultName,
    string? name = UserNotificationStateSensor.DefaultName,
    string? id = default,
    string? advancedSettings = default)
    : AbstractSingleValueSensor(entityName ?? DefaultName, name ?? null, updateInterval ?? 10, id,
        advancedSettings: advancedSettings)
{
    private const string DefaultName = "notificationstate";

    public override DiscoveryConfigModel? GetAutoDiscoveryConfig()
    {
        if (Variables.MqttManager == null) return null;

        var deviceConfig = Variables.MqttManager.GetDeviceConfigModel();
        if (deviceConfig == null) return null;

        return AutoDiscoveryConfigModel ?? SetAutoDiscoveryConfigModel(new SensorDiscoveryConfigModel
        {
            EntityName = EntityName,
            Name = Name,
            Unique_id = Id,
            Device = deviceConfig,
            State_topic = $"{Variables.MqttManager.MqttDiscoveryPrefix()}/{Domain}/{deviceConfig.Name}/{EntityName}/state",
            Icon = "mdi:laptop",
            Availability_topic = $"{Variables.MqttManager.MqttDiscoveryPrefix()}/{Domain}/{deviceConfig.Name}/availability"
        });
    }

    public override string GetState() => GetStateEnum().ToString();

    [DllImport("shell32.dll")]
    private static extern int SHQueryUserNotificationState(out UserNotificationState state);

    public UserNotificationState GetStateEnum()
    {
        _ = SHQueryUserNotificationState(out var state);
        return state;
    }

    public override string GetAttributes() => string.Empty;
}