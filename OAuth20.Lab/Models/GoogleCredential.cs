using OAuth20.Lab.Models.Interfaces;

namespace OAuth20.Lab.Models
{
    public class GoogleCredential : IOAuth20Credential, IRedirectUri
    {
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }
        public string RedirectUri { get; set; }
    }
}