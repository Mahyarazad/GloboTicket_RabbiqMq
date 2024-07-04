﻿using GloboTicket.Web.Extensions;
using GloboTicket.Web.Models.Api;
using IdentityModel.Client;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace GloboTicket.Web.Services
{
    public class EventCatalogService : IEventCatalogService
    {
        private readonly HttpClient client;
        private readonly IHttpContextAccessor httpContextAccessor;
        public EventCatalogService(HttpClient client, IHttpContextAccessor httpContextAccessor)
        {
            this.client = client;
            this.httpContextAccessor = httpContextAccessor;

        }

        //public async Task<string> GetToken()
        //{
        //    if (!string.IsNullOrEmpty(_accessToken))
        //    {
        //        return _accessToken;
        //    }

        //    var discoveryDocumentResponse = await client.GetDiscoveryDocumentAsync("https://localhost:5010");
        //    if (discoveryDocumentResponse.IsError)
        //    {
        //        throw new Exception(discoveryDocumentResponse.Error);
        //    }

        //    var tokenResponse = await client.RequestClientCredentialsTokenAsync(new ClientCredentialsTokenRequest
        //    {
        //        Address = discoveryDocumentResponse.TokenEndpoint,
        //        ClientId = "globoticketclient",
        //        ClientSecret = "aed65b30-071f-4058-b42b-6ac0955ca3b9",
        //        Scope = "eventcatalog.read"
        //    });

        //    if (tokenResponse.IsError)
        //    {
        //        throw new Exception(tokenResponse.Error);
        //    }

        //    _accessToken = tokenResponse.AccessToken;
        //    return _accessToken;

        //}

        public async Task<IEnumerable<Event>> GetAll()
        {
            client.SetBearerToken(await httpContextAccessor.HttpContext.GetTokenAsync("access_token"));
            var response = await client.GetAsync("api/events");
            return await response.ReadContentAs<List<Event>>();
        }

        public async Task<IEnumerable<Event>> GetByCategoryId(Guid categoryid)
        {
            client.SetBearerToken(await httpContextAccessor.HttpContext.GetTokenAsync("access_token"));
            var response = await client.GetAsync($"api/events/?categoryId={categoryid}");
            return await response.ReadContentAs<List<Event>>();
        }

        public async Task<Event> GetEvent(Guid id)
        {
            client.SetBearerToken(await httpContextAccessor.HttpContext.GetTokenAsync("access_token"));
            var response = await client.GetAsync($"api/events/{id}");
            return await response.ReadContentAs<Event>();
        }

        public async Task<IEnumerable<Category>> GetCategories()
        {
            client.SetBearerToken(await httpContextAccessor.HttpContext.GetTokenAsync("access_token"));
            var response = await client.GetAsync("api/categories");
            return await response.ReadContentAs<List<Category>>();
        }

        public async Task<PriceUpdate> UpdatePrice(PriceUpdate priceUpdate)
        {
            client.SetBearerToken(await httpContextAccessor.HttpContext.GetTokenAsync("access_token"));
            var response = await client.PostAsJson($"api/events/eventpriceupdate", priceUpdate);
            return await response.ReadContentAs<PriceUpdate>();
        }
    }
}
