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
/// datacenter (Render, Vercel, AWS, etc.) YouTube bloquea el scraping directo
/// con "Sign in to confirm you're not a bot" — confirmado que pasa incluso
/// autenticado con cookies y con un navegador real, para practicamente
/// cualquier video (no es un caso de borde, es la norma en produccion). Por
/// eso el metodo principal es la API de Supadata (maneja el bloqueo del lado
/// de ellos); si no esta configurada la API key, cae a YoutubeExplode directo,
/// que sirve para desarrollo local porque ahi el bloqueo no aplica.
/// </summary>
public class YouTubeTranscriptFetcher : IYouTubeTranscriptFetcher
{
    private static readonly Regex VideoUrlRegex = new(
        @"(?:youtube\.com/(?:watch\?v=|embed/|shorts/)|youtu\.be/)([a-zA-Z0-9_-]{11})",
        RegexOptions.Compiled);

    private const int MaxAttempts = 3;

    private readonly HttpClient _http;
    private readonly YoutubeClient _youtubeClient = new();
    private readonly ILogger<YouTubeTranscriptFetcher> _logger;
    private readonly string? _supadataApiKey;

    public YouTubeTranscriptFetcher(HttpClient http, IConfiguration configuration, ILogger<YouTubeTranscriptFetcher> logger)
    {
        _http = http;
        _logger = logger;
        _supadataApiKey = configuration["Supadata:ApiKey"];
    }

    public bool IsYouTubeUrl(string url) => VideoUrlRegex.IsMatch(url);

    public async Task<string?> GetTitleAsync(string url)
    {
        try
        {
            var response = await _http.GetAsync(
                $"https://www.youtube.com/oembed?url={Uri.EscapeDataString(url)}&format=json");

            if (!response.IsSuccessStatusCode)
                return null;

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);

            return doc.RootElement.TryGetProperty("title", out var title) ? title.GetString() : null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "No se pudo obtener el titulo del video para {Url}", url);
            return null;
        }
    }

    public Task<string> FetchTranscriptAsync(string url)
    {
        return !string.IsNullOrWhiteSpace(_supadataApiKey)
            ? FetchViaSupadataAsync(url)
            : FetchViaYoutubeExplodeAsync(ExtractVideoId(url));
    }

    private async Task<string> FetchViaSupadataAsync(string url)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"https://api.supadata.ai/v1/transcript?url={Uri.EscapeDataString(url)}");
        request.Headers.Add("x-api-key", _supadataApiKey);

        using var response = await _http.SendAsync(request);
        var json = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Supadata devolvio {StatusCode} para {Url}: {Body}", (int)response.StatusCode, url, json);
            throw new ArgumentException("No pudimos obtener la transcripcion de ese video.");
        }

        using var doc = JsonDocument.Parse(json);
        if (!doc.RootElement.TryGetProperty("content", out var contentElement) || contentElement.ValueKind != JsonValueKind.Array)
            throw new ArgumentException("Ese video no tiene subtitulos/transcripcion disponible.");

        var text = string.Join(" ", contentElement.EnumerateArray()
            .Select(segment => segment.TryGetProperty("text", out var t) ? t.GetString() : null)
            .Where(t => !string.IsNullOrWhiteSpace(t)));

        text = Regex.Replace(text, @"\s+", " ").Trim();

        if (string.IsNullOrWhiteSpace(text))
            throw new ArgumentException("No pudimos extraer texto de la transcripcion de ese video.");

        return text;
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
            catch (VideoUnavailableException) when (attempt < MaxAttempts)
            {
                await Task.Delay(TimeSpan.FromSeconds(attempt));
            }
            catch (VideoUnavailableException)
            {
                throw new ArgumentException("No pudimos acceder a ese video en este momento. Puede ser una restriccion temporal — proba de nuevo en unos minutos o con otro video.");
            }
            catch (YoutubeExplodeException)
            {
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
