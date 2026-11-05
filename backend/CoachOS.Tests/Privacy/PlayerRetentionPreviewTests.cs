using CoachOS.Domain.Entities;
using CoachOS.Infrastructure.Persistence;
using CoachOS.Infrastructure.Privacy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using Assert = NUnit.Framework.Legacy.ClassicAssert;

namespace CoachOS.Tests.Privacy;

public class PlayerRetentionPreviewTests
{
    [Test]
    public async Task Disabled_worker_does_not_resolve_database_scope()
    {
        Moq.Mock<Microsoft.Extensions.DependencyInjection.IServiceScopeFactory> scopes = new(Moq.MockBehavior.Strict);
        using PlayerRetentionWorker worker = new(scopes.Object, Options.Create(new PlayerRetentionOptions()),
            NullLogger<PlayerRetentionWorker>.Instance);
        await worker.StartAsync(CancellationToken.None);
        await worker.StopAsync(CancellationToken.None);
        scopes.VerifyNoOtherCalls();
    }

}
