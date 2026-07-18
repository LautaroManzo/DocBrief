using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using DocBrief.Application.Interfaces;
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
/// </summary>
public class YouTubeTranscriptFetcher : IYouTubeTranscriptFetcher
{
    private static readonly Regex VideoUrlRegex = new(
        @"(?:youtube\.com/(?:watch\?v=|embed/|shorts/)|youtu\.be/)([a-zA-Z0-9_-]{11})",
        RegexOptions.Compiled);

    private readonly YoutubeClient _youtubeClient = new();
    private readonly ILogger<YouTubeTranscriptFetcher> _logger;

    public YouTubeTranscriptFetcher(ILogger<YouTubeTranscriptFetcher> logger)
    {
        _logger = logger;
    }

    public bool IsYouTubeUrl(string url) => VideoUrlRegex.IsMatch(url);

    private const int MaxAttempts = 3;

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
                await LogPlayabilityReasonAsync(videoId);
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
    /// Diagnostico: replica las llamadas internas que hace YoutubeExplode al endpoint
    /// de player de YouTube para leer el motivo real de "playabilityStatus.reason"
    /// (ej. "Sign in to confirm you're not a bot"), que la libreria descarta antes de
    /// tirar VideoUnavailableException con un mensaje generico. Prueba tambien el
    /// cliente TVHTML5_SIMPLY_EMBEDDED_PLAYER (pensado para reproducir videos
    /// embebidos en sitios de terceros sin login) para ver si esquiva el bloqueo
    /// que si afecta al cliente ANDROID_VR que usa YoutubeExplode para subtitulos.
    /// </summary>
    private async Task LogPlayabilityReasonAsync(string videoId)
    {
        await LogPlayabilityForClientAsync(
            videoId,
            "ANDROID_VR",
            $$"""
            {
              "videoId": "{{videoId}}",
              "contentCheckOk": true,
              "context": {
                "client": {
                  "clientName": "ANDROID_VR",
                  "clientVersion": "1.60.19",
                  "deviceMake": "Oculus",
                  "deviceModel": "Quest 3",
                  "osName": "Android",
                  "osVersion": "12L",
                  "platform": "MOBILE",
                  "hl": "en",
                  "gl": "US",
                  "utcOffsetMinutes": 0
                }
              }
            }
            """,
            "com.google.android.apps.youtube.vr.oculus/1.60.19 (Linux; U; Android 12L; Quest 3 Build/SQ3A.220605.009.A1) gzip");

        await LogPlayabilityForClientAsync(
            videoId,
            "TVHTML5_SIMPLY_EMBEDDED_PLAYER",
            $$"""
            {
              "videoId": "{{videoId}}",
              "context": {
                "client": {
                  "clientName": "TVHTML5_SIMPLY_EMBEDDED_PLAYER",
                  "clientVersion": "2.0",
                  "hl": "en",
                  "gl": "US",
                  "utcOffsetMinutes": 0
                },
                "thirdParty": {
                  "embedUrl": "https://www.youtube.com"
                }
              }
            }
            """,
            null);
    }

    private async Task LogPlayabilityForClientAsync(string videoId, string clientName, string body, string? userAgent)
    {
        try
        {
            using var http = new HttpClient();

            using var request = new HttpRequestMessage(HttpMethod.Post, "https://www.youtube.com/youtubei/v1/player")
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json")
            };
            if (userAgent is not null)
                request.Headers.Add("User-Agent", userAgent);

            using var response = await http.SendAsync(request);
            var json = await response.Content.ReadAsStringAsync();

            using var doc = JsonDocument.Parse(json);
            var playability = doc.RootElement.GetProperty("playabilityStatus");
            var status = playability.TryGetProperty("status", out var s) ? s.GetString() : null;
            var reason = playability.TryGetProperty("reason", out var r) ? r.GetString() : null;
            var hasCaptions = doc.RootElement.TryGetProperty("captions", out _);

            _logger.LogWarning(
                "Diagnostico playability YouTube {VideoId} [{ClientName}]: status={Status} reason={Reason} hasCaptions={HasCaptions}",
                videoId, clientName, status, reason, hasCaptions);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "No se pudo obtener el diagnostico de playability [{ClientName}] para {VideoId}", clientName, videoId);
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
