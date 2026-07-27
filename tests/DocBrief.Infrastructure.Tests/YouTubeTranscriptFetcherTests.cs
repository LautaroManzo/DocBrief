using DocBrief.Infrastructure.Web;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace DocBrief.Infrastructure.Tests;

public class YouTubeTranscriptFetcherTests
{
    private static YouTubeTranscriptFetcher CreateFetcher() => new(
        new HttpClient(),
        new ConfigurationBuilder().Build(),
        NullLogger<YouTubeTranscriptFetcher>.Instance);

    [Theory]
    [InlineData("https://www.youtube.com/watch?v=dQw4w9WgXcQ", true)]
    [InlineData("https://youtu.be/dQw4w9WgXcQ", true)]
    [InlineData("https://www.youtube.com/shorts/dQw4w9WgXcQ", true)]
    [InlineData("https://www.youtube.com/embed/dQw4w9WgXcQ", true)]
    [InlineData("https://www.wikipedia.org/articulo", false)]
    [InlineData("https://vimeo.com/12345", false)]
    public void IsYouTubeUrl_DetectaLosFormatosDeLinkDeYoutube(string url, bool esperado)
    {
        var fetcher = CreateFetcher();

        var resultado = fetcher.IsYouTubeUrl(url);

        Assert.Equal(esperado, resultado);
    }
}
