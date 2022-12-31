using System.ComponentModel;

namespace QMailSender.Models;

public class SmtpSettings
{
    [DefaultValue("mx2.qtechnics.net")]
    public string SmtpServer { get; set; }
    [DefaultValue(465)]
    public int SmtpPort { get; set; }
    public string Username { get; set; }
    public string Password { get; set; }
    public bool UseSSL { get; set; }
    public bool UseStartTls { get; set; }
}