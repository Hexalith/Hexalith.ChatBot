using System.Net;
using System.Net.Http.Headers;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using Shouldly;

namespace Hexalith.ChatBot.Server.Tests.Authentication;

/// <summary>Proves inherited discovery cannot replace the fixture's actual signed bearer validation.</summary>
public sealed class ChatBotSignedBearerFixtureTests
{
    [Theory]
    [InlineData("valid", HttpStatusCode.OK)]
    [InlineData("signature", HttpStatusCode.Unauthorized)]
    [InlineData("issuer", HttpStatusCode.Unauthorized)]
    [InlineData("audience", HttpStatusCode.Unauthorized)]
    public async Task ExternalAuthorityMustNotReplaceSignatureIssuerOrAudienceValidation(string credential, HttpStatusCode expectedStatus)
    {
        using WebApplicationFactory<Program> factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            _ = builder.UseSetting("Authentication:JwtBearer:Authority", "https://external.invalid/realms/inherited");
            ChatBotSignedBearerFixture.Configure(builder);
        });
        using HttpClient client = factory.CreateClient();
        JwtBearerOptions options = factory.Services.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>().Get(JwtBearerDefaults.AuthenticationScheme);
        string.IsNullOrEmpty(options.Authority).ShouldBeTrue();
        options.ConfigurationManager.ShouldBeNull();
        options.TokenValidationParameters.ValidateIssuerSigningKey.ShouldBeTrue();
        options.TokenValidationParameters.ValidateIssuer.ShouldBeTrue();
        options.TokenValidationParameters.ValidateAudience.ShouldBeTrue();
        using HttpRequestMessage request = new(HttpMethod.Get, "/health/chatbot");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", ChatBotSignedBearerFixture.CreateToken(
            "fixture-tenant", invalidSignature: credential == "signature", invalidIssuer: credential == "issuer", invalidAudience: credential == "audience"));

        using HttpResponseMessage response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(expectedStatus);
    }
}
