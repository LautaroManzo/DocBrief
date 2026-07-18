using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using DocBrief.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using YoutubeExplode;
using YoutubeExplode.Common;
using YoutubeExplode.Exceptions;

namespace DocBrief.Infrastructure.Web;

/// <summary>
/// Extrae la transcripcion/subtitulos de un video de YouTube usando YoutubeExplode,
/// una libreria mantenida activamente que se encarga de la complejidad y los cambios
/// frecuentes de YouTube (no hay API publica oficial para bajar subtitulos de videos
/// ajenos, asi que replicar esto a mano es fragil y se rompe seguido).
/// Desde IPs de datacenter (como las de Render) YouTube bloquea las requests
/// anonimas con "Sign in to confirm you're not a bot" — por eso, si esta
/// configurada la variable YouTube:CookiesFile (cookies.txt de una cuenta
/// autenticada, formato Netscape), se usan para autenticar las requests.
/// </summary>
public class YouTubeTranscriptFetcher : IYouTubeTranscriptFetcher
{
    private static readonly Regex VideoUrlRegex = new(
        @"(?:youtube\.com/(?:watch\?v=|embed/|shorts/)|youtu\.be/)([a-zA-Z0-9_-]{11})",
        RegexOptions.Compiled);

    private const int MaxAttempts = 3;

    private readonly YoutubeClient _youtubeClient;
    private readonly ILogger<YouTubeTranscriptFetcher> _logger;
    private readonly List<Cookie> _cookies;

    public YouTubeTranscriptFetcher(IConfiguration configuration, ILogger<YouTubeTranscriptFetcher> logger)
    {
        _logger = logger;

        var cookiesFile = configuration["YouTube:CookiesFile"];
        if (!string.IsNullOrWhiteSpace(cookiesFile))
        {
            _cookies = NetscapeCookieParser.Parse(cookiesFile);
            _youtubeClient = new YoutubeClient(_cookies);
            _logger.LogInformation("YouTubeTranscriptFetcher inicializado con {Count} cookies de autenticacion.", _cookies.Count);
        }
        else
        {
            _cookies = [];
            _youtubeClient = new YoutubeClient();
        }
    }

    public bool IsYouTubeUrl(string url) => VideoUrlRegex.IsMatch(url);

    public async Task<string> FetchTranscriptAsync(string url)
    {
        var videoId = ExtractVideoId(url);

        for (var attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            try
            {
                var trackManifest = await _youtubeClient.Videos.ClosedCaptions.GetManifestAsync(videoId);

                var trackInfo = trackManifest.TryGetByLanguage("en")
                    ?? trackManifest.Tracks.FirstOrDefault()
                    ?? throw new ArgumentException("Ese video no tiene subtitulos/transcripcion disponible.");

                var track = await _youtubeClient.Videos.ClosedCaptions.GetAsync(trackInfo);

                var text = string.Join(" ", track.Captions.Select(c => c.Text));
                text = Regex.Replace(text, @"\s+", " ").Trim();

                if (string.IsNullOrWhiteSpace(text))
                    throw new ArgumentException("No pudimos extraer texto de la transcripcion de ese video.");

                return text;
            }
            catch (VideoUnavailableException ex) when (attempt < MaxAttempts)
            {
                _logger.LogWarning(ex, "YouTube VideoUnavailableException en intento {Attempt}/{MaxAttempts} para {VideoId}: {Message}",
                    attempt, MaxAttempts, videoId, ex.Message);
                await Task.Delay(TimeSpan.FromSeconds(attempt));
            }
            catch (VideoUnavailableException ex)
            {
                _logger.LogError(ex, "YouTube VideoUnavailableException final para {VideoId}: {Message}", videoId, ex.Message);
                await LogWebClientPlayabilityAsync(videoId);
                throw new ArgumentException("No pudimos acceder a ese video en este momento. Puede ser una restriccion temporal — proba de nuevo en unos minutos o con otro video.");
            }
            catch (YoutubeExplodeException ex)
            {
                _logger.LogError(ex, "YoutubeExplodeException para {VideoId}: {Message}", videoId, ex.Message);
                throw new ArgumentException("No pudimos obtener la transcripcion de ese video.");
            }
        }

        throw new ArgumentException("No pudimos acceder a ese video en este momento. Puede ser una restriccion temporal — proba de nuevo en unos minutos o con otro video.");
    }

    /// <summary>
    /// Diagnostico: YoutubeExplode usa el cliente ANDROID_VR para subtitulos, que no
    /// usa cookies de sesion de navegador (las apps moviles no se autentican asi).
    /// Esto prueba el cliente WEB, que si usa cookies de forma nativa, para ver si
    /// con la sesion autenticada esquiva el "Sign in to confirm you're not a bot".
    /// </summary>
    private async Task LogWebClientPlayabilityAsync(string videoId)
    {
        try
        {
            var cookieContainer = new CookieContainer();
            foreach (var cookie in _cookies)
                cookieContainer.Add(cookie);

            using var handler = new HttpClientHandler { CookieContainer = cookieContainer, UseCookies = true };
            using var http = new HttpClient(handler);
            http.DefaultRequestHeaders.Add(
                "User-Agent",
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");

            var body = $$"""
            {
              "videoId": "{{videoId}}",
              "context": {
                "client": {
                  "clientName": "WEB",
                  "clientVersion": "2.20240101.00.00",
                  "hl": "en",
                  "gl": "US"
                }
              }
            }
            """;

            using var request = new HttpRequestMessage(
                HttpMethod.Post,
                "https://www.youtube.com/youtubei/v1/player?key=AIzaSyAO_FJ2SlqU8Q4STEHLGCilw_Y9_11qcW8")
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json")
            };

            using var response = await http.SendAsync(request);
            var json = await response.Content.ReadAsStringAsync();

            using var doc = JsonDocument.Parse(json);
            var playability = doc.RootElement.GetProperty("playabilityStatus");
            var status = playability.TryGetProperty("status", out var s) ? s.GetString() : null;
            var reason = playability.TryGetProperty("reason", out var r) ? r.GetString() : null;
            var hasCaptions = doc.RootElement.TryGetProperty("captions", out _);

            _logger.LogWarning(
                "Diagnostico playability YouTube {VideoId} [WEB+cookies]: status={Status} reason={Reason} hasCaptions={HasCaptions}",
                videoId, status, reason, hasCaptions);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "No se pudo obtener el diagnostico [WEB+cookies] para {VideoId}", videoId);
        }
    }

    private static string ExtractVideoId(string url)
    {
        var match = VideoUrlRegex.Match(url);
        if (!match.Success)
            throw new ArgumentException("No pudimos reconocer un video de YouTube en esa URL.");

        return match.Groups[1].Value;
    }
}
