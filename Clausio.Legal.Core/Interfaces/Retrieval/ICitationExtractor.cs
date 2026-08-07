using System.Collections.Generic;

namespace Clausio.Legal.Core.Interfaces.Retrieval;

public interface ICitationExtractor
{
    List<string> ExtractCitations(string text);
}
