using System;
using System.Numerics;
using System.Threading.Tasks;
using Dalamud.Interface.Windowing;
using ImGuiNET;

namespace SpotifyCutscener.Windows;

public class ConfigWindow : Window, IDisposable
{
  private readonly Configuration configuration;
  private readonly Plugin plugin;

  public ConfigWindow(Plugin plugin) : base(
    "Spotify Cutscener Config",
    ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoScrollbar |
    ImGuiWindowFlags.NoScrollWithMouse)
  {
    this.Size = new Vector2(280, 170);
    this.SizeCondition = ImGuiCond.Always;

    this.configuration = plugin.Configuration;
    this.plugin = plugin;
  }

  public void Dispose() { }

  public override void Draw()
  {
    ImGui.Text("In-game BGM settings");
    
    var muteWhenSpotifyIsPlaying = this.configuration.MuteWhenSpotifyIsPlaying;
    if (ImGui.Checkbox("Mute in-game BGM while Spotify is playing", ref muteWhenSpotifyIsPlaying))
    {
      this.configuration.MuteWhenSpotifyIsPlaying = muteWhenSpotifyIsPlaying;

      this.configuration.Save();
    }
    
    if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
    {
      ImGui.SetTooltip("Will mute the in-game OST when Spotify is playing and outside of a cutscene.");
    }
    
    ImGui.Spacing();
    ImGui.Spacing();
    
    ImGui.Text("Cutscene settings");
    
    var muteBeforeCutscene = this.configuration.MuteBeforeCutscene;
    if (ImGui.Checkbox("Mute before cutscene", ref muteBeforeCutscene))
    {
      this.configuration.MuteBeforeCutscene = muteBeforeCutscene;

      this.configuration.Save();
    }
    
    if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
    {
      ImGui.SetTooltip("Will mute Spotify when you enter a cutscene.");
    }
    
    ImGui.BeginDisabled(!muteBeforeCutscene);
    
    var unmuteSpotifyAfterCutscene = this.configuration.UnmuteAfterCutscene;
    if (ImGui.Checkbox("Unmute after cutscene", ref unmuteSpotifyAfterCutscene))
    {
      this.configuration.UnmuteAfterCutscene = unmuteSpotifyAfterCutscene;

      this.configuration.Save();
    }

    if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
    {
      ImGui.SetTooltip("Unmutes Spotify after you leave a cutscene and it been previously paused by the plugin.");
    }
    
    ImGui.EndDisabled();
  }
}
