# SpotifyAutoLike

This is a simple program that automatically likes fully played songs on Spotify.

I made this primarily for cycling, as I skip songs I don't like, and if I fully play a song, it means I like it.

## How to use

1. Download the latest exe from the [releases page](https://github.com/SophiaH67/SpotifyAutoLike/releases)
2. Run the exe
3. Complete the authorization flow

## How it works

The program checks every second for current track progress. When the track switches, it checks if the previous track was fully played (> 95% progress). If it was, it likes the track.

## How to build

```
dotnet publish -c Release -r win-x64
```
