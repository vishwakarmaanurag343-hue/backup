using System.Collections.Generic;

namespace Clausio.Legal.Core.Interfaces.AI;

public interface IPromptBuilder
{
    string BuildSystemPrompt(string templateName, Dictionary<string, string>? variables = null);
    string BuildUserPrompt(string templateName, string userRequest = "", Dictionary<string, string>? variables = null);
    string GetTemplateVersion(string templateName);
}
