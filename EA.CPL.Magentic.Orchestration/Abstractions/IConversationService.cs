using EA.CPL.Magentic.Orchestration.Models;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Agents.AI.Workflows.Checkpointing;
using System;
using System.Collections.Generic;
using System.Text;

namespace EA.CPL.Magentic.Orchestration.Abstractions
{
    public interface IConversationService
    {
         ValueTask<CheckpointInfo?> GetLatestCheckpointAsync(FileSystemJsonCheckpointStore store, string conversationId);

        SessionStore? TryLoadSessionStore(string json);

        SessionStore? CreateLegacySessionStore(string json);

        ConversationSessionState LoadConversationSession(string conversationId);

        void SaveConversationSession(string conversationId, ConversationSessionState state);

        string GetConversationSessionFilePath(string conversationId);


    }
}
