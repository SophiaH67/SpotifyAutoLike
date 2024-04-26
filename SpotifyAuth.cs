using SpotifyAPI.Web;
using SpotifyAPI.Web.Auth;

public class SpotifyAuth
{
  private EmbedIOAuthServer _server;
  private readonly string ClientId = "0d62ea3874874058aabe8761b4908a0e";
  private string? _accessToken = null;

  private async Task StartAuthFlow()
  {  ///    // Make sure "http://localhost:5543/callback" is in your spotify application as redirect uri!
    _server = new EmbedIOAuthServer(new Uri("http://localhost:5543/callback"), 5543);
    await _server.Start();

    _server.ImplictGrantReceived += OnImplicitGrantReceived;
    _server.ErrorReceived += OnErrorReceived;

    var request = new LoginRequest(_server.BaseUri, ClientId, LoginRequest.ResponseType.Token)
    {
      Scope = new List<string> { Scopes.UserReadEmail }
    };
    BrowserUtil.Open(request.ToUri());
  }

  private async Task OnImplicitGrantReceived(object sender, ImplictGrantResponse response)
  {
    await _server.Stop();
    _accessToken = response.AccessToken;
  }

  private async Task OnErrorReceived(object sender, string error, string state)
  {
    Console.WriteLine($"Aborting authorization, error received: {error}");
    await _server.Stop();
  }

  public async Task<string> GetAccessToken()
  {
    // If the access token is already available, return it
    if (_accessToken != null)
    {
      return _accessToken;
    }

    // Otherwise, start a new task to:
    // 1. Start the auth flow
    // 2. Wait for the token
    // 3. Return the token
    await StartAuthFlow();

    return await Task.Run(() =>
    {
      var t = new TaskCompletionSource<string>();

      // This is a type I copied from my IDE, no idea what it means 
      Func<object, ImplictGrantResponse, Task> callback = null!;

      callback = (object sender, ImplictGrantResponse response) =>
      {
        t.SetResult(response.AccessToken);
        _server.ImplictGrantReceived -= callback;
        return Task.CompletedTask;
      };
      _server.ImplictGrantReceived += callback;

      return t.Task;
    });
  }

  public void InvalidateAccessToken()
  {
    _accessToken = null;
  }
}