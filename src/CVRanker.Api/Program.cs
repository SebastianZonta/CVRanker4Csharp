using CVRanker.Domain;
using CVRanker;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<IRanker, Bm25Ranker>();
builder.Services.AddSingleton<ISnapshotStore, InMemorySnapshotStore>();
builder.Services.AddSingleton<IPdfTextExtractor, PdfPigTextExtractor>();
builder.Services.AddSingleton<IPdfStore>(sp =>
    new DirectoryPdfStore(sp.GetRequiredService<IConfiguration>()["PdfDirectory"] ?? "pdfs"));

var app = builder.Build();

// Rankear button: extract PDFs by ref, rank once, freeze the snapshot.
app.MapPost("/rankings", (
    RankRequest request,
    IRanker ranker,
    ISnapshotStore store,
    IPdfTextExtractor extractor,
    IPdfStore pdfs) =>
{
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
            return Results.BadRequest($"PDF not found for candidate '{entry.Ref}': {e.FileName}. Check PdfDirectory.");
        }
        var text = extractor.Extract(pdf).Text;
        cvs.Add(new CandidateCv(entry.Ref, text, entry.ExperienceYears,
            entry.HasDegree, entry.IsSenior, entry.HasLanguage));
    }

    var offer = new Offer(request.JobDescription, request.MustHave, request.NiceToHave, request.Weights);
    var snapshot = new RankingService(ranker, store).Rank(offer, cvs);
    return Results.Ok(new RankResponse(snapshot.Id));
});

// Frozen ranking view with HR filters.
app.MapGet("/rankings/{id}", (
    string id,
    ISnapshotStore store,
    bool? mustComplete,
    string? q) =>
{
    var snapshot = RequireSnapshot(id, store);
    return snapshot is null
        ? Results.NotFound()
        : Results.Ok(RankingView.FromSnapshot(snapshot, mustComplete ?? false, q));
});

// Original PDF behind the blind-phase banner.
app.MapGet("/rankings/{id}/cvs/{cvRef}/pdf", (
    string id,
    string cvRef,
    ISnapshotStore store,
    IPdfStore pdfs,
    HttpResponse response) =>
{
    if (RequireSnapshot(id, store) is null)
        return Results.NotFound();
    byte[] bytes;
    try
    {
        bytes = pdfs.GetPdf(cvRef);
    }
    catch (FileNotFoundException)
    {
        return Results.NotFound();
    }
    response.Headers.Append("X-Blind-Phase", RankingView.BlindPhaseBannerText);
    return Results.Bytes(bytes, "application/pdf");
}).WithName("GetCvPdf");

app.Run();

static RankingSnapshot? RequireSnapshot(string id, ISnapshotStore store)
{
    try
    {
        return store.Get(id);
    }
    catch (KeyNotFoundException)
    {
        return null;
    }
}

public sealed record CvEntry(
    string Ref,
    double ExperienceYears,
    bool HasDegree,
    bool IsSenior,
    bool HasLanguage);

public sealed record RankRequest(
    string JobDescription,
    IReadOnlyList<string> MustHave,
    IReadOnlyList<string> NiceToHave,
    ScoringWeights? Weights,
    IReadOnlyList<CvEntry> Cvs);

public sealed record RankResponse(string SnapshotId);

public partial class Program;
