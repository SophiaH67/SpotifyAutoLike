﻿// See https://aka.ms/new-console-template for more information
using SpotifyAPI.Web;
using Microsoft.Extensions.Logging;

using ILoggerFactory logging = LoggerFactory.Create(builder =>
  builder.AddConsole()
  // Show all logs
  .AddFilter(level => true)
);

SpotifyAuth auth = new();
await auth.Load();
var token = await auth.GetAccessToken();

ILogger Logger = logging.CreateLogger("SpotifyAutoLike");

Console.WriteLine("Retrieved token " + token);

var spotify = new SpotifyClient(token);


FullTrack? previousTrack = null;
double previousPercentage = 0;

while (true)
{
  await Task.Delay(1000);
  // Get the current playback
  var playback = await spotify.Player.GetCurrentPlayback();
  if (playback == null)
  {
    Logger.LogDebug("No current playback");
    continue;
  }

  // Check if it's a song
  if (playback.Item is not FullTrack)
  {
    Logger.LogDebug(playback.Item.ToString() + " is not a track");
    continue;
  }
  FullTrack? track = playback.Item as FullTrack;

  // Make sure we're actually playing something
  if (!playback.IsPlaying)
  {
    Logger.LogDebug("Track is not playing");
    continue;
  }

  // Calculate the percentage of the track that has been played
  int position = playback.ProgressMs;
  int duration = track!.DurationMs;
  double percentage = (double)position / duration * 100;
  Logger.LogDebug("Currently playing: " + track.Name + " - " + position + "ms / " + duration + "ms (" + percentage + "%)");

  // Most important of all, the liking
  if (previousTrack != null && previousTrack.Id != track.Id)
  {
    if (previousPercentage > 95)
    {
      // Check if the track has been liked
      var liked = await spotify.Library.CheckTracks(new([previousTrack.Id]));
      var isLiked = liked[0];
      if (isLiked)
      {
        Logger.LogDebug("Not liking " + previousTrack.Name + " (Already liked)");

      }
      else
      {
        Logger.LogInformation("Liking " + previousTrack.Name + " (" + previousPercentage + "% played)");
        await spotify.Library.SaveTracks(new([previousTrack.Id]));
      }
    }
    else
    {
      Logger.LogDebug("Not liking " + previousTrack.Name + " (" + previousPercentage + "% played)");
    }
  }

  // Finally, record the current track and percentage
  previousPercentage = percentage;
  previousTrack = track;
}
