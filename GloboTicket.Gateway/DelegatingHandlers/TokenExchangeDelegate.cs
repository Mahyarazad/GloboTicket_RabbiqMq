
using IdentityModel.AspNetCore.AccessTokenManagement;
using IdentityModel.Client;
using System.Net.Http;
using System.Net.Http.Headers;

namespace GloboTicket.Gateway.DelegatingHandlers
{
    public class TokenExchangeDelegate : DelegatingHandler
    {
        private readonly HttpClient _httpClient;
        private readonly IClientAccessTokenCache _clientAccessTokenCache;
        public TokenExchangeDelegate(HttpClient httpClient, IClientAccessTokenCache clientAccessTokenCache)
        {
            _httpClient = httpClient;
            _clientAccessTokenCache = clientAccessTokenCache;
        }

        private async Task<string> GetAccessToken(string incomingToken)
        {
            var token = await _clientAccessTokenCache.GetAsync("gatewaytodownstreamtokenexchnageclient_eventcatalog", default);
            if(token != null)
            {
                return token.AccessToken;
            }

            var (accessToken, expiresIn) = await ExchangeToken(incomingToken);

            await _clientAccessTokenCache.SetAsync("gatewaytodownstreamtokenexchnageclient_eventcatalog", accessToken, expiresIn, default);

            return accessToken;

        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            // extract access token from header
            var accessToken = request.Headers.Authorization!.Parameter;

            if (string.IsNullOrWhiteSpace(accessToken))
            {
                throw new Exception("Could not find bearer access token in headers");
            }


            // replace
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", await GetAccessToken(accessToken));


            // return the token
            return await base.SendAsync(request, cancellationToken);
        }

        private async Task<(string, int)> ExchangeToken(string accessToken)
        {
            var disconveryDocument = await _httpClient.GetDiscoveryDocumentAsync("https://localhost:5010");

            // exchange 
            var parameters = new Parameters
            {
                { "subject_token", accessToken},
                { "subject_token_type","urn:ietf:params:oauth:grant-type:access-token"},
                { "scope",$"openid profile eventcatalog.fullaccess"}
            };

            var tokenResponse = await _httpClient.RequestTokenAsync(new TokenRequest
            {
                Address = disconveryDocument.TokenEndpoint,
                ClientId = "globoticketgatewaydownstreamtokenexchangeclient",
                ClientSecret = "aed65b30-071f-4058-b42b-6ac0955ca3b9",
                GrantType = "urn:ietf:params:oauth:grant-type:token-exchange",
                Parameters = parameters
            });

            if (tokenResponse.IsError)
            {
                throw new Exception(tokenResponse.ErrorDescription);
            }

            return (tokenResponse.AccessToken, tokenResponse.ExpiresIn);
        }
    }
}
