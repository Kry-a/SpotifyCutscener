using System;
using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Plugin;
using System.IO;
using System.Threading;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.Config;
using Dalamud.Interface.Windowing;
using Dalamud.Logging;
using Dalamud.Memory;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.System.Framework;
using FFXIVClientStructs.FFXIV.Common.Configuration;
using SpotifyCutscener.Windows;
using Task = System.Threading.Tasks.Task;

namespace SpotifyCutscener
{
  public sealed class Plugin : IDalamudPlugin
  {
    public string Name => "Sample Plugin";
    private const string ConfigCommandName = "/spcut";

    private DalamudPluginInterface PluginInterface { get; init; }
    private ICommandManager CommandManager { get; init; }
    public Configuration Configuration { get; init; }
    public WindowSystem WindowSystem = new("SpotifyCutscener");
    public readonly SpotifyControls SpotifyControls = new();
    private PeriodicTimer spotifyTimer = new(TimeSpan.FromSeconds(5));
    private CancellationTokenSource spotifyTimerCancel = new();
    private readonly ICondition condition;
    private IGameConfig gameConfig;
    public bool IsInCutscene { get; private set; }
    private bool pausedByGame = false;

    private ConfigWindow ConfigWindow { get; init; }

    public Plugin(
      [RequiredVersion("1.0")] DalamudPluginInterface pluginInterface,
      [RequiredVersion("1.0")] ICommandManager commandManager,
      IFramework framework,
      ICondition condition,
      IGameConfig gameConfig)
    {
      this.PluginInterface = pluginInterface;
      this.CommandManager = commandManager;
      this.condition = condition;
      this.gameConfig = gameConfig;

      this.Configuration = this.PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
      this.Configuration.Initialize(this.PluginInterface);

      // you might normally want to embed resources and load them from the manifest stream
      // var imagePath = Path.Combine(PluginInterface.AssemblyLocation.Directory?.FullName!, "goat.png");
      // var goatImage = this.PluginInterface.UiBuilder.LoadImage(imagePath);

      ConfigWindow = new ConfigWindow(this);

      WindowSystem.AddWindow(ConfigWindow);

      this.CommandManager.AddHandler(ConfigCommandName, new CommandInfo(OnSettingsCommand)
      {
        HelpMessage = "Opens the SpotifyCutscener config window."
      });

      this.PluginInterface.UiBuilder.Draw += DrawUi;
      this.PluginInterface.UiBuilder.OpenConfigUi += DrawConfigUi;
      framework.Update += OnFrameworkUpdate;

      SpotifyControls.UpdateSpotifyStatus();

      if (SpotifyControls.SpotifyStatus.IsSpotifyOpen)
      {
        SetSpotifyTimer(TimeSpan.FromSeconds(1));
      }

      _ = HandleSpotifyTimer(spotifyTimerCancel.Token);
    }

    private void SetSpotifyTimer(TimeSpan timeSpan)
    {
      spotifyTimerCancel.Cancel();
      spotifyTimerCancel = new CancellationTokenSource();
      spotifyTimer = new PeriodicTimer(timeSpan);
      _ = HandleSpotifyTimer(spotifyTimerCancel.Token);
    }
    
    private void MuteBgm(bool muted)
    {
      gameConfig.Set(SystemConfigOption.IsSndBgm, muted);
    }

    private async Task HandleSpotifyTimer(CancellationToken cancel = default)
    {
      try
      {
        while (await spotifyTimer.WaitForNextTickAsync(cancel))
        {
          var wasSpotifyOpenBefore = SpotifyControls.SpotifyStatus.IsSpotifyOpen;

          SpotifyControls.UpdateSpotifyStatus();
          
          if (Configuration.MuteBeforeCutscene && IsInCutscene && SpotifyControls.SpotifyStatus is { IsSpotifyOpen: true, IsSpotifyPlaying: true })
          {
            SpotifyControls.TryPause();
            MuteBgm(false);
            pausedByGame = true;
            continue;
          }
          
          if (Configuration.UnmuteAfterCutscene && !IsInCutscene && pausedByGame)
          {
            SpotifyControls.TryPlay();
            pausedByGame = false;
            continue;
          }

          if (!IsInCutscene && SpotifyControls.SpotifyStatus.IsSpotifyPlaying)
          {
            if (Configuration.MuteWhenSpotifyIsPlaying)
              MuteBgm(true);
          }
          else if (!SpotifyControls.SpotifyStatus.IsSpotifyPlaying)
          {
            MuteBgm(false);
          }

          if (wasSpotifyOpenBefore && !SpotifyControls.SpotifyStatus.IsSpotifyOpen)
          {
            pausedByGame = false;

            SetSpotifyTimer(TimeSpan.FromSeconds(5));
          }
          else if (!wasSpotifyOpenBefore && SpotifyControls.SpotifyStatus.IsSpotifyOpen)
          {
            SetSpotifyTimer(TimeSpan.FromSeconds(1));
          }
        }
      }
      catch (OperationCanceledException)
      {
        // ignore
      }
      catch (Exception e)
      {
        PluginLog.LogError(e, "Error in spotify timer");
      }
    }

    private void OnFrameworkUpdate(IFramework framework)
    {
      IsInCutscene = condition[ConditionFlag.OccupiedInCutSceneEvent];
    }

    public void Dispose()
    {
      spotifyTimerCancel.Cancel();
      spotifyTimer.Dispose();

      this.WindowSystem.RemoveAllWindows();

      ConfigWindow.Dispose();

      this.CommandManager.RemoveHandler(ConfigCommandName);
    }

    private void OnSettingsCommand(string command, string args)
    {
      ConfigWindow.IsOpen = true;
    }

    private void DrawUi()
    {
      this.WindowSystem.Draw();
    }

    public void DrawConfigUi()
    {
      ConfigWindow.IsOpen = true;
    }
  }
}
