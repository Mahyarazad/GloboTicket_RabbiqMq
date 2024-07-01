﻿// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
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
                    new ApiResource("globoticket", "GlooTicket API")
                    {
                        Scopes = { "globoticket.fullaccess" }
                    }
                };
        public static IEnumerable<ApiScope> ApiScopes =>
            new ApiScope[]
            {
                new ApiScope("globoticket.fullaccess")
            };

        public static IEnumerable<Client> Clients =>
            new Client[]
            {
                new Client
                {
                    ClientName = "GloboTicket Machine 2 Machine Client",
                    ClientId = "globoticketm2m",
                    ClientSecrets = { new Secret("3416eeca-e569-44fe-a06e-b0eb0d70a855".Sha256())},
                    AllowedGrantTypes = GrantTypes.ClientCredentials,
                    AllowedScopes = { "globoticket.fullaccess" }
                },

                new Client
                {
                     ClientName = "GloboTicket Interactive Client",
                    ClientId = "globoticketinteractive",
                    ClientSecrets = { new Secret("aed65b30-071f-4058-b42b-6ac0955ca3b9".Sha256())},
                    AllowedGrantTypes = GrantTypes.Code,
                    RedirectUris = {"https://localhost:5000/signin-oidc"},
                    PostLogoutRedirectUris = {"https://localhost:5000/signout-callback-oidc"},
                    AllowedScopes = { "openid", "profile"}
                }
            };
    }
}