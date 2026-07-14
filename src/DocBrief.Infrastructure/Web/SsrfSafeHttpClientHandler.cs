using System.Net;
using System.Net.Sockets;

namespace DocBrief.Infrastructure.Web;

/// <summary>
/// Handler HTTP que valida la IP de destino justo antes de abrir cada conexion TCP
/// (incluyendo las que se abren al seguir redirects). Esto evita que un servidor
/// malicioso bypasee la validacion de URL con un redirect a una IP interna, o con
/// DNS rebinding (devolver una IP distinta entre el chequeo y la conexion real).
/// </summary>
public static class SsrfSafeHttpClientHandler
{
    public static SocketsHttpHandler Create()
    {
        return new SocketsHttpHandler
        {
            AllowAutoRedirect = true,
            MaxAutomaticRedirections = 5,
            ConnectCallback = async (context, cancellationToken) =>
            {
                var host = context.DnsEndPoint.Host;
                var port = context.DnsEndPoint.Port;

                var addresses = await Dns.GetHostAddressesAsync(host, cancellationToken);
                var safeAddress = addresses.FirstOrDefault(a => !UrlContentFetcher.IsPrivateOrLoopback(a));

                if (safeAddress is null)
                    throw new InvalidOperationException("Esa URL no esta permitida.");

                var socket = new Socket(SocketType.Stream, ProtocolType.Tcp) { NoDelay = true };

                try
                {
                    await socket.ConnectAsync(safeAddress, port, cancellationToken);
                    return new NetworkStream(socket, ownsSocket: true);
                }
                catch
                {
                    socket.Dispose();
                    throw;
                }
            }
        };
    }
}
