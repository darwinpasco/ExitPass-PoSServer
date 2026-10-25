using System.Security.Cryptography;
using ExitPass.PosServer.Persistence.Postgres.FiscalDocuments;
using ExitPass.PosServer.Runtime.FiscalDocuments;
using Xunit;

namespace ExitPass.PosServer.Persistence.Postgres.Tests.FiscalDocuments;

public sealed class StatutoryBeneficiaryIdentityProtectorTests
{
    [Fact]
    public void ProtectUsesRandomizedAesGcmAndRoundTripsRestrictedValues()
    {
        var protector = Protector(Enumerable.Range(1, 32).Select(x => (byte)x).ToArray());
        var document = Guid.NewGuid(); var decision = Guid.NewGuid(); var identity = Identity();
        var first = protector.Protect(document, decision, identity); var second = protector.Protect(document, decision, identity);
        Assert.NotEqual(first.Envelope, second.Envelope);
        Assert.Equal(identity, protector.Unprotect(document, decision, first));
        Assert.DoesNotContain(identity.StatutoryIdNumber, Convert.ToBase64String(first.Envelope));
    }

    [Fact]
    public void WrongKeyAndTamperingFailClosed()
    {
        var document=Guid.NewGuid();var decision=Guid.NewGuid();var protectedValue=Protector(new byte[32]).Protect(document,decision,Identity());
        Assert.ThrowsAny<CryptographicException>(()=>Protector(Enumerable.Repeat((byte)1,32).ToArray()).Unprotect(document,decision,protectedValue));
        protectedValue.Envelope[^1]^=1;
        Assert.ThrowsAny<CryptographicException>(()=>Protector(new byte[32]).Unprotect(document,decision,protectedValue));
    }

    private static AesGcmStatutoryBeneficiaryIdentityProtector Protector(byte[] key)=>new(Convert.ToBase64String(key),"pos-statutory-beneficiary","v1");
    private static StatutoryBeneficiaryIdentitySnapshot Identity()=>new("Maria Dela Cruz","OSCA_ID","OSCA-12345","123-456-789","actor","OPERATOR_CONSOLE",DateTimeOffset.UnixEpoch,"actor","OPERATOR_CONSOLE",DateTimeOffset.UnixEpoch,"reviewer",DateTimeOffset.UnixEpoch,"decision", "correlation");
}
