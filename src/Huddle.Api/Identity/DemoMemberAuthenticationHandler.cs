using System.Security.Claims;
using System.Text.Encodings.Web;
using Huddle.SharedKernel.Contracts;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace Huddle.Api.Identity;

/// <summary>
/// Development stand-in for real sign-in: the caller names the member in an X-Member-Id header.
/// Refused outside Development unless explicitly allowed (see HuddleApi). Replaced by OIDC in a later slice.
/// </summary>
internal sealed class DemoMemberAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    IGroupDirectory directory) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "DemoMember";
    public const string Header = "X-Member-Id";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var memberId = Request.Headers[Header].ToString();
        if (string.IsNullOrWhiteSpace(memberId))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var person = directory.FindPerson(memberId);
        if (person is null)
        {
            return Task.FromResult(AuthenticateResult.Fail("Unknown member."));
        }

        var identity = new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, person.MemberId), new Claim(ClaimTypes.Name, person.DisplayName)],
            SchemeName);
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName)));
    }
}
