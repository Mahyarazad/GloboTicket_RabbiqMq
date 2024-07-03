﻿using GloboTicket.Web.Extensions;
using GloboTicket.Web.Models;
using GloboTicket.Web.Models.Api;
using IdentityModel.Client;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace GloboTicket.Web.Services
{
    public class ShoppingBasketService : IShoppingBasketService
    {
        private readonly HttpClient client;
        private readonly Settings settings;
        private readonly IHttpContextAccessor httpContextAccessor;

        public ShoppingBasketService(HttpClient client, Settings settings, IHttpContextAccessor accessor)
        {
            this.client = client;
            this.settings = settings;
            this.httpContextAccessor = accessor;
        }

        public async Task<BasketLine> AddToBasket(Guid basketId, BasketLineForCreation basketLine)
        {
            //if (basketId == Guid.Empty)
            //{
            //    string UserID = "";
            //    var accessToken = await httpContextAccessor.HttpContext.GetTokenAsync("access_token");
            //    var discoveryDocumentResponse = await client.GetDiscoveryDocumentAsync("https://localhost:5010");
            //    if (discoveryDocumentResponse.IsError)
            //    {
            //        throw new Exception(discoveryDocumentResponse.Error);
            //    }

            //    if (httpContextAccessor.HttpContext.User.Claims.FirstOrDefault(x => x.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier").Value is null)
            //    {
            //    }

            //    var userResponse = await client.GetUserInfoAsync(new UserInfoRequest()
            //    {
            //        Address = discoveryDocumentResponse.UserInfoEndpoint,
            //        Token = accessToken,
            //    });
            //    UserID = userResponse.Claims.FirstOrDefault(c => c.Type == "sub").Value;


            //    client.SetBearerToken(accessToken);
            //    var basketResponse = await client.PostAsJson("/api/baskets", new BasketForCreation { UserId = Guid.Parse(UserID) });
            //    var basket = await basketResponse.ReadContentAs<Basket>();
            //    basketId = basket.BasketId;
            //}

            //client.SetBearerToken(await httpContextAccessor.HttpContext.GetTokenAsync("access_token"));
            //var response = await client.PostAsJson($"api/baskets/{basketId}/basketlines", basketLine);
            //return await response.ReadContentAs<BasketLine>();

            var token = await httpContextAccessor.HttpContext.GetTokenAsync("access_token");

            if (basketId == Guid.Empty)
            {
                client.SetBearerToken(token);
                var basketResponse = await client.PostAsJson("/api/baskets",
                    new BasketForCreation
                    {
                        UserId =
                    Guid.Parse(
                        httpContextAccessor.HttpContext
                        .User.Claims.FirstOrDefault(c => c.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value)
                    });
                var basket = await basketResponse.ReadContentAs<Basket>();
                basketId = basket.BasketId;
            }

            client.SetBearerToken(token);
            var response = await client.PostAsJson($"api/baskets/{basketId}/basketlines", basketLine);
            return await response.ReadContentAs<BasketLine>();
        }

        public async Task<Basket> GetBasket(Guid basketId)
        {
            if (basketId == Guid.Empty)
                return null;

            client.SetBearerToken(await httpContextAccessor.HttpContext.GetTokenAsync("access_token"));
            var response = await client.GetAsync($"/api/baskets/{basketId}");
            return await response.ReadContentAs<Basket>();
        }

        public async Task<IEnumerable<BasketLine>> GetLinesForBasket(Guid basketId)
        {
            if (basketId == Guid.Empty)
                return new BasketLine[0];

            client.SetBearerToken(await httpContextAccessor.HttpContext.GetTokenAsync("access_token"));
            var response = await client.GetAsync($"/api/baskets/{basketId}/basketLines");
            return await response.ReadContentAs<BasketLine[]>();

        }

        public async Task UpdateLine(Guid basketId, BasketLineForUpdate basketLineForUpdate)
        {
            client.SetBearerToken(await httpContextAccessor.HttpContext.GetTokenAsync("access_token"));
            await client.PutAsJson($"/api/baskets/{basketId}/basketLines/{basketLineForUpdate.LineId}", basketLineForUpdate);
        }

        public async Task RemoveLine(Guid basketId, Guid lineId)
        {
            client.SetBearerToken(await httpContextAccessor.HttpContext.GetTokenAsync("access_token"));
            await client.DeleteAsync($"/api/baskets/{basketId}/basketLines/{lineId}");
        }

        public async Task ApplyCouponToBasket(Guid basketId, CouponForUpdate couponForUpdate)
        {
            client.SetBearerToken(await httpContextAccessor.HttpContext.GetTokenAsync("access_token"));
            var response = await client.PutAsJson($"/api/baskets/{basketId}/coupon", couponForUpdate);
            //return await response.ReadContentAs<Coupon>();
        }

        public async Task<BasketForCheckout> Checkout(Guid basketId, BasketForCheckout basketForCheckout)
        {
            client.SetBearerToken(await httpContextAccessor.HttpContext.GetTokenAsync("access_token"));
            var response = await client.PostAsJson($"api/baskets/checkout", basketForCheckout);
            if(response.IsSuccessStatusCode)
                return await response.ReadContentAs<BasketForCheckout>();
            else
            {
                throw new Exception("Something went wrong placing your order. Please try again.");
            }
        }
    }
}
