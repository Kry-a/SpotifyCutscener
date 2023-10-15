using Dalamud.Configuration;
using Dalamud.Plugin;
using System;

namespace SpotifyCutscener
{
  [Serializable]
  public class Configuration : IPluginConfiguration
  {
    public int Version { get; set; } = 0;

    public bool MuteWhenSpotifyIsPlaying { get; set; } = true;
    
    public bool MuteBeforeCutscene { get; set; } = true;
    public bool UnmuteAfterCutscene { get; set; } = true;
    
    [NonSerialized]
    private DalamudPluginInterface? pluginInterface;

    public void Initialize(DalamudPluginInterface pluginInterfaceParam)
    {
      this.pluginInterface = pluginInterfaceParam;
    }

    public void Save()
    {
      this.pluginInterface!.SavePluginConfig(this);
    }
  }
}
