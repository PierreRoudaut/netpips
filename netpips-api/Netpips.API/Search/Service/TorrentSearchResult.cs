using Netpips.API.Core.Http;
using Netpips.API.Search.Model;

namespace Netpips.API.Search.Service;

public class TorrentSearchResult
{
    public HttpResponseLite Response { get; set; }
    public List<TorrentSearchItem> Items  { get; set; }
    public bool Succeeded { get; set; }
}