using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using OAuth20.Lab.Models;
using OAuth20.Lab.Models.ViewModels;
using System.Net.Http.Headers;
using OAuth20.Common.Model;
using OAuth20.Common.Models;


namespace OAuth20.Lab.Controllers
{
    public class GithubController : Controller
    {
        private const string AuthorizeUri = "https://github.com/login/oauth/authorize";

        private readonly IHttpClientFactory _httpClientFactory;
        private readonly string _clientId;
        private readonly string _clientSecret;
        private readonly string _redirectUri;

        public GithubController(IHttpClientFactory httpClientFactory, IOptions<GithubCredential> options)
        {
            _httpClientFactory = httpClientFactory;

            var credential = options.Value;

            _clientId = credential.ClientId;
            _clientSecret = credential.ClientSecret;
            _redirectUri = credential.RedirectUri;
        }

        public IActionResult Authorize()
        {
            var scopes = new List<string>
            {
                "user","repo","gist"
            };
            
            var param = new Dictionary<string, string>
            {
                { "client_id", _clientId },
                { "redirect_uri", _redirectUri },
                { "scope", string.Join(',', scopes) },
                { "state", "123456" }
            };

            var requestUri = QueryHelpers.AddQueryString(AuthorizeUri, param);

            return Redirect(requestUri);
        }

        public async Task<IActionResult> Callback(string code, string state)
        {
            const string uri = "https://github.com/login/oauth/access_token";

            var param = new Dictionary<string, string>
            {
                ["client_id"] = _clientId,
                ["client_secret"] = _clientSecret,
                ["code"] = code,
                ["state"] = state
            };

            using var httpClient = _httpClientFactory.CreateClient();
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var response = await httpClient.PostAsync(
                uri,
                new FormUrlEncodedContent(param));

            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException($"{response.StatusCode}, {responseContent}");
            }

            string accessToken = Convert.ToString(
                JsonConvert.DeserializeObject<dynamic>(responseContent)!.access_token);

            if (string.IsNullOrWhiteSpace(accessToken))
            {
                throw new ArgumentNullException($"accessToken is empty");
            }

            HttpContext.Response.Cookies.Append(CookieNames.GithubAccessToken, accessToken);

            return RedirectToAction("Index", "Home");
        }

        /// <summary>
        /// Get the authenticated user
        /// </summary>
        /// <returns></returns>
        public async Task<IActionResult> UserData()
        {
            const string uri = "https://api.github.com/user";

            using var httpClient = _httpClientFactory.CreateClient();

            if (!HttpContext.Request.Cookies.TryGetValue(CookieNames.GithubAccessToken, out var accessToken))
            {
                return RedirectToAction("Authorize");
            }
            httpClient.DefaultRequestHeaders.Add("User-Agent", "Awesome-Octocat-App");
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("token", accessToken);

            var response = await httpClient.GetAsync(uri);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException($"{response.StatusCode}, {responseContent}");
            }

            return View(JsonConvert.DeserializeObject<GithubUserViewModel>(responseContent));
        }
    }
}