namespace CVRanker.Application.Handlers;

/// <summary>Original PDF behind the blind phase: verifies the snapshot exists, then serves dumb bytes.</summary>
public sealed class GetCandidatePdfHandler(ISnapshotStore store, IPdfStore pdfs)
{
    public byte[] Handle(string snapshotId, string cvRef)
    {
        _ = store.Get(snapshotId);
        return pdfs.GetPdf(cvRef);
    }
}
