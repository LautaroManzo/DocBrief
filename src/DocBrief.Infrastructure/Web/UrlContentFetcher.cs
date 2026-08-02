using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using DocBrief.Application.Interfaces;
using HtmlAgilityPack;

namespace DocBrief.Infrastructure.Web;

public class UrlContentFetcher : IUrlContentFetcher
{
    private const long MaxContentLengthBytes = 5 * 1024 * 1024;

    private static readonly string[] RemovableTags = { "script", "style", "nav", "header", "footer", "noscript", "svg" };

    private static int _encodingProviderRegistered;

    private readonly HttpClient _httpClient;

    public UrlContentFetcher(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<UrlContent> FetchAsync(string url)
    {
        var uri = ValidateUrl(url);

        using var response = await _httpClient.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead);
        response.EnsureSuccessStatusCode();

        if (response.Content.Headers.ContentLength is > MaxContentLengthBytes)
            throw new InvalidOperationException("La pagina supera el limite de tamano permitido.");

        var bytes = await response.Content.ReadAsByteArrayAsync();
        var html = DecodeHtml(bytes, response.Content.Headers.ContentType?.CharSet);

        var document = new HtmlDocument();
        document.LoadHtml(html);

        var title = document.DocumentNode.SelectSingleNode("//title")?.InnerText;
        title = string.IsNullOrWhiteSpace(title) ? null : CleanTitle(WebUtility.HtmlDecode(title).Trim());

        var removable = document.DocumentNode.SelectNodes(string.Join("|", RemovableTags.Select(t => $"//{t}")));
        if (removable is not null)
        {
            foreach (var node in removable)
            {
                node.Remove();
            }
        }

        var text = WebUtility.HtmlDecode(document.DocumentNode.InnerText);
        text = Regex.Replace(text, @"\s+", " ").Trim();

        return new UrlContent(text, title);
    }

    /// <summary>
    /// HttpClient solo mira el charset del header Content-Type, pero muchas paginas lo
    /// declaran unicamente en un &lt;meta charset&gt; dentro del HTML (o lo declaran mal
    /// en el header). Sin esto, paginas en Windows-1252/ISO-8859-1 salen con tildes
    /// corruptas. Prioridad: header -> meta tag -> UTF-8 por defecto.
    /// </summary>
    private static string DecodeHtml(byte[] bytes, string? headerCharset)
    {
        var encoding = ResolveEncoding(headerCharset) ?? ResolveEncoding(SniffMetaCharset(bytes)) ?? Encoding.UTF8;
        return encoding.GetString(bytes);
    }

    private static Encoding? ResolveEncoding(string? charset)
    {
        if (string.IsNullOrWhiteSpace(charset)) return null;

        try
        {
            if (Interlocked.Exchange(ref _encodingProviderRegistered, 1) == 0)
            {
                Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            }

            return Encoding.GetEncoding(charset.Trim().Trim('"', '\''));
        }
        catch (ArgumentException)
        {
            return null;
        }
    }

    private static string? SniffMetaCharset(byte[] bytes)
    {
        // El charset se declara siempre en ASCII, asi que decodificar los primeros bytes
        // como Latin1 (mapeo 1 a 1 byte->char) es seguro para buscar la declaracion,
        // sea cual sea la codificacion real del resto del documento.
        var head = Encoding.Latin1.GetString(bytes, 0, Math.Min(bytes.Length, 2048));
        var match = Regex.Match(head, @"charset\s*=\s*[""']?([a-zA-Z0-9_\-]+)", RegexOptions.IgnoreCase);

        return match.Success ? match.Groups[1].Value : null;
    }

    /// <summary>
    /// Los &lt;title&gt; suelen venir como "Articulo | NombreDelSitio" o similar.
    /// Se queda con el segmento mas largo (el titulo real) y descarta el nombre del sitio.
    /// </summary>
    private static string CleanTitle(string title)
    {
        var parts = title.Split(new[] { " | ", " – ", " — ", " - ", " · ", " » " }, StringSplitOptions.RemoveEmptyEntries);

        return parts.Length > 1
            ? parts.OrderByDescending(p => p.Length).First().Trim()
            : title;
    }

    private static Uri ValidateUrl(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            throw new ArgumentException("La URL no es valida. Debe empezar con http:// o https://");
        }

        return uri;
    }

    /// <summary>
    /// Valida que una IP resuelta no apunte a una red privada, loopback o link-local.
    /// Se usa en el momento exacto de conectar (ver <see cref="SsrfSafeConnectHandler"/>)
    /// para evitar bypass por redirects o DNS rebinding.
    /// </summary>
    public static bool IsPrivateOrLoopback(IPAddress ip)
    {
        if (ip.IsIPv4MappedToIPv6)
        {
            ip = ip.MapToIPv4();
        }

        if (IPAddress.IsLoopback(ip))
            return true;

        if (ip.AddressFamily == AddressFamily.InterNetwork)
        {
            var bytes = ip.GetAddressBytes();

            if (bytes[0] == 0) return true;
            if (bytes[0] == 10) return true;
            if (bytes[0] == 172 && bytes[1] is >= 16 and <= 31) return true;
            if (bytes[0] == 192 && bytes[1] == 168) return true;
            if (bytes[0] == 169 && bytes[1] == 254) return true;
        }
        else if (ip.AddressFamily == AddressFamily.InterNetworkV6)
        {
            if (ip.IsIPv6LinkLocal || ip.IsIPv6SiteLocal || ip.IsIPv6UniqueLocal)
                return true;
        }

        return false;
    }
}
