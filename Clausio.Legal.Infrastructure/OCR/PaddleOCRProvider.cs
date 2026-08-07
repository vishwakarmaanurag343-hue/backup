using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Clausio.Legal.Core.Interfaces.OCR;
using Microsoft.Extensions.Logging;

namespace Clausio.Legal.Infrastructure.OCR;

/// <summary>
/// This provider currently acts as a stub/simulator for PaddleOCR.
/// In a real deployment, this would invoke a local Python CLI script running PaddleOCR
/// or send an HTTP request to a dedicated OCR microservice container.
/// </summary>
public class PaddleOCRProvider : IOCRProvider
{
    private readonly ILogger<PaddleOCRProvider> _logger;

    public PaddleOCRProvider(ILogger<PaddleOCRProvider> logger)
    {
        _logger = logger;
    }

    public async Task<OCRResult> ExtractTextAsync(Stream fileStream, string contentType, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("[OCR] Extracting text from stream using local PaddleOCR service. Size {Length} bytes, Type: {Type}", fileStream.Length, contentType);
        
        // Reset stream position if needed by other components
        if (fileStream.CanSeek) fileStream.Position = 0;

        using var http = new System.Net.Http.HttpClient();
        http.Timeout = System.TimeSpan.FromSeconds(180); // OCR can take some time for large PDFs
        
        using var request = new System.Net.Http.HttpRequestMessage(System.Net.Http.HttpMethod.Post, "http://localhost:8000/api/ocr");
        
        var content = new System.Net.Http.MultipartFormDataContent();
        
        // Determine extension based on content type
        string filename = "upload.png";
        if (contentType.Contains("pdf")) filename = "upload.pdf";
        else if (contentType.Contains("jpeg") || contentType.Contains("jpg")) filename = "upload.jpg";

        var streamContent = new System.Net.Http.StreamContent(fileStream);
        streamContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType ?? "application/octet-stream");
        content.Add(streamContent, "file", filename);
        
        request.Content = content;

        try 
        {
            var response = await http.SendAsync(request, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
                var parsed = System.Text.Json.JsonDocument.Parse(responseJson);
                var responseText = parsed.RootElement.GetProperty("text").GetString() ?? string.Empty;
                    
                _logger.LogInformation("[OCR] PaddleOCR extraction complete. Result length: {Len}", responseText.Length);
                return new OCRResult(responseText.Trim(), 0.95);
            }
            else 
            {
                var error = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning("[OCR] PaddleOCR extraction failed with status {Status}: {Error}", response.StatusCode, error);
            }
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, "[OCR] Exception during local PaddleOCR extraction");
        }

        // Fallback if vision fails
        return new OCRResult("Error: Unable to extract text from the provided file via PaddleOCR.", 0.0);
    }
}
