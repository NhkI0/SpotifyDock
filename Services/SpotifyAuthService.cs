using System.IO;
using System.Security.Cryptography;
using System.Text;
using SpotifyAPI.Web;
using SpotifyAPI.Web.Auth;

namespace SpotifyDock.Services;

public class SpotifyAuthService
{
    private const string ClientId = "SPOTIFY_CLIENT_ID_PLACEHOLDER";
    private static readonly Uri RedirectUri = new ("http://127.0.0.1:5543/callback");

    private static readonly string TokenPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "SpotifyDock", "token.dat");

    private readonly string[] _scopes =
    [
        Scopes.UserReadPlaybackState,
        Scopes.UserReadCurrentlyPlaying,
        Scopes.UserModifyPlaybackState,
    ];
    
    private TaskCompletionSource<string>? _authTcs;
    private EmbedIOAuthServer? _server;

    public async Task<SpotifyClient?> AuthenticateAsync()
    {
        var refreshToken = LoadRefreshToken();
        if (refreshToken != null)
        {
            try
            {
                var response = await new OAuthClient().RequestToken(
                    new PKCETokenRefreshRequest(ClientId, refreshToken));
                SaveRefreshToken(response.RefreshToken);
                return BuildClient(response);
            }
            catch (APIException) // If token incorrect
            {
                if (File.Exists(TokenPath)) File.Delete(TokenPath);
            }
        }

        return await FreshAuthAsync();
    }

    private async Task<SpotifyClient?> FreshAuthAsync()
    {
        var (verifier, challenge) = PKCEUtil.GenerateCodes();

        _server = new EmbedIOAuthServer(RedirectUri, 5543);
        await _server.Start();

        _authTcs = new TaskCompletionSource<string>();
        _server.AuthorizationCodeReceived += (_, response) =>
        {
            _authTcs.TrySetResult(response.Code);
            return Task.CompletedTask;
        };

        var loginRequest = new LoginRequest(
            _server.BaseUri, ClientId, LoginRequest.ResponseType.Code)
        {
            CodeChallengeMethod = "S256",
            CodeChallenge = challenge,
            Scope = _scopes,
        };

        var uri = loginRequest.ToUri();

        try
        {
            var psi = new System.Diagnostics.ProcessStartInfo()
            {
                FileName = uri.ToString(),
                UseShellExecute = true,
            };
            System.Diagnostics.Process.Start(psi);
        }
        catch
        {
            return null;
        }
        
        // Wait for the callback (timeout after 2 minutes)
        using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(2));
        cts.Token.Register (() => _authTcs.TrySetCanceled());

        string code;
        try
        {
            code = await _authTcs.Task;
        }
        catch (TaskCanceledException)
        {
            await _server.Stop();
            return null;
        }

        await _server.Stop();

        var tokenResponse = await new OAuthClient().RequestToken(
            new PKCETokenRequest(ClientId, code, _server.BaseUri, verifier));

        SaveRefreshToken(tokenResponse.RefreshToken);
        return BuildClient(tokenResponse);
    }

    private static SpotifyClient BuildClient(PKCETokenResponse token)
    {
        var authenticator = new PKCEAuthenticator(ClientId, token);
        var config = SpotifyClientConfig.CreateDefault().WithAuthenticator(authenticator);
        return new SpotifyClient(config);
    }

    private void SaveRefreshToken(string token)
    {
        try
        {
            var dir = Path.GetDirectoryName(TokenPath)!;
            Directory.CreateDirectory(dir);
            var encrypted = ProtectedData.Protect(
                Encoding.UTF8.GetBytes(token), null,
                DataProtectionScope.CurrentUser);
            File.WriteAllBytes(TokenPath, encrypted);
        }
        catch
        {
            // Token won't be saved
        }
    }

    private string? LoadRefreshToken()
    {
        try
        {
            if (!File.Exists(TokenPath)) return null;
            var encrypted = File.ReadAllBytes(TokenPath);
            var decrypted = ProtectedData.Unprotect(
                encrypted, null, DataProtectionScope.CurrentUser);
            return Encoding.UTF8.GetString(decrypted);
        }
        catch
        {
            return null;
        }
    }
}