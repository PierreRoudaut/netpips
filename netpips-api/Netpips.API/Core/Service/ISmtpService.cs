
using System.Net.Mail;

namespace Netpips.API.Core.Service;

public interface ISmtpService
{
    void Send(MailMessage email);
}