using EA.CPL.Magentic.Orchestration.Abstractions;
using EA.CPL.Magentic.Orchestration.Models;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Agents.AI.Workflows.Checkpointing;
using Microsoft.Extensions.AI;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace EA.CPL.Magentic.Orchestration.Services
{
    public class ConversationService : IConversationService
    {
        private const string LegacyConversationId = "legacy";

        public SessionStore? CreateLegacySessionStore(string json)
        {
            try
            {
                List<ConversationRecord>? records = JsonSerializer.Deserialize<List<ConversationRecord>>(json, JsonOptions);
                if (records is null) return null;
                return new SessionStore
                {
                    Conversations = new Dictionary<string, ConversationSession>(StringComparer.OrdinalIgnoreCase)
                    {
                        [LegacyConversationId] = new ConversationSession
                        {
                            ConversationId = LegacyConversationId,
                            Messages = records
                        }
                    }
                };
            }
            catch (JsonException) { return null; }
        }

        private static JsonSerializerOptions JsonOptions { get; } = new()
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        public string GetConversationSessionFilePath(string conversationId)
        {
            string dirPath = Path.Combine(AppContext.BaseDirectory, "sessions");
            Directory.CreateDirectory(dirPath);
            string safe = string.Concat(conversationId.Select(c =>
                Path.GetInvalidFileNameChars().Contains(c) ? '_' : c));
            return Path.Combine(dirPath, safe + ".json");
        }

        public async ValueTask<CheckpointInfo?> GetLatestCheckpointAsync(FileSystemJsonCheckpointStore store, string conversationId)
        {
            CheckpointInfo? latest = null;
            foreach (CheckpointInfo info in await store.RetrieveIndexAsync(conversationId))
                latest = info;
            return latest;
        }

        public ConversationSessionState LoadConversationSession(string conversationId)
        {
            string filePath = GetConversationSessionFilePath(conversationId);
            if (File.Exists(filePath))
            {
                try
                {
                    string json = File.ReadAllText(filePath);
                    ConversationSession? session = JsonSerializer.Deserialize<ConversationSession>(json, JsonOptions);
                    if (session is null) return new ConversationSessionState();
                    return new ConversationSessionState
                    {
                        WorkflowStage = session.WorkflowStage,
                        LatestPlanText = session.LatestPlanText,
                        Messages = session.Messages.Select(ToChatMessage).ToList()
                    };
                }
                catch { return new ConversationSessionState(); }
            }

            try
            {
                string legacyFilePath = Path.Combine(AppContext.BaseDirectory, "magentic-session-history.json");
                if (!File.Exists(legacyFilePath)) return new ConversationSessionState();
                string json = File.ReadAllText(legacyFilePath);
                SessionStore? store = TryLoadSessionStore(json);
                if (store?.Conversations is null || !store.Conversations.TryGetValue(conversationId, out ConversationSession? session))
                    return new ConversationSessionState();
                ConversationSessionState migrated = new()
                {
                    WorkflowStage = session.WorkflowStage,
                    LatestPlanText = session.LatestPlanText,
                    Messages = session.Messages.Select(ToChatMessage).ToList()
                };
                SaveConversationSession(conversationId, migrated);
                return migrated;
            }
            catch { return new ConversationSessionState(); }
        }

        public void SaveConversationSession(string conversationId, ConversationSessionState state)
        {
            string dirPath = Path.Combine(AppContext.BaseDirectory, "sessions");
            Directory.CreateDirectory(dirPath);
            ConversationSession session = new()
            {
                ConversationId = conversationId,
                WorkflowStage = state.WorkflowStage,
                LatestPlanText = state.LatestPlanText,
                Messages = state.Messages
                    .Select(m => new ConversationRecord(m.Role.ToString(), m.AuthorName, m.Text ?? string.Empty))
                    .ToList()
            };
            File.WriteAllText(GetConversationSessionFilePath(conversationId),
                JsonSerializer.Serialize(session, JsonOptions));
        }

        public SessionStore? TryLoadSessionStore(string json)
        {
            using JsonDocument document = JsonDocument.Parse(json);
            return document.RootElement.ValueKind switch
            {
                JsonValueKind.Object => JsonSerializer.Deserialize<SessionStore>(json, JsonOptions),
                JsonValueKind.Array => CreateLegacySessionStore(json),
                _ => null,
            };
        }

        private static ChatMessage ToChatMessage(ConversationRecord record) =>
     new(ParseRole(record.Role), record.Text) { AuthorName = record.AuthorName };

        private static ChatRole ParseRole(string role) =>
       string.Equals(role, nameof(ChatRole.User), StringComparison.OrdinalIgnoreCase) ? ChatRole.User :
       string.Equals(role, nameof(ChatRole.Assistant), StringComparison.OrdinalIgnoreCase) ? ChatRole.Assistant :
       string.Equals(role, nameof(ChatRole.System), StringComparison.OrdinalIgnoreCase) ? ChatRole.System :
       ChatRole.User;
    }
}
