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

    public YouTubeTranscriptFetcher(IConfiguration configuration, ILogger<YouTubeTranscriptFetcher> logger)
    {
        _logger = logger;

        var cookiesFile = configuration["YouTube:CookiesFile"];
        if (!string.IsNullOrWhiteSpace(cookiesFile))
        {
            var cookies = NetscapeCookieParser.Parse(cookiesFile);
            _youtubeClient = new YoutubeClient(cookies);
            _logger.LogInformation("YouTubeTranscriptFetcher inicializado con {Count} cookies de autenticacion.", cookies.Count);
        }
        else
        {
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
