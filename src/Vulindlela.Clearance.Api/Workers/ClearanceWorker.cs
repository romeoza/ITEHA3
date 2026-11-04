using Vulindlela.Clearance.Api.Business;
using Vulindlela.Clearance.Api.Data;

namespace Vulindlela.Clearance.Api.Workers;

public class ClearanceWorker : BackgroundService
{
    private readonly ISubmissionQueue _queue;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ClearanceWorker> _log;

    public ClearanceWorker(ISubmissionQueue queue, IServiceScopeFactory scopeFactory, ILogger<ClearanceWorker> log)
    { _queue = queue; _scopeFactory = scopeFactory; _log = log; }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var submission in _queue.ReadAllAsync(stoppingToken))
        {
            using var scope = _scopeFactory.CreateScope();
            var repo = scope.ServiceProvider.GetRequiredService<ISubmissionRepository>();
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(3), stoppingToken);
                submission.Status = string.IsNullOrEmpty(submission.MissingDocumentsCsv)
                    ? "Complete" : "CompleteWithMissing";
                await repo.UpdateAsync(submission, stoppingToken);
                await repo.SaveAsync(stoppingToken);
            }
            catch (OperationCanceledException) { /* shutdown */ }
            catch (Exception ex) { _log.LogError(ex, "Worker failed for submission {Id}", submission.Id); }
        }
    }
}