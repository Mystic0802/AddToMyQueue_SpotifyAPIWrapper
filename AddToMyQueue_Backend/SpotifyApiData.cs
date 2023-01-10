using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace AddToMyQueue_Backend
{
    public class SpotifyApiData
    {
        public string ApiBaseUrl { get; }
        public string AuthBaseUrl { get; }
        public string ClientId { get; }
        public string ClientSecret { get; }
        public string Scopes { get; }
        public string RedirectUrl { get; }
        public bool ShowDialog { get; }
        public string AuthHeader { get; }

        public SpotifyApiData(
            string apiBaseUrl,
            string authBaseUrl,
            string clientId,
            string clientSecret,
            string scopes,
            string redirectUrl,
            bool showDialog = true
            )
        {
            ApiBaseUrl = apiBaseUrl;
            AuthBaseUrl = authBaseUrl;
            ClientId = clientId;
            ClientSecret = clientSecret;
            Scopes = scopes;
            RedirectUrl = redirectUrl;
            ShowDialog = showDialog;
            AuthHeader = Convert.ToBase64String(Encoding.UTF8.GetBytes(ClientId + ":" + ClientSecret));
        }


    }
}
