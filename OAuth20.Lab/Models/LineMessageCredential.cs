using OAuth20.Lab.Models.Interfaces;

namespace OAuth20.Lab.Models
{
    public class LineMessageCredential : IOAuth20Credential
    {
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }
        public string MyUserId { get; set; }
    }
}