namespace Gov.News.Media.Model
{
    public class Settings
    {
        public string AssetsCacheFolder { get; set; }

        public string[] AllowedMediaHosts { get; set; }

        public string[] AllowedContentType { get; set; }

        //Number of hours before retrying an invalid request with the external server
        public int InvalidRequestRetryHours { get; set; }

        public bool EnableRefererHeaderFilter { get; set; }

        public string[] RefererAllowedDomains { get; set; }
        public bool EnableCryptoMode { get; set; }
        public string[] PasswordKeys { get; set; }
        public string UserAgent { get; set; }
    }
}
