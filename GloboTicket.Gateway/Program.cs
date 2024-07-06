using GloboTicket.Gateway.DelegatingHandlers;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using Ocelot.Values;
using System.IdentityModel.Tokens.Jwt;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddHttpClient();
builder.Services.AddAccessTokenManagement();
JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

var autheticationScheme = "GloboTicketGatewayAuthenticationScheme";

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(autheticationScheme, options => {
        options.Audience = "globoticketgateway";
        options.Authority = "https://localhost:5010";
    });

builder.Services.AddScoped<TokenExchangeDelegate>();
builder.Services.AddOcelot().AddDelegatingHandler<TokenExchangeDelegate>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseAuthentication();

await app.UseOcelot();
await app.RunAsync();
