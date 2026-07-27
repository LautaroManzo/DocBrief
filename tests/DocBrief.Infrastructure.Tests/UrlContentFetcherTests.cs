using System.Net;
using DocBrief.Infrastructure.Web;

namespace DocBrief.Infrastructure.Tests;

public class UrlContentFetcherTests
{
    [Theory]
    [InlineData("10.0.0.5", true)]        // rango privado
    [InlineData("192.168.1.1", true)]     // rango privado
    [InlineData("172.20.0.1", true)]      // rango privado
    [InlineData("127.0.0.1", true)]       // loopback
    [InlineData("169.254.1.1", true)]     // link-local
    [InlineData("8.8.8.8", false)]        // IP publica
    [InlineData("1.1.1.1", false)]        // IP publica
    public void IsPrivateOrLoopback_DetectaCorrectamenteSegunElRangoDeIp(string ip, bool esperado)
    {
        var resultado = UrlContentFetcher.IsPrivateOrLoopback(IPAddress.Parse(ip));

        Assert.Equal(esperado, resultado);
    }
}
