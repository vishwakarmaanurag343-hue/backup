using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Clausio.Legal.Core.Interfaces.OCR;

namespace Clausio.Legal.Infrastructure.Extraction;

public class OcrExtractionStrategy : IDocumentTextExtractionStrategy
{
    private readonly IOCRProvider _ocrProvider;

    public OcrExtractionStrategy(IOCRProvider ocrProvider)
    {
        _ocrProvider = ocrProvider;
    }

    public bool CanHandle(string fileExtension)
    {
        if (string.IsNullOrWhiteSpace(fileExtension)) return false;
        var ext = fileExtension.ToLowerInvariant();
        return ext == ".pdf" || ext == ".png" || ext == ".jpg" || ext == ".jpeg" || ext == ".bmp" || ext == ".tiff";
    }

    public async Task<string?> ExtractAsync(string storagePath, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(storagePath)) return null;

        var ext = Path.GetExtension(storagePath).ToLowerInvariant();
        var contentType = ext switch
        {
            ".pdf" => "application/pdf",
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            _ => "application/octet-stream"
        };

        using var stream = File.OpenRead(storagePath);
        var result = await _ocrProvider.ExtractTextAsync(stream, contentType, cancellationToken);
        return result?.Text;
    }
}
