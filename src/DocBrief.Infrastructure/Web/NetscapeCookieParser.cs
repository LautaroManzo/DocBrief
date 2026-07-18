using System.Net;

namespace DocBrief.Infrastructure.Web;

/// <summary>
/// Parsea cookies exportadas en formato Netscape (cookies.txt), el que generan
/// extensiones como "Get cookies.txt LOCALLY". Formato por linea, separado por tabs:
/// dominio, incluirSubdominios, path, secure, expiracion(unix), nombre, valor.
/// </summary>
internal static class NetscapeCookieParser
{
    public static List<Cookie> Parse(string content)
    {
        var cookies = new List<Cookie>();

        foreach (var rawLine in content.Split('\n'))
        {
            var line = rawLine.TrimEnd('\r');

            if (line.StartsWith("#HttpOnly_"))
                line = line["#HttpOnly_".Length..];

            if (string.IsNullOrWhiteSpace(line) || line.StartsWith('#'))
                continue;

            var fields = line.Split('\t');
            if (fields.Length != 7)
                continue;

            var domain = fields[0];
            var path = fields[2];
            var secure = string.Equals(fields[3], "TRUE", StringComparison.OrdinalIgnoreCase);
            var name = fields[5];
            var value = fields[6];

            if (string.IsNullOrWhiteSpace(name))
                continue;

            cookies.Add(new Cookie(name, value, path, domain) { Secure = secure });
        }

        return cookies;
    }
}
