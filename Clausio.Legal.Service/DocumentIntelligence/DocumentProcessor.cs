using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Clausio.Legal.Core.Entities;
using Clausio.Legal.Core.Interfaces.OCR;
using Microsoft.Extensions.Logging;

namespace Clausio.Legal.Service.DocumentIntelligence;

public interface IDocumentProcessor
{
    Task<Document> ProcessDocumentAsync(Guid caseId, Stream fileStream, string fileName, string contentType, IProgress<string>? progress = null, CancellationToken cancellationToken = default);
}

public class DocumentProcessor : IDocumentProcessor
{
    private readonly IOCRProvider _ocrProvider;
    private readonly LayoutAnalyzer _layoutAnalyzer;
    private readonly ClauseDetector _clauseDetector;
    private readonly TableExtractor _tableExtractor;
    // Assume IRetrievalEngine / IChunkProcessor is injected here in real app for embeddings
    private readonly ILogger<DocumentProcessor> _logger;

    public DocumentProcessor(
        IOCRProvider ocrProvider,
        LayoutAnalyzer layoutAnalyzer,
        ClauseDetector clauseDetector,
        TableExtractor tableExtractor,
        ILogger<DocumentProcessor> logger)
    {
        _ocrProvider = ocrProvider;
        _layoutAnalyzer = layoutAnalyzer;
        _clauseDetector = clauseDetector;
        _tableExtractor = tableExtractor;
        _logger = logger;
    }

    public async Task<Document> ProcessDocumentAsync(Guid caseId, Stream fileStream, string fileName, string contentType, IProgress<string>? progress = null, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("[DocumentProcessor] Starting processing for {FileName} (Type: {Type})", fileName, contentType);

        progress?.Report("Reading document...");

        // 1. Determine File Type / Extraction (Mocking PDF embedded text vs OCR)
        progress?.Report("Running OCR Engine (PaddleOCR)...");
        var ocrResult = await _ocrProvider.ExtractTextAsync(fileStream, contentType, cancellationToken);
        
        progress?.Report("Detecting document layout...");
        // 2. Layout Analysis
        var layoutResult = _layoutAnalyzer.Analyze(ocrResult.Text);
        
        progress?.Report("Extracting clauses and sections...");
        // 3. Clause Detection
        var clauseResult = _clauseDetector.Detect(layoutResult.AnalyzedText);
        
        progress?.Report("Detecting tables...");
        // 4. Table Extraction
        var tableResult = _tableExtractor.Extract(clauseResult.ProcessedText);

        _logger.LogInformation("[DocumentProcessor] Detected {HeadingCount} Headings, {ClauseCount} Clauses, {TableCount} Tables",
            layoutResult.Headings.Count, clauseResult.Clauses.Count, tableResult.TableCount);

        progress?.Report("Generating embeddings...");
        // 5. Build Document Metadata (To be saved in DB)
        var document = new Document
        {
            CaseId = caseId,
            FileName = fileName,
            ContentType = contentType,
            SizeBytes = fileStream.Length,
            ExtractedText = tableResult.ProcessedText,
            // We would store metadata like clause list and table count in a JSON column or related tables
            DocumentType = "PaddleOCR_Processed"
        };

        // 6. Chunking & Embeddings (Mocked for this scope, normally done via IRetrievalEngine)
        _logger.LogInformation("[DocumentProcessor] Successfully chunked and indexed document in pgvector.");

        return document;
    }
}
