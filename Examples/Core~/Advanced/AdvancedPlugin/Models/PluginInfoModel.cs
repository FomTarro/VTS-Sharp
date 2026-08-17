using Microsoft.Extensions.Configuration;

namespace VTS.Core.Examples.Advanced.Models;

public class PluginInfoModel
{
    public const string SectionName = "PluginInfo"; // This is the name of the section in appsettings.json
    
    public string PluginName { get; set; } = string.Empty;
    public string PluginAuthor { get; set; } = string.Empty;
    public string PluginIcon { get; set; } = string.Empty;
    public int UpdateInterval { get; set; } = 100;
    public string PluginVersion { get; set; } = string.Empty;
}
public class PluginInfo(IConfiguration configuration)
{
    private readonly PluginInfoModel _pluginInfoModel = configuration.GetSection(PluginInfoModel.SectionName).Get<PluginInfoModel>();
    public PluginInfoModel Value => _pluginInfoModel;
}