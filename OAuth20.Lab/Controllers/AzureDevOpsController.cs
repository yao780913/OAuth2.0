using System.Web;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using OAuth20.Common.Models;
using OAuth20.Lab.Models;

namespace OAuth20.Lab.Controllers;

public class AzureDevOpsController : Controller
{
    private readonly string _redirectUri;

    private readonly string _clientId;
    private readonly string _clientSecret;
    private readonly IHttpClientFactory _httpClientFactory;

    public AzureDevOpsController (IOptions<AzureDevOpsCredential> options, IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
        _clientId = options.Value.ClientId;
        _clientSecret = options.Value.ClientSecret;
        _redirectUri = options.Value.RedirectUri;
    }

    public IActionResult Authorize ()
    {
        // https://learn.microsoft.com/en-us/azure/devops/integrate/get-started/authentication/oauth?view=azure-devops#register-your-app
        var uri = "https://app.vssps.visualstudio.com/oauth2/authorize";
        var param = new Dictionary<string, string>
        {
            { "response_type", "Assertion" },
            { "client_id", _clientId },
            { "redirect_uri", _redirectUri },
            {
                "scope",
                "vso.code_manage vso.project_manage vso.release_manage vso.serviceendpoint_manage vso.taskgroups_manage vso.variablegroups_manage"
            }
        };

        var requestUri = QueryHelpers.AddQueryString(uri, param);

        return Redirect(requestUri);
    }

    public async Task<IActionResult> Callback ([FromQuery] string code, [FromQuery] string state)
    {
        var uri = "https://app.vssps.visualstudio.com/oauth2/token";
        var param = new Dictionary<string, string>
        {
            { "client_assertion_type", "urn:ietf:params:oauth:client-assertion-type:jwt-bearer" },
            { "grant_type", "urn:ietf:params:oauth:grant-type:jwt-bearer" },
            { "client_assertion", HttpUtility.UrlEncode(_clientSecret) },
            { "assertion", HttpUtility.UrlEncode(code) },
            { "redirect_uri", _redirectUri },
            { "state", state }
        };

        using var httpClient = _httpClientFactory.CreateClient();
        var requestContent = new FormUrlEncodedContent(param);

        var response = await httpClient.PostAsync(uri, requestContent);

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            throw new Exception(errorContent);
        }

        var content = await response.Content.ReadAsStringAsync();
        var json = JObject.Parse(content);
        var accessToken = json["access_token"].Value<string>();
        var refreshToken = json["refresh_token"].Value<string>();
        var expiresIn = json["expires_in"].Value<int>();
        var tokenType = json["token_type"].Value<string>();
        
        HttpContext.Response.Cookies.Append(CookieNames.AzureDevopsAccessToken, accessToken);
        
        return RedirectToAction("Index", "Home");
    }
    
    
}