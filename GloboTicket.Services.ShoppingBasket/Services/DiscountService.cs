using GloboTicket.Services.ShoppingBasket.Extensions;
using GloboTicket.Services.ShoppingBasket.Models;
using IdentityModel.Client;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace GloboTicket.Services.ShoppingBasket.Services
{
    public class DiscountService: IDiscountService
    {
        private readonly HttpClient client;
        private string _accessToken;
        private readonly IHttpContextAccessor _contextAccessor;
        public DiscountService(HttpClient client, IHttpContextAccessor httpContextAccessor)
        {
            this.client = client;
            _contextAccessor = httpContextAccessor;
        }


        private async Task<string> GetToken()
        {

            if (!string.IsNullOrEmpty(_accessToken))
            {
                return _accessToken;
            }

            var discoveryDocumentResponse = await client.GetDiscoveryDocumentAsync("https://localhost:5010");
            if (discoveryDocumentResponse.IsError)
            {
                throw new Exception(discoveryDocumentResponse.Exception.Message);
            }

            var customParameters = new Dictionary<string, string>()
            {
                { "subject_token_type" , "urn:ietf:params:oauth:grant-type:access-token"},
                { "subject_token" , await _contextAccessor.HttpContext.GetTokenAsync("access_token")},
                { "scope" , "openid profile discount.fullaccess"}
            };

            var tokenResponse = await client.RequestTokenAsync(new TokenRequest()
            {
                Address = discoveryDocumentResponse.TokenEndpoint,
                GrantType = "urn:ietf:params:oauth:grant-type:token-exchange",
                Parameters = customParameters,
                ClientSecret = "aed65b30-071f-4058-b42b-6ac0955ca3b9",
                ClientId = "shoppingbaskettodownstreamtokenexchangeclient"
            });

            if (tokenResponse.IsError)
            {
                throw new Exception(tokenResponse.Exception.Message);
            }

            _accessToken = tokenResponse.AccessToken;
            return _accessToken;
        }

        public async Task<Coupon> GetCoupon(Guid couponId)
        {
            client.SetBearerToken(await GetToken());
            var response = await client.GetAsync($"/api/discount/{couponId}");
            return await response.ReadContentAs<Coupon>();
        }

        public async Task<Coupon> GetCouponWithError(Guid couponId)
        {
            client.SetBearerToken(await GetToken());
            var response = await client.GetAsync($"/api/discount/error/{couponId}");
            return await response.ReadContentAs<Coupon>();
        }
    }
}
