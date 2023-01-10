using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace AddToMyQueue_Backend
{
    public class SpotifyClient
    {
        //https://api.spotify.com/v1 base url..
        private readonly HttpClient _httpClient;
        private readonly SpotifyApiData _apiData;
        private string _state;
        private string? _refreshToken;
        private string? _accessToken;

        public SpotifyClient(SpotifyApiData apiData) 
        { 
            _httpClient = new HttpClient();
            _apiData = apiData;

            _state = GenerateState();
        }

        public SpotifyClient(HttpClient httpClient, SpotifyApiData apiData)
        {
            _httpClient = httpClient;
            _apiData = apiData;

            _state = GenerateState();
        }

        private static string GenerateState()
        {
            string allowedChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            int length = 20;

            const int byteSize = 0x100;
            var allowedCharSet = new HashSet<char>(allowedChars).ToArray();
            if (byteSize < allowedCharSet.Length) throw new ArgumentException(String.Format("allowedChars may contain no more than {0} characters.", byteSize));

            using var rng = RandomNumberGenerator.Create();
            var result = new StringBuilder();
            var buf = new byte[128];
            while (result.Length < length)
            {
                rng.GetBytes(buf);
                for (var i = 0; i < buf.Length && result.Length < length; ++i)
                {
                    // Divide the byte into allowedCharSet-sized groups. If the
                    // random value falls into the last group and the last group is
                    // too small to choose from the entire allowedCharSet, ignore
                    // the value in order to avoid biasing the result.
                    var outOfRangeStart = byteSize - (byteSize % allowedCharSet.Length);
                    if (outOfRangeStart <= buf[i]) continue;
                    result.Append(allowedCharSet[buf[i] % allowedCharSet.Length]);
                }
            }
            return result.ToString();
        }

        public string GetAuthUri()
        {
            var uri = _apiData.AuthBaseUrl + $"/authorize?client_id={_apiData.ClientId}&response_type=code&redirect_uri={_apiData.RedirectUrl}";
            uri += !string.IsNullOrEmpty(_state) ? "&state=" + _state : "";
            uri += !string.IsNullOrEmpty(_apiData.Scopes) ? "&scope=" + _apiData.Scopes : "";
            uri += _apiData.ShowDialog ? "&show_dialog=" + _apiData.ShowDialog : "";
            return uri;
        }

        public bool ConfirmState(string receivedState)
        {
            return _state == receivedState;
        }

        public async Task GetAccessToken(string code)
        {
            var postContent = new FormUrlEncodedContent(new Dictionary<string, string>()
            {
                ["code"] = code,
                ["redirect_uri"] = _apiData.RedirectUrl,
                ["grant_type"] = "authorization_code"
            });

            var anonObj = new
            {
                access_token = "",
                token_type = "",
                expires_in = "",
                refresh_token = "",
                scope = ""
            };

            var responseJsonString = await PostToken(postContent);
            var obj = JsonConvert.DeserializeAnonymousType(responseJsonString, anonObj);

            _accessToken = obj?.access_token;
            _refreshToken = obj?.refresh_token;
        }

        public async Task RefreshAccessToken()
        {
            if (_refreshToken == null)
                return;

            var postContent = new FormUrlEncodedContent(new Dictionary<string, string>()
            {
                ["refresh_token"] = _refreshToken,
                ["grant_type"] = "refresh_token"
            });

            var anonObj = new
            {
                access_token = "",
                token_type = "",
                expires_in = "",
                scope = ""
            };

            var responseJsonString = await PostToken(postContent);
            var obj = JsonConvert.DeserializeAnonymousType(responseJsonString, anonObj);

            _accessToken = obj?.access_token;
        }

        private async Task<string> PostToken(FormUrlEncodedContent postContent)
        {
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", _apiData.AuthHeader);

            string url = _apiData.AuthBaseUrl + "/api/token";

            var response = await _httpClient.PostAsync(url, postContent);
            return await response.Content.ReadAsStringAsync();
        }

        public async Task GetDetails()
        {
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", _apiData.AuthHeader);
            string url = _apiData.ApiBaseUrl + "/me";

            var response = await _httpClient.GetAsync(url);
        }

        public async Task<string> TrackSearch(string searchTerms)
        {
            return "";
        }

        public async Task<bool> AddSongToPlaybackQueue(string trackUri)
        {
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);
            string url = _apiData.ApiBaseUrl + "/me/player/queue";

            var param = $"?uri={trackUri}";

            // Content is only used to allow PostAsync function to be used. The value of content does not matter.
            var content =  new StringContent(param, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(url + param, content);
            return response.IsSuccessStatusCode;
        }
    }
}
