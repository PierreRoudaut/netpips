namespace Netpips.API.Media.Service;

public interface IArchiveExtractorService
{
    bool HandleRarFile(string fsInfoFullName, out string destDir);
}