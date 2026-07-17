using EA.CPL.Magentic.Orchestration.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace EA.CPL.Magentic.Orchestration.Abstractions
{
    public interface IServiceBusPublisher
    {
        Task SendMessage(JobMessage message, CancellationToken ct = default);
    }
}
