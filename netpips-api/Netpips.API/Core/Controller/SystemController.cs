using Humanizer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Netpips.API.Download.Model;

namespace Netpips.API.Core.Controller
{
    [Route("api/_sytem")]
    [ApiController]
    [AllowAnonymous]
    public class SystemController : ControllerBase
    {
        private readonly IDownloadItemRepository repository;

        public SystemController(IDownloadItemRepository repository)
        {
            this.repository = repository;
        }

        [HttpGet("ping", Name = "Ping")]
        [ProducesResponseType(200)]
        public string Ping() => "Pong";

        [HttpGet("status", Name = "Status")]
        [ProducesResponseType(200)]
        public ObjectResult Status()
        {
            var now = DateTime.UtcNow;
            var lastBuildAt = System.IO.File.GetLastWriteTimeUtc(GetType().Assembly.Location);
            var elapsed = now.AddMilliseconds(-now.Subtract(lastBuildAt).TotalMilliseconds);

            var canRedeploy = !repository.HasPendingDownloads();

            return Ok(new
            {
                Date = DateTime.Now,
                Ip = HttpContext.Connection.RemoteIpAddress.ToString(),
                Build = new
                {
                    Timestamp = lastBuildAt,
                    Elapsed = elapsed.Humanize()
                },
                CanRedeploy = canRedeploy
            });
        }
    }
}