using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;
using Netpips.API.Core.Settings;

namespace Netpips.API.Core.Service;

public class GmailSmtpClient : ISmtpService
{
    private readonly SmtpClient _client;

    private readonly ILogger<GmailSmtpClient> _logger;

    private readonly MailAddress _netpipsAddress;

    public GmailSmtpClient(ILogger<GmailSmtpClient> logger, IOptions<GmailMailerAccountSettings> options)
    {
        _logger = logger;
        _netpipsAddress = new MailAddress(options.Value.Username, "Netpips");
        _client = new SmtpClient
        {
            Port = 587,
            Host = "smtp.gmail.com",
            UseDefaultCredentials = false,
            EnableSsl = true,
            DeliveryMethod = SmtpDeliveryMethod.Network,
            Credentials = new NetworkCredential(_netpipsAddress.Address, options.Value.Password),
        };
    }

    public void Send(MailMessage email)
    {
        email.From = _netpipsAddress;
        try
        {
            _logger.LogInformation("[Send] Sending");
            _client.Send(email);
            _logger.LogInformation("[Send] Success");
        }
        catch (Exception ex)
        {
            _logger.LogWarning("[Send] Faileure");
            _logger.LogWarning(ex.Message);
        }
    }
}