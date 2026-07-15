using UglyToad.PdfPig;

if (args.Length == 0)
{
    Console.WriteLine("Uso: dotnet run --project tests/DocBrief.TestConsole -- <ruta-al-pdf>");
    return;
}

var pdfPath = args[0];

using var document = PdfDocument.Open(pdfPath);

Console.WriteLine($"Paginas: {document.NumberOfPages}");
Console.WriteLine(new string('-', 50));

foreach (var page in document.GetPages())
{
    var text = page.Text;
    Console.WriteLine($"--- Pagina {page.Number} ({text.Length} chars) ---");
    Console.WriteLine(text[..Math.Min(400, text.Length)]);
    Console.WriteLine();
}

Console.WriteLine("PDF parseado exitosamente!");
