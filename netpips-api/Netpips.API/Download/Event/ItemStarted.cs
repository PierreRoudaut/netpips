using Coravel.Events.Interfaces;
using Netpips.API.Download.Model;

namespace Netpips.API.Download.Event;

public class ItemStarted : IEvent
{
    public DownloadItem? Item { get; set; }
    public ItemStarted(DownloadItem? item)
    {
        Item = item;
    }
}