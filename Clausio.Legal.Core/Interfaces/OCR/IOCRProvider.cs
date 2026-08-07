using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Clausio.Legal.Core.Interfaces.OCR;

public record OCRResult(string Text, double Confidence);

public interface IOCRProvider
{
    Task<OCRResult> ExtractTextAsync(Stream fileStream, string contentType, CancellationToken cancellationToken = default);
}
