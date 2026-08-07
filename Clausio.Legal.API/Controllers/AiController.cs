using Clausio.Legal.Core.Dtos;
using Clausio.Legal.Service;
using Clausio.Legal.Service.DocumentIntelligence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Channels;

namespace Clausio.Legal.API.Controllers;

[Authorize]
[ApiController]
[Route("api/ai")]
public class AiController(IAiService aiService) : ControllerBase
{
    // ✅ Returns { summary: "..." } — matches frontend aiApi.getSummary()
    [HttpPost("summary/{caseId:guid}")]
    public async Task<IActionResult> Summary(Guid caseId, CancellationToken cancellationToken)
    {
        var result = await aiService.SummarizeCaseAsync(caseId, cancellationToken);
        return Ok(new { result = result });
    }

    // ✅ Returns { chronology: "..." }
    [HttpPost("chronology/{caseId:guid}")]
    public async Task<IActionResult> Chronology(Guid caseId, CancellationToken cancellationToken)
    {
        var result = await aiService.GenerateChronologyAsync(caseId, cancellationToken);
        return Ok(new { result = result });
    }

    // ✅ Returns { contradictions: "..." }
    [HttpPost("contradictions/{caseId:guid}")]
    public async Task<IActionResult> Contradictions(Guid caseId, CancellationToken cancellationToken)
    {
        var result = await aiService.DetectContradictionsAsync(caseId, cancellationToken);
        return Ok(new { contradictions = result });
    }

    // ✅ Returns { evidence: "..." }
    [HttpPost("evidence/{documentId:guid}")]
    public async Task<IActionResult> Evidence(Guid documentId, CancellationToken cancellationToken)
    {
        var result = await aiService.AnalyzeEvidenceAsync(documentId, cancellationToken);
        return Ok(new { result = result });
    }

    // ✅ Returns { judgments: "..." } — matches frontend aiApi.getLegalResearch()
    [HttpPost("research/{caseId:guid}")]
    public async Task<IActionResult> Research(Guid caseId, CancellationToken cancellationToken)
    {
        var result = await aiService.ResearchAsync(caseId, cancellationToken);
        return Ok(new { judgments = result });
    }

    // ✅ Returns { actionPlan: "..." } — matches frontend aiApi.getActionPlan()
    [HttpPost("actionplan/{caseId:guid}")]
    public async Task<IActionResult> ActionPlan(Guid caseId, CancellationToken cancellationToken)
    {
        var result = await aiService.GenerateActionPlanAsync(caseId, cancellationToken);
        return Ok(new { actionPlan = result });
    }

    // ✅ Returns { translatedText, detectedLanguage, originalText }
    [HttpPost("translate")]
    public async Task<IActionResult> Translate([FromBody] TranslateRequest request, CancellationToken cancellationToken)
    {
        var result = await aiService.TranslateAsync(request, cancellationToken);
        return Ok(new { translatedText = result, detectedLanguage = "Auto-detected", originalText = request.Text });
    }

    [AllowAnonymous]
    [HttpPost("chat/stream")]
    public async Task Chat([FromBody] ChatRequestDto request, CancellationToken cancellationToken)
    {
        Response.ContentType = "text/event-stream";
        try
        {
            var stream = aiService.StreamChatAsync(request, cancellationToken);
            await foreach (var chunk in stream)
            {
                var jsonChunk = System.Text.Json.JsonSerializer.Serialize(chunk);
                var data = $"data: {jsonChunk}\n\n";
                await Response.WriteAsync(data, cancellationToken);
                await Response.Body.FlushAsync(cancellationToken);
            }
        }
        catch (Exception ex)
        {
            var data = $"data: [sys] ERROR: {ex.Message}\n\n";
            await Response.WriteAsync(data, cancellationToken);
            await Response.Body.FlushAsync(cancellationToken);
        }
    }

    // ✅ Returns { message: "..." } — matches frontend aiApi.getWhatsApp()
    [HttpPost("whatsapp/{caseId:guid}")]
    public async Task<IActionResult> WhatsApp(Guid caseId, [FromBody] WhatsAppRequestDto request, CancellationToken cancellationToken)
    {
        var result = await aiService.DraftWhatsAppAsync(caseId, request, cancellationToken);
        return Ok(new { message = result });
    }

    // ✅ Returns { analysis: "..." } — matches frontend aiApi.getFinancial()
    [HttpPost("financial/{caseId:guid}")]
    public async Task<IActionResult> Financial(Guid caseId, CancellationToken cancellationToken)
    {
        var result = await aiService.AnalyzeFinancialsAsync(caseId, cancellationToken);
        return Ok(new { analysis = result });
    }

    // ✅ Returns { readiness: "..." } — matches frontend aiApi.getReadiness()
    [HttpPost("readiness/{caseId:guid}")]
    public async Task<IActionResult> Readiness(Guid caseId, CancellationToken cancellationToken)
    {
        var result = await aiService.AssessReadinessAsync(caseId, cancellationToken);
        return Ok(new { readiness = result });
    }

    // ✅ Returns { response: "..." } — matches frontend aiApi.getEmergency()
    [HttpPost("emergency/{caseId:guid}")]
    public async Task<IActionResult> Emergency(Guid caseId, [FromBody] EmergencyRequestDto request, CancellationToken cancellationToken)
    {
        var result = await aiService.EmergencyTriageAsync(request, cancellationToken);
        return Ok(new { response = result });
    }

    // ✅ Returns { brief: "..." } — matches frontend aiApi.getPrep()
    [HttpPost("prep/{caseId:guid}")]
    public async Task<IActionResult> Prep(Guid caseId, CancellationToken cancellationToken)
    {
        var result = await aiService.PrepHearingAsync(caseId, cancellationToken);
        return Ok(new { brief = result });
    }

    // ✅ Returns { intelligence: "..." } — matches frontend aiApi.getWitness()
    [HttpPost("witness/{caseId:guid}")]
    public async Task<IActionResult> Witness(Guid caseId, CancellationToken cancellationToken)
    {
        var result = await aiService.PrepWitnessAsync(caseId, cancellationToken);
        return Ok(new { intelligence = result });
    }

    // ✅ Returns { prediction: "..." } — matches frontend aiApi.getCaseType()
    [HttpPost("casetype")]
    public async Task<IActionResult> CaseType([FromBody] CaseTypeRequestDto request, CancellationToken cancellationToken)
    {
        var result = await aiService.ClassifyCaseTypeAsync(request, cancellationToken);
        return Ok(new { prediction = result });
    }

    // ✅ Returns { draft: "..." } — matches frontend aiApi.getDraft()
    [HttpPost("draft/{caseId:guid}")]
    public async Task<IActionResult> Draft(Guid caseId, [FromBody] DraftRequestDto request, CancellationToken cancellationToken)
    {
        var result = await aiService.DraftDocumentAsync(caseId, request, cancellationToken);
        return Ok(new { draft = result });
    }

    [HttpPost("upload-context/{caseId:guid}")]
    public async Task UploadContext(Guid caseId, [FromForm] IFormFile file, [FromServices] IDocumentProcessor documentProcessor, [FromServices] Clausio.Legal.Infrastructure.ClausioDbContext db, CancellationToken cancellationToken)
    {
        Response.ContentType = "text/event-stream";
        var channel = Channel.CreateUnbounded<string>();

        var _ = Task.Run(async () => 
        {
            try 
            {
                var progress = new Progress<string>(msg => channel.Writer.TryWrite(msg));
                using var stream = file.OpenReadStream();
                var doc = await documentProcessor.ProcessDocumentAsync(caseId, stream, file.FileName, file.ContentType, progress, cancellationToken);
                
                db.Documents.Add(doc);
                await db.SaveChangesAsync(cancellationToken);
                
                channel.Writer.TryWrite($"SUCCESS: I've successfully processed '{doc.FileName}'. You can now ask me anything about this document.");
            }
            catch (Exception ex)
            {
                channel.Writer.TryWrite($"ERROR: {ex.Message}");
            }
            finally
            {
                channel.Writer.Complete();
            }
        }, cancellationToken);

        await foreach (var chunk in channel.Reader.ReadAllAsync(cancellationToken))
        {
            var data = $"data: {chunk}\n\n";
            await Response.WriteAsync(data, cancellationToken);
            await Response.Body.FlushAsync(cancellationToken);
        }
    }
}
