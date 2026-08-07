using System;
using System.Collections.Generic;
using System.Linq;
using Clausio.Legal.Core.Entities;
using Clausio.Legal.Core.Interfaces.Retrieval;

namespace Clausio.Legal.Service.Retrieval.Hybrid;

public class BM25Retriever : IBM25Retriever
{
    private const double K1 = 1.2;
    private const double B = 0.75;
    
    private List<DocumentChunk> _chunks = new();
    private Dictionary<string, int> _documentFrequency = new(StringComparer.OrdinalIgnoreCase);
    private Dictionary<string, Dictionary<Guid, int>> _termFrequency = new(StringComparer.OrdinalIgnoreCase);
    private Dictionary<Guid, int> _documentLengths = new();
    private double _averageDocumentLength = 0;
    
    public void BuildIndex(List<DocumentChunk> chunks)
    {
        _chunks = chunks ?? new List<DocumentChunk>();
        _documentFrequency.Clear();
        _termFrequency.Clear();
        _documentLengths.Clear();
        
        if (_chunks.Count == 0) return;

        double totalLength = 0;
        
        foreach (var chunk in _chunks)
        {
            var tokens = Tokenize(chunk.TextContent);
            _documentLengths[chunk.Id] = tokens.Count;
            totalLength += tokens.Count;
            
            var uniqueTokens = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            
            foreach (var token in tokens)
            {
                if (!_termFrequency.ContainsKey(token))
                    _termFrequency[token] = new Dictionary<Guid, int>();
                    
                if (!_termFrequency[token].ContainsKey(chunk.Id))
                    _termFrequency[token][chunk.Id] = 0;
                    
                _termFrequency[token][chunk.Id]++;
                
                if (uniqueTokens.Add(token))
                {
                    if (!_documentFrequency.ContainsKey(token))
                        _documentFrequency[token] = 0;
                    _documentFrequency[token]++;
                }
            }
        }
        
        _averageDocumentLength = totalLength / _chunks.Count;
    }

    public List<(DocumentChunk Chunk, double Score)> Search(string query, int topK = 15)
    {
        if (string.IsNullOrWhiteSpace(query) || _chunks.Count == 0)
            return new List<(DocumentChunk Chunk, double Score)>();

        var queryTokens = Tokenize(query);
        var scores = new Dictionary<Guid, double>();

        foreach (var token in queryTokens)
        {
            if (!_documentFrequency.ContainsKey(token)) continue;

            // IDF calculation
            var idf = Math.Log((_chunks.Count - _documentFrequency[token] + 0.5) / (_documentFrequency[token] + 0.5) + 1);

            if (_termFrequency.TryGetValue(token, out var termFreqs))
            {
                foreach (var (docId, tf) in termFreqs)
                {
                    var docLength = _documentLengths[docId];
                    // BM25 term weighting
                    var score = idf * (tf * (K1 + 1)) / (tf + K1 * (1 - B + B * (docLength / _averageDocumentLength)));
                    
                    if (!scores.ContainsKey(docId))
                        scores[docId] = 0;
                        
                    scores[docId] += score;
                }
            }
        }

        var result = _chunks
            .Where(c => scores.ContainsKey(c.Id))
            .Select(c => (Chunk: c, Score: scores[c.Id]))
            .OrderByDescending(x => x.Score)
            .Take(topK)
            .ToList();

        return result;
    }

    private List<string> Tokenize(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return new List<string>();
        
        var charArray = text.Where(c => char.IsLetterOrDigit(c) || char.IsWhiteSpace(c)).ToArray();
        return new string(charArray)
            .Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(t => t.ToLowerInvariant())
            .Where(t => t.Length > 2) // Very basic stop word filtering (remove short words)
            .ToList();
    }
}
