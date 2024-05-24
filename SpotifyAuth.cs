using SpotifyAPI.Web;
using SpotifyAPI.Web.Auth;

public class Token
{
  public string AccessToken;
  public string RefreshToken;
  public DateTime ExpiresAt;

  public Token(string accessToken, string refreshToken, int expiresIn)
  {
    AccessToken = accessToken;
    RefreshToken = refreshToken;
    ExpiresAt = DateTime.Now.AddSeconds(expiresIn);
  }

  public bool IsAboutToExpire()
  {
    // Margin of 30 seconds
    return DateTime.Now > ExpiresAt.AddSeconds(-30);
  }
}

public class SpotifyAuth
{
  private EmbedIOAuthServer _server;
  private readonly string ClientId = "0d62ea3874874058aabe8761b4908a0e";
  private readonly string ClientSecret;
  private Token? _token = null;

  public SpotifyAuth()
  {
    _server = new EmbedIOAuthServer(new Uri("http://localhost:5543/callback"), 5543);

    // Read client secret from environment variable
    string? rawClientSecret = Environment.GetEnvironmentVariable("SPOTIFY_CLIENT_SECRET");
    if (string.IsNullOrEmpty(rawClientSecret))
    {
      throw new Exception("SPOTIFY_CLIENT_SECRET environment variable is not set");
    }
    ClientSecret = rawClientSecret;
  }

  private async Task StartAuthFlow()
  {
    _server = new EmbedIOAuthServer(new Uri("http://localhost:5543/callback"), 5543);
    await _server.Start();

    _server.AuthorizationCodeReceived += OnAuthorizationCodeReceived;
    _server.ErrorReceived += OnErrorReceived!;

    var request = new LoginRequest(_server.BaseUri, ClientId, LoginRequest.ResponseType.Code)
    {
      Scope = new List<string> {
        // To read player data
        Scopes.UserReadPlaybackState,
        Scopes.UserReadPlaybackPosition,

        Scopes.UserLibraryModify, // To add tracks to liked
        Scopes.UserLibraryRead, // To check if a track is already liked
        }
    };
    BrowserUtil.Open(request.ToUri());
  }

  private async Task OnAuthorizationCodeReceived(object sender, AuthorizationCodeResponse response)
  {
    await _server.Stop();
  }

  private async Task OnErrorReceived(object sender, string error, string state)
  {
    Console.WriteLine($"Aborting authorization, error received: {error}");
    await _server.Stop();
  }

  public async Task<Token> GetToken()
  {
    // If the access token is already available, return it
    if (_token != null)
    {
      return _token;
    }

    // Otherwise, start a new task to:
    // 1. Start the auth flow
    // 2. Wait for the token
    // 3. Return the token
    await StartAuthFlow();

    return await Task.Run(() =>
    {
      var t = new TaskCompletionSource<Token>();

      // This is a type I copied from my IDE, no idea what it means 
      Func<object, AuthorizationCodeResponse, Task> callback = null!;

      callback = async (object sender, AuthorizationCodeResponse response) =>
      {
        // Retrieve the access token
        var config = SpotifyClientConfig.CreateDefault();

        var tokenResponse = await new OAuthClient(config).RequestToken(
          new AuthorizationCodeTokenRequest(
            ClientId, ClientSecret, response.Code, new Uri("http://localhost:5543/callback")
          )
        );

        // Save the refresh token
        _token = new Token(tokenResponse.AccessToken, tokenResponse.RefreshToken, tokenResponse.ExpiresIn);
        t.SetResult(_token);

        // Remove the callback
        _server.AuthorizationCodeReceived -= callback;
      };
      _server.AuthorizationCodeReceived += callback;

      return t.Task;
    });
  }

  public void InvalidateToken()
  {
    _token = null;
  }
}