using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Clausio.Legal.Core.Entities;
using Clausio.Legal.Core.Interfaces.Retrieval;
using Microsoft.Extensions.Logging;

namespace Clausio.Legal.Service.Retrieval.Chunking;

public class ChunkProcessor : IChunkProcessor
{
    private readonly ILogger<ChunkProcessor> _logger;
    private const int MaxChunkLength = 2000;
    private const int OverlapLength = 200;

    public ChunkProcessor(ILogger<ChunkProcessor> logger)
    {
        _logger = logger;
    }

    public List<DocumentChunk> Process(string text, Guid documentId, Guid caseId, string? documentType)
    {
        var chunks = new List<DocumentChunk>();
        
        if (string.IsNullOrWhiteSpace(text))
            return chunks;

        _logger.LogInformation("Processing chunks for document {DocumentId}", documentId);

        // Simple sentence/paragraph-aware chunking
        // A robust legal text chunker would look for Section/Article headers, but here we split by paragraphs first
        var paragraphs = Regex.Split(text, @"\n\s*\n");
        
        var currentChunkText = string.Empty;
        var currentHeading = string.Empty;
        var currentSection = string.Empty;

        // Try to identify headings using simple heuristics (e.g. all caps short lines)
        var headingRegex = new Regex(@"^([A-Z0-9\s\.\-]{3,50})$", RegexOptions.Multiline);

        foreach (var p in paragraphs)
        {
            var paragraph = p.Trim();
            if (string.IsNullOrEmpty(paragraph)) continue;

            var match = headingRegex.Match(paragraph);
            if (match.Success && match.Length == paragraph.Length)
            {
                currentHeading = paragraph;
                // If it looks like a section (e.g., "Section 1")
                if (paragraph.Contains("SECTION", StringComparison.OrdinalIgnoreCase) || 
                    paragraph.Contains("ARTICLE", StringComparison.OrdinalIgnoreCase))
                {
                    currentSection = paragraph;
                }
            }

            if (currentChunkText.Length + paragraph.Length > MaxChunkLength && currentChunkText.Length > 0)
            {
                chunks.Add(CreateChunk(documentId, caseId, documentType, currentChunkText, currentHeading, currentSection));
                
                // Keep overlap from the end of the previous chunk
                var overlapStart = Math.Max(0, currentChunkText.Length - OverlapLength);
                var overlapText = currentChunkText.Substring(overlapStart);
                
                // Ensure we don't break in the middle of a word for overlap
                var lastSpace = overlapText.IndexOf(' ');
                if (lastSpace >= 0 && lastSpace < overlapText.Length - 1)
                {
                    overlapText = overlapText.Substring(lastSpace + 1);
                }
                
                currentChunkText = overlapText + "\n\n" + paragraph;
            }
            else
            {
                if (currentChunkText.Length > 0)
                    currentChunkText += "\n\n";
                currentChunkText += paragraph;
            }
        }

        if (currentChunkText.Length > 0)
        {
            chunks.Add(CreateChunk(documentId, caseId, documentType, currentChunkText, currentHeading, currentSection));
        }

        return chunks;
    }

    private DocumentChunk CreateChunk(Guid documentId, Guid caseId, string? documentType, string text, string heading, string section)
    {
        return new DocumentChunk
        {
            Id = Guid.NewGuid(),
            DocumentId = documentId,
            CaseId = caseId,
            DocumentType = documentType,
            TextContent = text.Trim(),
            Heading = string.IsNullOrEmpty(heading) ? null : heading,
            Section = string.IsNullOrEmpty(section) ? null : section,
            CreatedAt = DateTime.UtcNow
        };
    }
}
