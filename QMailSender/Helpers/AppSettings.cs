namespace QMailSender.Helpers;

public class AppSettings
{
    public string Secret { get; set; }

    // refresh token time to live (in days), inactive tokens are
    // automatically deleted from the database after this time
    public int RefreshTokenTTL { get; set; }
    public string DefaultUsername { get; set; }
    public string DefaultPassword { get; set; }
    public string DefaultFirstName { get; set; }
    public string DefaultLastName { get; set; }

}