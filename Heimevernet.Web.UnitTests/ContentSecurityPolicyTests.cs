using Heimevernet.Web.Security;

namespace Heimevernet.Web.UnitTests;

// Passer på at CSP-en (Security/ContentSecurityPolicy.cs) ikke blir svekket ved et uhell senere.
public class ContentSecurityPolicyTests
{
    // Henter én regel, f.eks. "script-src ...", fra hele headeren.
    private static string Directive(string policy, string name) =>
        policy.Split("; ").Single(directive => directive.StartsWith(name + " "));

    [Fact]
    public void Scripts_OnlyFromOwnServerOrWithTheNonce()
    {
        var policy = ContentSecurityPolicy.Build("abc123", isDevelopment: false);
        var scripts = Directive(policy, "script-src");

        Assert.Equal("script-src 'self' 'nonce-abc123'", scripts);
        Assert.DoesNotContain("unsafe-inline", scripts); // Ville latt injiserte scripts kjøre.
        Assert.DoesNotContain("unsafe-eval", scripts);
    }

    [Fact]
    public void Nonce_IsNewForEveryRequest()
    {
        var first = ContentSecurityPolicy.CreateNonce();
        var second = ContentSecurityPolicy.CreateNonce();

        Assert.NotEqual(first, second);
        Assert.Equal(16, Convert.FromBase64String(first).Length);
    }

    [Fact]
    public void Localhost_IsOnlyAllowedDuringDevelopment()
    {
        Assert.DoesNotContain("localhost", ContentSecurityPolicy.Build("n", isDevelopment: false));
        Assert.Contains("localhost", Directive(ContentSecurityPolicy.Build("n", isDevelopment: true), "connect-src"));
    }

    [Fact]
    public void Page_CannotBeFramedOrUsePlugins()
    {
        var policy = ContentSecurityPolicy.Build("n", isDevelopment: false);

        Assert.Contains("frame-ancestors 'none'", policy);
        Assert.Contains("object-src 'none'", policy);
    }
}
