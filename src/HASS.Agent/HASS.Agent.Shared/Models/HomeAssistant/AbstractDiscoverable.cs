using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace HASS.Agent.Shared.Models.HomeAssistant;

/// <summary>
/// Abstract discoverable object from which all commands and sensors are derived
/// </summary>
[SuppressMessage("ReSharper", "RedundantDefaultMemberInitializer")]
public abstract partial class AbstractDiscoverable
{
    public string Domain { get; set; } = "switch";
    public string EntityName { get; set; } = string.Empty;
    public string? Name { get; set; } = null;
    public string TopicName { get; set; } = string.Empty;

    private string _objectId = string.Empty;

    public string ObjectId
    {
        get
        {
            if (!string.IsNullOrEmpty(_objectId)) return _objectId;

            _objectId = IsValidAlphaNumericOrSpecial().Replace(EntityName, "_");
            return _objectId;
        }

        set => _objectId = IsValidAlphaNumericOrSpecial().Replace(value, "_");
    }


    public string Id { get; set; } = string.Empty;
    public bool UseAttributes { get; set; } = false;

    public abstract DiscoveryConfigModel? GetAutoDiscoveryConfig();
    public bool IgnoreAvailability { get; set; } = false;
    public abstract void ClearAutoDiscoveryConfig();
    
    [GeneratedRegex("[^a-zA-Z0-9_-]")]
    private static partial Regex IsValidAlphaNumericOrSpecial();
}