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
    private readonly Clausio.Legal.Core.Interfaces.Retrieval.IChunkProcessor _chunkProcessor;
    private readonly Clausio.Legal.Core.Interfaces.Embedding.IEmbeddingProvider _embeddingProvider;
    private readonly Clausio.Legal.Infrastructure.ClausioDbContext _db;
    private readonly ILogger<DocumentProcessor> _logger;

    public DocumentProcessor(
        IOCRProvider ocrProvider,
        LayoutAnalyzer layoutAnalyzer,
        ClauseDetector clauseDetector,
        TableExtractor tableExtractor,
        Clausio.Legal.Core.Interfaces.Retrieval.IChunkProcessor chunkProcessor,
        Clausio.Legal.Core.Interfaces.Embedding.IEmbeddingProvider embeddingProvider,
        Clausio.Legal.Infrastructure.ClausioDbContext db,
        ILogger<DocumentProcessor> logger)
    {
        _ocrProvider = ocrProvider;
        _layoutAnalyzer = layoutAnalyzer;
        _clauseDetector = clauseDetector;
        _tableExtractor = tableExtractor;
        _chunkProcessor = chunkProcessor;
        _embeddingProvider = embeddingProvider;
        _db = db;
        _logger = logger;
    }

    public async Task<Document> ProcessDocumentAsync(Guid caseId, Stream fileStream, string fileName, string contentType, IProgress<string>? progress = null, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("[DocumentProcessor] Starting processing for {FileName} (Type: {Type})", fileName, contentType);

        progress?.Report("Reading document...");

        // 1. Determine File Type / Extraction (PaddleOCR)
        progress?.Report("Running OCR Engine (PaddleOCR)...");
        var ocrResult = await _ocrProvider.ExtractTextAsync(fileStream, contentType, cancellationToken);
        
        progress?.Report("Detecting document layout...");
        var layoutResult = _layoutAnalyzer.Analyze(ocrResult.Text);
        
        progress?.Report("Extracting clauses and sections...");
        var clauseResult = _clauseDetector.Detect(layoutResult.AnalyzedText);
        
        progress?.Report("Detecting tables...");
        var tableResult = _tableExtractor.Extract(clauseResult.ProcessedText);

        _logger.LogInformation("[DocumentProcessor] Detected {HeadingCount} Headings, {ClauseCount} Clauses, {TableCount} Tables",
            layoutResult.Headings.Count, clauseResult.Clauses.Count, tableResult.TableCount);

        // 2. Build Document Record
        var document = new Document
        {
            CaseId = caseId,
            FileName = fileName,
            ContentType = contentType,
            SizeBytes = fileStream.Length,
            ExtractedText = tableResult.ProcessedText,
            DocumentType = "PaddleOCR_Processed"
        };

        _db.Documents.Add(document);
        await _db.SaveChangesAsync(cancellationToken);

        // 3. Chunking, Vector Embedding & Indexing
        progress?.Report("Generating chunks & vector embeddings...");
        var chunks = _chunkProcessor.Process(document.ExtractedText, document.Id, caseId, document.DocumentType);
        
        foreach (var chunk in chunks)
        {
            var vector = await _embeddingProvider.GenerateEmbeddingAsync(chunk.TextContent, cancellationToken);
            chunk.Embedding = new Pgvector.Vector(vector);
            _db.DocumentChunks.Add(chunk);
        }

        await _db.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("[DocumentProcessor] Created and indexed {ChunkCount} chunks in pgvector.", chunks.Count);

        return document;
    }
}
