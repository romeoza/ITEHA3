using System.Threading.Channels;
using Vulindlela.Clearance.Api.Data.Entities;

namespace Vulindlela.Clearance.Api.Business;

public interface ISubmissionQueue
{
    ValueTask EnqueueAsync(DocumentSubmission submission, CancellationToken ct);
    IAsyncEnumerable<DocumentSubmission> ReadAllAsync(CancellationToken ct);
}

public class ChannelSubmissionQueue : ISubmissionQueue
{
    private readonly Channel<DocumentSubmission> _channel =
        Channel.CreateUnbounded<DocumentSubmission>(new UnboundedChannelOptions { SingleReader = true });

    public ValueTask EnqueueAsync(DocumentSubmission submission, CancellationToken ct) =>
        _channel.Writer.WriteAsync(submission, ct);

    public IAsyncEnumerable<DocumentSubmission> ReadAllAsync(CancellationToken ct) =>
        _channel.Reader.ReadAllAsync(ct);
}