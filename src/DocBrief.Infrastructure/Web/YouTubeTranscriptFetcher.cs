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
/// Extrae la transcripcion/subtitulos de un video de YouTube. Desde IPs de
/// datacenter (como las de Render) YouTube bloquea las requests con "Sign in to
/// confirm you're not a bot" a nivel de IP — confirmado que pasa incluso con
/// cookies de una cuenta autenticada, asi que no hay forma de esquivarlo desde el
/// propio backend. Si esta configurada la variable Proxy:YouTubeTranscriptUrl,
/// el fetch se delega a una funcion serverless en Vercel (otra red, otra IP);
/// si no, usa YoutubeExplode directamente (sirve para desarrollo local, donde el
/// bloqueo no aplica).
/// </summary>
public class YouTubeTranscriptFetcher : IYouTubeTranscriptFetcher
{
    private static readonly Regex VideoUrlRegex = new(
        @"(?:youtube\.com/(?:watch\?v=|embed/|shorts/)|youtu\.be/)([a-zA-Z0-9_-]{11})",
        RegexOptions.Compiled);

    private const int MaxAttempts = 3;

    private readonly HttpClient _http;
    private readonly YoutubeClient _youtubeClient;
    private readonly ILogger<YouTubeTranscriptFetcher> _logger;
    private readonly string? _proxyUrl;

    public YouTubeTranscriptFetcher(HttpClient http, IConfiguration configuration, ILogger<YouTubeTranscriptFetcher> logger)
    {
        _http = http;
        _logger = logger;
        _proxyUrl = configuration["Proxy:YouTubeTranscriptUrl"];

        var cookiesFile = configuration["YouTube:CookiesFile"];
        _youtubeClient = !string.IsNullOrWhiteSpace(cookiesFile)
            ? new YoutubeClient(NetscapeCookieParser.Parse(cookiesFile))
            : new YoutubeClient();
    }

    public bool IsYouTubeUrl(string url) => VideoUrlRegex.IsMatch(url);

    public Task<string> FetchTranscriptAsync(string url)
    {
        var videoId = ExtractVideoId(url);

        return !string.IsNullOrWhiteSpace(_proxyUrl)
            ? FetchViaProxyAsync(videoId)
            : FetchViaYoutubeExplodeAsync(videoId);
    }

    private async Task<string> FetchViaProxyAsync(string videoId)
    {
        for (var attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            try
            {
                var response = await _http.GetAsync($"{_proxyUrl}?videoId={videoId}");
                var json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);

                if (response.IsSuccessStatusCode && doc.RootElement.TryGetProperty("text", out var textElement))
                {
                    var text = textElement.GetString() ?? "";
                    if (string.IsNullOrWhiteSpace(text))
                        throw new ArgumentException("No pudimos extraer texto de la transcripcion de ese video.");

                    return text;
                }

                var error = doc.RootElement.TryGetProperty("error", out var errorElement)
                    ? errorElement.GetString()
                    : null;

                _logger.LogWarning(
                    "Proxy de transcripcion devolvio {StatusCode} en intento {Attempt}/{MaxAttempts} para {VideoId}: {Error}",
                    (int)response.StatusCode, attempt, MaxAttempts, videoId, error);

                if (attempt == MaxAttempts)
                    throw new ArgumentException(error ?? "No pudimos obtener la transcripcion de ese video.");
            }
            catch (HttpRequestException ex) when (attempt < MaxAttempts)
            {
                _logger.LogWarning(ex, "Error de red contra el proxy de transcripcion en intento {Attempt}/{MaxAttempts} para {VideoId}",
                    attempt, MaxAttempts, videoId);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Error de red contra el proxy de transcripcion para {VideoId}", videoId);
                throw new ArgumentException("No pudimos acceder a ese video en este momento. Proba de nuevo en unos minutos.");
            }

            await Task.Delay(TimeSpan.FromSeconds(attempt));
        }

        throw new ArgumentException("No pudimos acceder a ese video en este momento. Proba de nuevo en unos minutos.");
    }

    private async Task<string> FetchViaYoutubeExplodeAsync(string videoId)
    {
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

    private static string ExtractVideoId(string url)
    {
        var match = VideoUrlRegex.Match(url);
        if (!match.Success)
            throw new ArgumentException("No pudimos reconocer un video de YouTube en esa URL.");

        return match.Groups[1].Value;
    }
}
