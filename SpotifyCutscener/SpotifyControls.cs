using System;
using System.Text;
using Dalamud.Logging;

namespace SpotifyCutscener;

public class SpotifyControls
{
  public SpotifyStatus SpotifyStatus;
  private const string SpotifyPausedWindowTitle = "Spotify";
  private const string SpotifyPremiumPausedWindowTitle = "Spotify Premium";
  private const string SpotifyWindowClassName = "Chrome_WidgetWin_0"; // Last checked spotify version 1.0.88.353
  private IntPtr spotifyWindowHandle;

  public string? GetSpotifyWindowTitle()
  {
    if (spotifyWindowHandle == IntPtr.Zero)
      return null;

    return !SpotifyStatus.IsSpotifyOpen ? null : GetTitle(spotifyWindowHandle);
  }

  private bool CheckIfSpotifyIsOpen()
  {
    var windowHandle = NativeMethods.FindWindowEx(IntPtr.Zero, IntPtr.Zero, SpotifyWindowClassName, null);

    if (windowHandle == IntPtr.Zero)
    {
      spotifyWindowHandle = IntPtr.Zero;
      return false;
    }

    // We are looking for the topmost window with the classname
    // We keep looking upwards by calling FindWindowEx again and setting the child as the previous value
    // Those other windows will have an empty window title
    var title = GetTitle(windowHandle);
    while (title is { Length: 0 })
    {
      // At the top, this call should return null
      var nextWindowHandle = NativeMethods.FindWindowEx(IntPtr.Zero, windowHandle, SpotifyWindowClassName, null);
      if (nextWindowHandle == IntPtr.Zero)
      {
        break;
      }

      windowHandle = nextWindowHandle;
    }

    spotifyWindowHandle = windowHandle;

    return windowHandle != IntPtr.Zero;
  }

  public bool CheckIfSpotifyIsPlaying()
  {
    var title = GetSpotifyWindowTitle();
    if (title is null)
    {
      return false;
    }

    return title != SpotifyPausedWindowTitle && title != SpotifyPremiumPausedWindowTitle;
  }

  public void UpdateSpotifyStatus()
  {
    SpotifyStatus.IsSpotifyOpen = CheckIfSpotifyIsOpen();
    SpotifyStatus.IsSpotifyPlaying = CheckIfSpotifyIsPlaying();
  }

  public bool TryPlay()
  {
    if (spotifyWindowHandle == IntPtr.Zero)
    {
      return false;
    }

    NativeMethods.SendMessage(spotifyWindowHandle, NativeMethods.WM_APPCOMMAND, 0,
                              new IntPtr((int)NativeMethods.AppCommand.Play));
    return true;
  }

  public bool TryPause()
  {
    if (spotifyWindowHandle == IntPtr.Zero)
    {
      return false;
    }

    NativeMethods.SendMessage(spotifyWindowHandle, NativeMethods.WM_APPCOMMAND, 0,
                              new IntPtr((int)NativeMethods.AppCommand.Pause));
    return true;
  }

  public bool TryPrevious()
  {
    if (spotifyWindowHandle == IntPtr.Zero)
    {
      return false;
    }

    NativeMethods.SendMessage(spotifyWindowHandle, NativeMethods.WM_APPCOMMAND, 0,
                              new IntPtr((int)NativeMethods.AppCommand.Previous));
    return true;
  }

  public bool TryNext()
  {
    if (spotifyWindowHandle == IntPtr.Zero)
    {
      return false;
    }

    NativeMethods.SendMessage(spotifyWindowHandle, NativeMethods.WM_APPCOMMAND, 0,
                              new IntPtr((int)NativeMethods.AppCommand.Next));
    return true;
  }

  public bool IsPaused()
  {
    return !SpotifyStatus.IsSpotifyPlaying;
  }

  private static string? GetTitle(IntPtr windowHandle)
  {
    var titleLength = NativeMethods.GetWindowTextLength(windowHandle);
    var title = new StringBuilder(titleLength + 1);
    _ = NativeMethods.GetWindowText(windowHandle, title, title.Capacity);

    return title.ToString();
  }
}
