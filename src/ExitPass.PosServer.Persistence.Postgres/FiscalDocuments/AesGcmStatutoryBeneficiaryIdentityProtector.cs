using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ExitPass.PosServer.Runtime.FiscalDocuments;

namespace ExitPass.PosServer.Persistence.Postgres.FiscalDocuments;

public sealed class AesGcmStatutoryBeneficiaryIdentityProtector : IStatutoryBeneficiaryIdentityProtector
{
    private const short CurrentFormatVersion = 1;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly byte[]? key;

    public AesGcmStatutoryBeneficiaryIdentityProtector(string? keyBase64, string? keyReference, string? keyVersion)
    {
        KeyReference = keyReference?.Trim() ?? string.Empty;
        KeyVersion = keyVersion?.Trim() ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(keyBase64))
        {
            try { key = Convert.FromBase64String(keyBase64); }
            catch (FormatException ex) { throw new InvalidOperationException("The statutory beneficiary protection key is not valid Base64.", ex); }
        }
        if (key is not null && key.Length != 32)
            throw new InvalidOperationException("The statutory beneficiary protection key must contain exactly 256 bits.");
    }

    public bool IsConfigured => key is not null && KeyReference.Length > 0 && KeyVersion.Length > 0;
    public string KeyReference { get; }
    public string KeyVersion { get; }

    public ProtectedStatutoryBeneficiaryIdentity Protect(Guid fiscalDocumentId, Guid statutoryDecisionId, StatutoryBeneficiaryIdentitySnapshot identity)
    {
        EnsureConfigured();
        var plaintext = JsonSerializer.SerializeToUtf8Bytes(identity, JsonOptions);
        try
        {
            var nonce = RandomNumberGenerator.GetBytes(12);
            var ciphertext = new byte[plaintext.Length];
            var tag = new byte[16];
            using var aes = new AesGcm(key!, 16);
            aes.Encrypt(nonce, plaintext, ciphertext, tag, AssociatedData(fiscalDocumentId, statutoryDecisionId));
            var envelope = new byte[2 + nonce.Length + tag.Length + ciphertext.Length];
            BinaryPrimitives.WriteInt16BigEndian(envelope.AsSpan(0, 2), CurrentFormatVersion);
            nonce.CopyTo(envelope, 2);
            tag.CopyTo(envelope, 14);
            ciphertext.CopyTo(envelope, 30);
            CryptographicOperations.ZeroMemory(ciphertext);
            return new(envelope, KeyReference, KeyVersion, CurrentFormatVersion);
        }
        finally { CryptographicOperations.ZeroMemory(plaintext); }
    }

    public StatutoryBeneficiaryIdentitySnapshot Unprotect(Guid fiscalDocumentId, Guid statutoryDecisionId, ProtectedStatutoryBeneficiaryIdentity protectedIdentity)
    {
        EnsureConfigured();
        var envelope = protectedIdentity.Envelope;
        if (protectedIdentity.EnvelopeFormatVersion != CurrentFormatVersion || envelope.Length < 31 ||
            !string.Equals(protectedIdentity.KeyReference, KeyReference, StringComparison.Ordinal) ||
            !string.Equals(protectedIdentity.KeyVersion, KeyVersion, StringComparison.Ordinal) ||
            BinaryPrimitives.ReadInt16BigEndian(envelope.AsSpan(0, 2)) != CurrentFormatVersion)
            throw new CryptographicException("The statutory beneficiary envelope cannot be opened by the active key version.");

        var plaintext = new byte[envelope.Length - 30];
        try
        {
            using var aes = new AesGcm(key!, 16);
            aes.Decrypt(envelope.AsSpan(2, 12), envelope.AsSpan(30), envelope.AsSpan(14, 16), plaintext,
                AssociatedData(fiscalDocumentId, statutoryDecisionId));
            return JsonSerializer.Deserialize<StatutoryBeneficiaryIdentitySnapshot>(plaintext, JsonOptions)
                ?? throw new CryptographicException("The statutory beneficiary envelope payload is invalid.");
        }
        finally { CryptographicOperations.ZeroMemory(plaintext); }
    }

    private byte[] AssociatedData(Guid fiscalDocumentId, Guid statutoryDecisionId) =>
        Encoding.UTF8.GetBytes($"exitpass:pos:statutory-beneficiary:v1:{fiscalDocumentId:D}:{statutoryDecisionId:D}:{KeyReference}:{KeyVersion}");

    private void EnsureConfigured()
    {
        if (!IsConfigured) throw new InvalidOperationException("Statutory beneficiary identity protection is not configured.");
    }
}
