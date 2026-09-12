using CVRanker.Application;
using CVRanker.Application.Handlers;
using CVRanker.Contracts.Requests.Rankings;
using CVRanker.Contracts.Responses.Rankings;
using CVRanker.Domain;
using CVRanker;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddApplication();
builder.Services.AddSingleton<ISnapshotStore, InMemorySnapshotStore>();
builder.Services.AddSingleton<IPdfTextExtractor, PdfPigTextExtractor>();
builder.Services.AddSingleton<IPdfStore>(sp =>
    new DirectoryPdfStore(sp.GetRequiredService<IConfiguration>()["PdfDirectory"] ?? "pdfs"));

var app = builder.Build();

// Rankear button: rank once, freeze the snapshot.
app.MapPost("/rankings", (RankRequest request, RankOfferHandler handler) =>
{
    try
    {
        return Results.Ok(handler.Handle(request));
    }
    catch (CandidatePdfNotFoundException e)
    {
        return Results.BadRequest(e.Message);
    }
});

// Frozen ranking view with HR filters.
app.MapGet("/rankings/{id}", (
    string id,
    GetRankingHandler handler,
    bool? mustComplete,
    string? q) =>
{
    try
    {
        return Results.Ok(handler.Handle(id, mustComplete ?? false, q));
    }
    catch (KeyNotFoundException)
    {
        return Results.NotFound();
    }
});

// Original PDF behind the blind-phase banner.
app.MapGet("/rankings/{id}/cvs/{cvRef}/pdf", (
    string id,
    string cvRef,
    GetCandidatePdfHandler handler,
    HttpResponse response) =>
{
    byte[] bytes;
    try
    {
        bytes = handler.Handle(id, cvRef);
    }
    catch (KeyNotFoundException)
    {
        return Results.NotFound();
    }
    catch (FileNotFoundException)
    {
        return Results.NotFound();
    }
    response.Headers.Append("X-Blind-Phase", RankingView.BlindPhaseBannerText);
    return Results.Bytes(bytes, "application/pdf");
}).WithName("GetCvPdf");

app.Run();

public partial class Program;
