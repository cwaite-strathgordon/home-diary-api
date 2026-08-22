using System.IO.Compression;
using System.Text;
using System.Xml.Linq;
using UglyToad.PdfPig;

namespace HomeDiary_api.Services;

public class DocumentTextExtractor
{
    public string Extract(string fileName, byte[] data)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        var extracted = extension switch
        {
            ".pdf" => ExtractPdf(data),
            ".docx" => ExtractDocx(data),
            ".txt" or ".md" or ".csv" => Encoding.UTF8.GetString(data),
            _ => string.Empty
        };
        // PostgreSQL text values cannot contain the Unicode null character.
        return extracted.Replace("\0", string.Empty);
    }

    private static string ExtractPdf(byte[] data)
    {
        using var stream = new MemoryStream(data);
        using var document = PdfDocument.Open(stream);
        return string.Join(Environment.NewLine, document.GetPages().Select(page => page.Text));
    }

    private static string ExtractDocx(byte[] data)
    {
        using var stream = new MemoryStream(data);
        using var archive = new ZipArchive(stream, ZipArchiveMode.Read);
        var entry = archive.GetEntry("word/document.xml");
        if (entry is null) return string.Empty;
        using var xmlStream = entry.Open();
        var document = XDocument.Load(xmlStream);
        XNamespace word = "http://schemas.openxmlformats.org/wordprocessingml/2006/main";
        return string.Join(" ", document.Descendants(word + "t").Select(node => node.Value));
    }
}
