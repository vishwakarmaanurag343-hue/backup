using System.Collections.Generic;
using Clausio.Legal.Core.Entities;

namespace Clausio.Legal.Core.Interfaces.Retrieval;

public interface ICitationVerifier
{
    bool VerifyCitation(string generatedText, List<DocumentChunk> retrievedContext);
}
