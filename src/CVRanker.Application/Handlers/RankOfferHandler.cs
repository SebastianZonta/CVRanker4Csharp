using CVRanker.Application.Mappers;
using CVRanker.Contracts.Requests.Rankings;
using CVRanker.Contracts.Responses.Rankings;
using CVRanker.Domain;

namespace CVRanker.Application.Handlers;

/// <summary>Rank-once flow: extract PDFs by ref, freeze the snapshot. No auto re-rank.</summary>
public sealed class RankOfferHandler(
    IRanker ranker,
    ISnapshotStore store,
    IPdfTextExtractor extractor,
    IPdfStore pdfs,
    string scorerVersion = Bm25Ranker.ScorerVersion)
{
    public RankResponse Handle(RankRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var cvs = new List<CandidateCv>(request.Cvs.Count);
        foreach (var entry in request.Cvs)
        {
            byte[] pdf;
            try
            {
                pdf = pdfs.GetPdf(entry.Ref);
            }
            catch (FileNotFoundException e)
            {
                throw new CandidatePdfNotFoundException(entry.Ref, e.FileName);
            }
            cvs.Add(entry.ToCandidate(extractor.Extract(pdf).Text));
        }
        var offer = request.ToOffer();
        var frozenCvs = cvs.ToList();
        var results = ranker.Rank(offer, frozenCvs).ToList();

        var snapshot = RankingSnapshot.Create(offer, frozenCvs, results, scorerVersion);
        store.Save(snapshot);
        return new RankResponse(snapshot.Id);
    }
}

public sealed class CandidatePdfNotFoundException(string cvRef, string? path)
    : FileNotFoundException($"PDF not found for candidate '{cvRef}': {path}. Check PdfDirectory.", path)
{
    public string CvRef { get; } = cvRef;
}
