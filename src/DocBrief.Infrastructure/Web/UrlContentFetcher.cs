using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using DocBrief.Application.Interfaces;
using HtmlAgilityPack;

namespace DocBrief.Infrastructure.Web;

public class UrlContentFetcher : IUrlContentFetcher
{
    private const long MaxContentLengthBytes = 5 * 1024 * 1024;

    private static readonly string[] RemovableTags = { "script", "style", "nav", "header", "footer", "noscript", "svg" };

    private readonly HttpClient _httpClient;

    public UrlContentFetcher(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<string> FetchTextAsync(string url)
    {
        var uri = ValidateUrl(url);

        using var response = await _httpClient.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead);
        response.EnsureSuccessStatusCode();

        if (response.Content.Headers.ContentLength is > MaxContentLengthBytes)
            throw new InvalidOperationException("La pagina supera el limite de tamano permitido.");

        var html = await response.Content.ReadAsStringAsync();

        var document = new HtmlDocument();
        document.LoadHtml(html);

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

        return text;
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
