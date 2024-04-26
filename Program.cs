// See https://aka.ms/new-console-template for more information
using SpotifyAPI.Web;

Console.WriteLine("Hello, World!");

SpotifyAuth auth = new();

var token = await auth.GetAccessToken();

Console.WriteLine("Retrieved token " + token);