using System;
using HASS.Agent.Shared.Models.HomeAssistant;

namespace HASS.Agent.Shared.HomeAssistant.Sensors.GeneralSensors.MultiValue.DataTypes;

/// <summary>
/// Generic int sensor
/// </summary>
public class DataTypeIntSensor : AbstractSingleValueSensor
{
    private readonly string _deviceClass;
    private readonly string _stateClass;
    private readonly string _unitOfMeasurement;
    private readonly string _icon;

    private int _value;
    private string _attributes = string.Empty;

    public DataTypeIntSensor(
        int? updateInterval,
        string entityName,
        string name,
        string id,
        string deviceClass,
        string stateClass,
        string icon,
        string unitOfMeasurement,
        string multiValueSensorName,
        bool useAttributes = false
    ) : base(
        entityName,
        name,
        updateInterval ?? 30,
        id,
        useAttributes
    )
    {
        TopicName = multiValueSensorName;
        _deviceClass = deviceClass;
        _stateClass = stateClass;
        _unitOfMeasurement = unitOfMeasurement;
        _icon = icon;
        ObjectId = id;
    }

    [Obsolete("Deprecated due to HA 2023.8 MQTT changes in favor of method specifying entityName")]
    public DataTypeIntSensor(
        int? updateInterval,
        string name,
        string id,
        string deviceClass,
        string icon,
        string unitOfMeasurement,
        string multiValueSensorName,
        bool useAttributes = false
    ) : base(
        name,
        name,
        updateInterval ?? 30,
        id,
        useAttributes
    )
    {
        TopicName = multiValueSensorName;
        _deviceClass = deviceClass;
        _unitOfMeasurement = unitOfMeasurement;
        _icon = icon;
        _stateClass = string.Empty;
        ObjectId = id;
    }

    public override DiscoveryConfigModel? GetAutoDiscoveryConfig()
    {
        if (AutoDiscoveryConfigModel != null)
            return AutoDiscoveryConfigModel;

        var mqttManager = Variables.MqttManager;
        var deviceConfig = mqttManager?.GetDeviceConfigModel();

        if (deviceConfig == null)
            return null;

        return SetAutoDiscoveryConfigModel(
            new SensorDiscoveryConfigModel
            {
                EntityName = EntityName,
                Name = Name,
                Unique_id = Id,
                Device = deviceConfig,
                State_topic = GenerateTopic("state"),
                Availability_topic = GenerateTopic("availability", isAvailability: true),
                Json_attributes_topic = UseAttributes
                    ? GenerateTopic("attributes")
                    : null,
                Device_class = GetValueIfNotEmpty(_deviceClass),
                State_class = GetValueIfNotEmpty(_stateClass),
                Unit_of_measurement = GetValueIfNotEmpty(_unitOfMeasurement),
                Icon = GetValueIfNotEmpty(_icon)
            }
        );

        // Local function to generate the topic string
        string GenerateTopic(string topicType, bool isAvailability = false) =>
            isAvailability
                ? $"{mqttManager?.MqttDiscoveryPrefix()}/{Domain}/{deviceConfig.Name}/availability"
                : $"{mqttManager?.MqttDiscoveryPrefix()}/{Domain}/{deviceConfig.Name}/" +
                  $"{TopicName}/{ObjectId}/{topicType}";

        // Local function to check and return values or null
        string? GetValueIfNotEmpty(string value) =>
            !string.IsNullOrWhiteSpace(value) ? value : null;
    }

    public void SetState(int value) => _value = value;

    public void SetAttributes(string value) =>
        _attributes = string.IsNullOrWhiteSpace(value) ? "{}" : value;

    public override string GetState() => _value.ToString();
    public override string GetAttributes() => _attributes;
}