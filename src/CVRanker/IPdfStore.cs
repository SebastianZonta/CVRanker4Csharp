using CVRanker.Domain;
namespace CVRanker;

/// <summary>Pilot PDF access by path (blob Azure in production). Serves the original PDF behind the blind-phase banner.</summary>
public interface IPdfStore
{
    byte[] GetPdf(string cvRef);
}
