
using IdentityModel.Client;
using System.Net.Http;
using System.Net.Http.Headers;

namespace GloboTicket.Gateway.DelegatingHandlers
{
    public class TokenExchangeDelegate : DelegatingHandler
    {
        private readonly HttpClient _httpClient;

        public TokenExchangeDelegate(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            // extract access token from header
            var accessToken = request.Headers.Authorization!.Parameter;

            if (string.IsNullOrWhiteSpace(accessToken))
            {
                throw new Exception("Could not find bearer access token in headers");
            }

            var newToken = await ExchangeToken(accessToken);


            // replace
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", newToken);


            // return the token
            return await _httpClient.SendAsync(request);
        }

        private async Task<string> ExchangeToken(string accessToken)
        {
            var disconveryDocument = await _httpClient.GetDiscoveryDocumentAsync("https://localhost:5010");

            // exchange 
            var parameters = new Parameters
            {
                { "subject_token",accessToken},
                { "subject_token_type","urn:ietf:params:oauth:grant-type:access-token"},
                { "scope","openid profile eventcatalog.fullaccess"}
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

            return tokenResponse.AccessToken;
        }
    }
}
