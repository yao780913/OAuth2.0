using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using OAuth20.Lab.Models;
using OAuth20.Lab.Models.ViewModels;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace OAuth20.Lab.Controllers
{
    public class GithubController : Controller
    {
        private const string AUTHORIZE_URI = "https://github.com/login/oauth/authorize";

        private readonly IHttpClientFactory _httpclientfactory;
        private readonly string _clientId;
        private readonly string _clientSecret;
        private readonly string _redirectUri;

        public GithubController(IHttpClientFactory httpclientfactory, IOptions<GithubCredential> options)
        {
            _httpclientfactory = httpclientfactory;

            var credential = options.Value;

            _clientId = credential.ClientId;
            _clientSecret = credential.ClientSecret;
            _redirectUri = credential.RedirectUri;
        }

        public IActionResult Authorize()
        {
            var param = new Dictionary<string, string>
            {
                { "client_id", _clientId },
                { "redirect_uri", _redirectUri },
                { "scope", "user,repo,gist" },
                { "state", "123456" }
            };

            var requestUri = QueryHelpers.AddQueryString(AUTHORIZE_URI, param);

            return Redirect(requestUri);
        }

        public async Task<IActionResult> Callback(string code, string state)
        {
            var uri = "https://github.com/login/oauth/access_token";

            var param = new Dictionary<string, string>
            {
                ["client_id"] = _clientId,
                ["client_secret"] = _clientSecret,
                ["code"] = code,
                ["state"] = state
            };

            using var httpClient = _httpclientfactory.CreateClient();
            httpClient.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

            var response = await httpClient.PostAsync(
                uri,
                new FormUrlEncodedContent(param));

            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException($"{response.StatusCode}, {responseContent}");
            }

            string accessToken = Convert.ToString(
                JsonConvert.DeserializeObject<dynamic>(responseContent).access_token);

            if (string.IsNullOrWhiteSpace(accessToken))
            {
                throw new ArgumentNullException("accessToken is empty");
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
            var uri = "https://api.github.com/user";

            using var httpClient = _httpclientfactory.CreateClient();

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