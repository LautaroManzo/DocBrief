using DocBrief.Application.Interfaces;
using DocBrief.Application.UseCases.SummarizeDocument;
using Microsoft.AspNetCore.Http;
using Moq;

namespace DocBrief.Application.Tests;

public class SummarizeDocumentHandlerTests
{
    private readonly Mock<IDocumentParserResolver> _parserResolver = new();
    private readonly Mock<IUrlContentFetcher> _urlContentFetcher = new();
    private readonly Mock<IYouTubeTranscriptFetcher> _youTubeTranscriptFetcher = new();
    private readonly Mock<ISummaryService> _summaryService = new();

    private SummarizeDocumentHandler CreateHandler() => new(
        _parserResolver.Object,
        _urlContentFetcher.Object,
        _youTubeTranscriptFetcher.Object,
        _summaryService.Object);

    [Fact]
    public async Task Handle_File_UsaElParserCorrectoYResumeElTextoExtraido()
    {
        var parser = new Mock<IDocumentParser>();
        parser.Setup(p => p.ParseAsync(It.IsAny<IFormFile>())).ReturnsAsync("texto del pdf");

        var file = new Mock<IFormFile>();
        file.Setup(f => f.FileName).Returns("documento.pdf");

        _parserResolver.Setup(r => r.Resolve("documento.pdf")).Returns(parser.Object);
        _summaryService
            .Setup(s => s.SummarizeAsync("texto del pdf", "basico", "es", false))
            .ReturnsAsync("resumen generado");

        var handler = CreateHandler();
        var command = new SummarizeDocumentCommand(file.Object, null, null, "file", "basico", "es");

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.Equal("resumen generado", result.Summary);
        _summaryService.Verify(s => s.SummarizeAsync("texto del pdf", "basico", "es", false), Times.Once);
    }

    [Fact]
    public async Task Handle_Texto_UsaElTextoPegadoTalCual()
    {
        _summaryService
            .Setup(s => s.SummarizeAsync("hola mundo", "basico", "es", false))
            .ReturnsAsync("resumen de texto");

        var handler = CreateHandler();
        var command = new SummarizeDocumentCommand(null, "hola mundo", null, "text", "basico", "es");

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.Equal("resumen de texto", result.Summary);
        _urlContentFetcher.Verify(f => f.FetchAsync(It.IsAny<string>()), Times.Never);
        _youTubeTranscriptFetcher.Verify(f => f.FetchTranscriptAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Handle_UrlGenerica_UsaElFetcherDePaginasWeb()
    {
        const string url = "https://ejemplo.com/articulo";

        _youTubeTranscriptFetcher.Setup(f => f.IsYouTubeUrl(url)).Returns(false);
        _urlContentFetcher
            .Setup(f => f.FetchAsync(url))
            .ReturnsAsync(new UrlContent("contenido del articulo", "Titulo del articulo"));
        _summaryService
            .Setup(s => s.SummarizeAsync("contenido del articulo", "basico", "es", false))
            .ReturnsAsync("resumen del articulo");

        var handler = CreateHandler();
        var command = new SummarizeDocumentCommand(null, null, url, "url", "basico", "es");

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.Equal("resumen del articulo", result.Summary);
        Assert.Equal("Titulo del articulo", result.SourceTitle);
        _youTubeTranscriptFetcher.Verify(f => f.FetchTranscriptAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Handle_UrlDeYouTube_UsaElFetcherDeTranscripciones()
    {
        const string url = "https://www.youtube.com/watch?v=abc12345678";

        _youTubeTranscriptFetcher.Setup(f => f.IsYouTubeUrl(url)).Returns(true);
        _youTubeTranscriptFetcher.Setup(f => f.FetchTranscriptAsync(url)).ReturnsAsync("transcripcion del video");
        _youTubeTranscriptFetcher.Setup(f => f.GetTitleAsync(url)).ReturnsAsync("Titulo del video");
        _summaryService
            .Setup(s => s.SummarizeAsync("transcripcion del video", "basico", "es", false))
            .ReturnsAsync("resumen del video");

        var handler = CreateHandler();
        var command = new SummarizeDocumentCommand(null, null, url, "url", "basico", "es");

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.Equal("resumen del video", result.Summary);
        Assert.Equal("Titulo del video", result.SourceTitle);
        _urlContentFetcher.Verify(f => f.FetchAsync(It.IsAny<string>()), Times.Never);
    }
}
