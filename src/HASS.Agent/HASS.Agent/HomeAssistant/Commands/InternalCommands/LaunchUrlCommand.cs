using HASS.Agent.Functions;
using HASS.Agent.Models.Internal;
using HASS.Agent.Shared.Enums;
using HASS.Agent.Shared.HomeAssistant.Commands;
using Newtonsoft.Json;
using Serilog;

namespace HASS.Agent.HomeAssistant.Commands.InternalCommands;

internal class LaunchUrlCommand : InternalCommand
{
    private const string DefaultName = "launchurl";
    private readonly string _url = string.Empty;
    private readonly bool _incognito;

    internal LaunchUrlCommand(
        string? entityName = DefaultName, 
        string? name = DefaultName, 
        string urlInfo = "", 
        CommandEntityType entityType = CommandEntityType.Switch, 
        string? id = default
    ) : base(entityName ?? DefaultName, name ?? null, urlInfo, entityType, id)
    {
        CommandConfig = urlInfo;
        State = "OFF";

        if (string.IsNullOrEmpty(urlInfo)) return;

        var urlPackage = JsonConvert.DeserializeObject<UrlInfo>(urlInfo);
        if (urlPackage == null) return;

        _url = urlPackage.Url;
        _incognito = urlPackage.Incognito;
    }

    public override void TurnOn() => TurnOnInternal(_url);

    public override void TurnOnWithAction(string action)
    {
        var command = string.IsNullOrWhiteSpace(_url) ? action : $"{_url} {action}";
        TurnOnInternal(command);
    }

    private void TurnOnInternal(string command)
    {
        if (string.IsNullOrWhiteSpace(command))
        {
            LogError("Unable to launch URL, either no URL or empty action is provided.");
            return;
        }

        State = "ON";
        HelperFunctions.LaunchUrl(command, _incognito);
        State = "OFF";
    }

    private void LogError(string message)
    {
        Log.Error("[LAUNCHURL] [{name}] {message}", EntityName, message);
    }
}