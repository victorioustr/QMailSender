using System.ComponentModel;
using System.Text;

namespace QMailSender.Models;

public class SendRequest
{
    [DefaultValue("QTeknoloji")]
    public string FromName { get; set; }
    [DefaultValue("noreply@qt.net.tr")]
    public string FromAddress { get; set; }
    [DefaultValue("QTeknoloji")]
    public string ReplyToName { get; set; }
    [DefaultValue("noreply@qt.net.tr")]
    public string ReplyToAddress { get; set; }
    public IEnumerable<string> TargetAddresses { get; set; }
    public string Subject { get; set; }
    public string Base64Body { get; set; }
    public string UnSubscribeUrl { get; set; }
    public SmtpSettings SmtpSettings { get; set; }
    [DefaultValue(2000)]
    public int SendDelay { get; set; } = 2000;
}