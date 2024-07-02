using IdentityModel.Client;
using IdentityServer4.Extensions;
using IdentityServer4.Models;
using IdentityServer4.Validation;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace GloboTicket.Services.Identity.Services
{
    public class TokenExchangeExtentionGrantValidator : IExtensionGrantValidator
    {
        public string GrantType => "urn:ietf:params:oauth:grant-type:token-exchange";
        private string _accessTokenType => "urn:ietf:params:oauth:grant-type:access-token";
        private readonly ITokenValidator _tokenValidator;

        public TokenExchangeExtentionGrantValidator(ITokenValidator tokenValidator)
        {
            _tokenValidator = tokenValidator ?? throw new ArgumentNullException(nameof(tokenValidator));
        }

        public async Task ValidateAsync(ExtensionGrantValidationContext context)
        {
            var requestToken = context.Request.Raw.Get("grant-type");
            if (string.IsNullOrEmpty(requestToken) || requestToken != GrantType)
            {
                context.Result = new GrantValidationResult(TokenRequestErrors.InvalidGrant, "Invalid Grant");
                return;
            }

            var subjectToken = context.Request.Raw.Get("subject-token");
            if (string.IsNullOrEmpty(subjectToken))
            {
                context.Result = new GrantValidationResult(TokenRequestErrors.InvalidRequest, "Subject token missing");
                return;
            }

            var subjectTokenType = context.Request.Raw.Get("subject-token-type");
            if (string.IsNullOrEmpty(subjectTokenType))
            {
                context.Result = new GrantValidationResult(TokenRequestErrors.InvalidRequest, "Subject token type missing");
                return;
            }

            if (subjectTokenType != _accessTokenType)
            {
                context.Result = new GrantValidationResult(TokenRequestErrors.InvalidRequest, "Subject token type invalid");
                return;
            }

            var result = await _tokenValidator.ValidateAccessTokenAsync(subjectToken);
            if (result.IsError)
            {
                context.Result = new GrantValidationResult(TokenRequestErrors.InvalidGrant, "Subject token invalid");
                return;
            }

            var subjectClaim = result.Claims.FirstOrDefault(c => c.Type == "sub");
            if (subjectClaim is null)
            {
                context.Result = new GrantValidationResult(TokenRequestErrors.InvalidRequest, "Subject token must contain sub value");
                return;
            }

            context.Result = new GrantValidationResult(subjectClaim.Value, "access-token", result.Claims);

        }
    }
}
