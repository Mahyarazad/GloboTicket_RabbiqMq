// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.


using IdentityServer4.Models;
using System.Collections.Generic;

namespace GloboTicket.Services.Identity
{
    public static class Config
    {
        public static IEnumerable<IdentityResource> IdentityResources =>
            new IdentityResource[]
            {
                new IdentityResources.OpenId(),
                new IdentityResources.Profile(),
            };
        public static IEnumerable<ApiResource> ApiResources =>
                new ApiResource[]
                {
                    new ApiResource("eventcatalog", "Event Catalog API")
                    {
                        Scopes = { "eventcatalog.read", "eventcatalog.write" }
                    },
                    new ApiResource("shoppingbasket", "Shopping Basket API")
                    {
                        Scopes = { "shoppingbasket.fullaccess" }
                    },
                    new ApiResource("discount", "Discount API")
                    {
                        Scopes = { "discount.fullaccess" }
                    }
                };
        public static IEnumerable<ApiScope> ApiScopes =>
            new ApiScope[]
            {
                new ApiScope("eventcatalog.fullaccess"),
                new ApiScope("shoppingbasket.fullaccess"), 
                new ApiScope("eventcatalog.read"),
                new ApiScope("eventcatalog.write"),
                new ApiScope("discount.fullaccess")
            };

        public static IEnumerable<Client> Clients =>
            new Client[]
            {
                new Client
                {
                    ClientName = "GloboTicket Machine 2 Machine Client",
                    ClientId = "globoticket",
                    ClientSecrets = { new Secret("3416eeca-e569-44fe-a06e-b0eb0d70a855".Sha256())},
                    AllowedGrantTypes = GrantTypes.ClientCredentials,
                    AllowedScopes = { "eventcatalog.fullaccess" }
                },

                new Client
                {
                     ClientName = "GloboTicket Interactive Client",
                    ClientId = "globoticketinteractive",
                    ClientSecrets = { new Secret("aed65b30-071f-4058-b42b-6ac0955ca3b9".Sha256())},
                    AllowedGrantTypes = GrantTypes.Code,
                    RedirectUris = {"https://localhost:5000/signin-oidc"},
                    PostLogoutRedirectUris = {"https://localhost:5000/signout-callback-oidc"},
                    AllowedScopes = { "openid", "profile", "shoppingbasket.fullaccess" }
                },
                new Client
                {
                    ClientName = "GloboTicket Client",
                    ClientId = "globoticketclient",
                    ClientSecrets = { new Secret ("aed65b30-071f-4058-b42b-6ac0955ca3b9".Sha256()) },
                    AllowedGrantTypes = GrantTypes.CodeAndClientCredentials,
                    RedirectUris = {"https://localhost:5000/signin-oidc"},
                    PostLogoutRedirectUris = {"https://localhost:5000/signout-callback-oidc"},
                    AllowedScopes = { "openid", "profile", "shoppingbasket.fullaccess", "eventcatalog.read", "eventcatalog.write" }
                },
                new Client
                {
                    ClientName = "Shopping Basket Token Exchange Client",
                    ClientId = "shoppingbaskettodownstreamtokenexchangeclient",
                    ClientSecrets = { new Secret ("aed65b30-071f-4058-b42b-6ac0955ca3b9".Sha256()) },
                    AllowedGrantTypes = new [] { "urn:ietf:params:oauth:grant-type:token-exchange"},
                    AllowedScopes = { "openid", "profile", "discount.fullaccess" }
                },

            };
    }
}