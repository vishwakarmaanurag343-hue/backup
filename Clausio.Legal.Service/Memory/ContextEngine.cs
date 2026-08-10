using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Clausio.Legal.Core.Interfaces.Memory;
using Clausio.Legal.Core.Interfaces.Retrieval;
using Microsoft.Extensions.Logging;

namespace Clausio.Legal.Service.Memory;

public class ContextEngine : IContextEngine
{
    private readonly IMemoryStore _memoryStore;
    private readonly IRetrievalEngine _retrievalEngine;
    private readonly IContextRanker _contextRanker;
    private readonly Clausio.Legal.Infrastructure.ClausioDbContext _db;
    private readonly ILogger<ContextEngine> _logger;

    public ContextEngine(
        IMemoryStore memoryStore, 
        IRetrievalEngine retrievalEngine, 
        IContextRanker contextRanker,
        Clausio.Legal.Infrastructure.ClausioDbContext db,
        ILogger<ContextEngine> logger)
    {
        _memoryStore = memoryStore;
        _retrievalEngine = retrievalEngine;
        _contextRanker = contextRanker;
        _db = db;
        _logger = logger;
    }

    public async Task<string> BuildChatContextAsync(Guid caseId, string userQuery, CancellationToken cancellationToken = default)
    {
        var sb = new StringBuilder();
        
        // 1. Get Case Memory (Core Facts)
        var caseMemory = await _memoryStore.GetCaseMemoryAsync(caseId, cancellationToken);
        if (caseMemory != null)
        {
            sb.AppendLine("<case_context>");
            sb.AppendLine($"Title: {caseMemory.CaseTitle}");
            sb.AppendLine($"Type: {caseMemory.CaseType}");
            sb.AppendLine($"Status: {caseMemory.CurrentStatus}");
            sb.AppendLine($"Summary: {caseMemory.ShortSummary}");
            sb.AppendLine($"Key Facts: {caseMemory.KeyFacts}");
            sb.AppendLine($"Objective: {caseMemory.CurrentObjective}");
            sb.AppendLine("</case_context>");
        }
        else
        {
            // Fallback: If AI CaseMemory is not generated yet, fetch basic case details directly from the Database
            var basicCaseInfo = _db.Cases.FirstOrDefault(c => c.Id == caseId);
            if (basicCaseInfo != null)
            {
                sb.AppendLine("<case_context>");
                sb.AppendLine($"Title: {basicCaseInfo.Name}");
                sb.AppendLine($"Type: {basicCaseInfo.CaseType}");
                sb.AppendLine($"Status: {basicCaseInfo.Status}");
                sb.AppendLine($"Stage: {basicCaseInfo.Stage}");
                sb.AppendLine($"Court: {basicCaseInfo.Court}");
                sb.AppendLine("</case_context>");
            }
        }

        // 2. Get Recent Conversation History
        var recentConversations = await _memoryStore.GetRecentConversationsAsync(caseId, 5, cancellationToken);
        if (recentConversations.Any())
        {
            sb.AppendLine("<recent_conversation>");
            foreach (var msg in recentConversations)
            {
                sb.AppendLine($"Summary: {msg.ConversationSummary}");
                sb.AppendLine($"Decisions: {msg.ImportantDecisions}");
            }
            sb.AppendLine("</recent_conversation>");
        }

        // 2.5 Inject Long-Term Advocate Preferences & Draft Memories
        var userPref = _db.UserPreferences.FirstOrDefault();
        if (userPref != null)
        {
            sb.AppendLine("<user_preferences>");
            sb.AppendLine($"WritingStyle: {userPref.WritingStyle}");
            sb.AppendLine($"PreferredLanguage: {userPref.PreferredLanguage}");
            sb.AppendLine($"CitationStyle: {userPref.CitationStyle}");
            sb.AppendLine($"PreferredJurisdiction: {userPref.PreferredJurisdiction}");
            sb.AppendLine("</user_preferences>");
        }

        var draftMemories = await _memoryStore.GetRecentDraftsAsync(caseId, 3, cancellationToken);
        if (draftMemories.Any())
        {
            sb.AppendLine("<draft_preferences_history>");
            foreach (var draft in draftMemories)
            {
                sb.AppendLine($"DraftType: {draft.DraftType}, Version: {draft.DraftVersion}, Status: {draft.DraftStatus}");
            }
            sb.AppendLine("</draft_preferences_history>");
        }

        // 3. Document attachment override (Bypass RAG if user attached a specific file)
        var attachedDocText = "";
        if (userQuery.Contains("📎 Attached Document: ["))
        {
            try
            {
                var fileNameStart = userQuery.IndexOf("📎 Attached Document: [") + 23;
                var bracketEnd = userQuery.IndexOf(']', fileNameStart);
                if (bracketEnd != -1)
                {
                    var fileName = userQuery.Substring(fileNameStart, bracketEnd - fileNameStart).Trim();
                    // Fetch the latest document with this exact filename for this case
                    var recentDoc = _db.Documents
                        .Where(d => d.CaseId == caseId && d.FileName == fileName)
                        .OrderByDescending(d => d.CreatedAt)
                        .FirstOrDefault();
                    
                    if (recentDoc != null && !string.IsNullOrWhiteSpace(recentDoc.ExtractedText))
                    {
                        attachedDocText = recentDoc.ExtractedText;
                        sb.AppendLine("<retrieved_evidence>");
                        sb.AppendLine($"[Document: {recentDoc.FileName}] {recentDoc.ExtractedText}");
                        sb.AppendLine("</retrieved_evidence>");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to inject attached document directly into context");
            }
        }

        // 4. RAG Retrieval for the specific query (Fallback to semantic search for everything else)
        if (string.IsNullOrEmpty(attachedDocText))
        {
            var relevantChunks = await _retrievalEngine.GetContextAsync(userQuery, caseId, cancellationToken);
            if (relevantChunks != null && relevantChunks.Any())
            {
                sb.AppendLine("<retrieved_evidence>");
                foreach (var chunk in relevantChunks)
                {
                    sb.AppendLine($"[Document: {chunk.DocumentType ?? "Unknown"}] {chunk.TextContent}");
                }
                sb.AppendLine("</retrieved_evidence>");
            }
            else
            {
                // Fallback: If RAG chunks are empty, directly load distinct recent uploaded case documents from database
                var recentDocs = _db.Documents
                    .Where(d => d.CaseId == caseId && !string.IsNullOrWhiteSpace(d.ExtractedText) && !d.ExtractedText.StartsWith("Error") && !d.ExtractedText.StartsWith("--- MOCK"))
                    .OrderByDescending(d => d.CreatedAt)
                    .ToList()
                    .DistinctBy(d => d.ExtractedText.Trim())
                    .Take(10)
                    .ToList();

                if (recentDocs.Any())
                {
                    sb.AppendLine("<retrieved_evidence>");
                    foreach (var doc in recentDocs)
                    {
                        sb.AppendLine($"[Document: {doc.FileName}] {doc.ExtractedText}");
                    }
                    sb.AppendLine("</retrieved_evidence>");
                }
            }
        }

        return await _contextRanker.ScoreRankAndCompressAsync(sb.ToString(), 800);
    }

    public async Task<string> BuildDraftingContextAsync(Guid caseId, string documentType, string specificInstructions, CancellationToken cancellationToken = default)
    {
        var sb = new StringBuilder();
        
        // 1. Core Facts needed for drafting
        var caseMemory = await _memoryStore.GetCaseMemoryAsync(caseId, cancellationToken);
        if (caseMemory != null)
        {
            sb.AppendLine("<case_context>");
            sb.AppendLine($"Title: {caseMemory.CaseTitle}");
            sb.AppendLine($"Parties: {caseMemory.Parties}");
            sb.AppendLine($"Key Facts: {caseMemory.KeyFacts}");
            sb.AppendLine($"Important Dates: {caseMemory.ImportantDates}");
            sb.AppendLine($"Legal Issues: {caseMemory.LegalIssues}");
            sb.AppendLine("</case_context>");
        }

        // 2. Retrieve precedents or relevant documents for drafting
        var query = $"Template or precedents for {documentType}. {specificInstructions}";
        var relevantChunks = await _retrievalEngine.GetContextAsync(query, caseId, cancellationToken);
        if (relevantChunks.Any())
        {
            sb.AppendLine("<retrieved_evidence>");
            foreach (var chunk in relevantChunks)
            {
                sb.AppendLine($"[Document: {chunk.DocumentType ?? "Unknown"}] {chunk.TextContent}");
            }
            sb.AppendLine("</retrieved_evidence>");
        }

        return await _contextRanker.ScoreRankAndCompressAsync(sb.ToString(), 1500);
    }

    public async Task<string> BuildAnalysisContextAsync(Guid caseId, string analysisType, CancellationToken cancellationToken = default)
    {
        var sb = new StringBuilder();
        
        var caseMemory = await _memoryStore.GetCaseMemoryAsync(caseId, cancellationToken);
        if (caseMemory != null)
        {
            sb.AppendLine("<case_context>");
            sb.AppendLine($"Title: {caseMemory.CaseTitle}");
            sb.AppendLine($"Type: {caseMemory.CaseType}");
            sb.AppendLine($"Summary: {caseMemory.ShortSummary}");
            sb.AppendLine($"Key Facts: {caseMemory.KeyFacts}");
            sb.AppendLine("</case_context>");
        }

        // Broad retrieval for analysis
        var query = $"All critical facts and evidence for {analysisType}";
        var relevantChunks = await _retrievalEngine.GetContextAsync(query, caseId, cancellationToken);
        if (relevantChunks.Any())
        {
            sb.AppendLine("<retrieved_evidence>");
            foreach (var chunk in relevantChunks)
            {
                sb.AppendLine($"[Source: {chunk.DocumentType ?? "Unknown"}] {chunk.TextContent}");
            }
            sb.AppendLine("</retrieved_evidence>");
        }

        return await _contextRanker.ScoreRankAndCompressAsync(sb.ToString(), 2000);
    }
}
