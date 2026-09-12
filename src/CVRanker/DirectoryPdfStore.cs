using CVRanker.Application;
using CVRanker.Domain;
namespace CVRanker;

public sealed class DirectoryPdfStore(string directory) : IPdfStore
{
    public byte[] GetPdf(string cvRef)
    {
        var path = Path.Combine(directory, cvRef + ".pdf");
        if (!File.Exists(path))
            throw new FileNotFoundException($"PDF not found for candidate: {cvRef}", path);
        return File.ReadAllBytes(path);
    }
}
