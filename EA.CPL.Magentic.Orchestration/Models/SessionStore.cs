using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace EA.CPL.Magentic.Orchestration.Models
{
    public sealed class SessionStore
    {
        [JsonPropertyName("conversations")]
        public Dictionary<string, ConversationSession> Conversations { get; set; } =
            new(StringComparer.OrdinalIgnoreCase);
    }
}
