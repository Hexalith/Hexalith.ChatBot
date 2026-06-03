using System.Text;

using Hexalith.ChatBot.Server.Audit;

using Shouldly;

namespace Hexalith.ChatBot.Server.Tests.Audit;

/// <summary>
/// Story 9.1 (AC3, NFR49a/NFR2/NFR42, cross-cutting #13) direct coverage for the separate-KMS boundary
/// <see cref="InMemoryKmsRedactionKeyStore"/>: an encrypt→decrypt round-trip recovers the plaintext; the ciphertext is
/// opaque at rest (it never contains the plaintext bytes); a fresh nonce per call makes repeated encryptions diverge;
/// shredding the key makes the held ciphertext permanently unrecoverable (crypto-shred); a tampered ciphertext is
/// rejected by the AEAD tag; and unknown / shredded handles fail closed rather than throwing on decrypt.
/// </summary>
public sealed class InMemoryKmsRedactionKeyStoreTests
{
    private const string Subject = "subject-1";

    [Fact]
    public void EncryptThenDecryptRoundTripsThePlaintext()
    {
        InMemoryKmsRedactionKeyStore kms = new();
        string handle = kms.CreateKey(Subject);
        byte[] plaintext = Encoding.UTF8.GetBytes("original-audit-payload");

        byte[] ciphertext = kms.Encrypt(handle, plaintext);

        kms.TryDecrypt(handle, ciphertext, out byte[] recovered).ShouldBeTrue();
        recovered.ShouldBe(plaintext);
    }

    [Fact]
    public void CiphertextIsOpaqueAndNeverContainsThePlaintextBytes()
    {
        // No-leak floor (NFR2/NFR42): the encrypted original is the ONLY carrier of raw content, and it is opaque
        // ciphertext at rest. A recognizable plaintext marker must not survive into the stored ciphertext.
        InMemoryKmsRedactionKeyStore kms = new();
        string handle = kms.CreateKey(Subject);
        byte[] plaintext = Encoding.UTF8.GetBytes("secret-resource-marker-value");

        byte[] ciphertext = kms.Encrypt(handle, plaintext);

        IndexOfSubsequence(ciphertext, plaintext).ShouldBe(-1);
        // The body (after the 12-byte nonce + 16-byte tag) is at least as long as the plaintext but byte-different.
        ciphertext.Length.ShouldBeGreaterThanOrEqualTo(plaintext.Length + 28);
    }

    [Fact]
    public void RepeatedEncryptionOfTheSamePlaintextDivergesViaPerCallNonce()
    {
        InMemoryKmsRedactionKeyStore kms = new();
        string handle = kms.CreateKey(Subject);
        byte[] plaintext = Encoding.UTF8.GetBytes("original-audit-payload");

        byte[] first = kms.Encrypt(handle, plaintext);
        byte[] second = kms.Encrypt(handle, plaintext);

        // A fresh random nonce per call means identical plaintext never yields identical ciphertext.
        first.ShouldNotBe(second);
        // Both still decrypt back to the same plaintext.
        kms.TryDecrypt(handle, first, out byte[] a).ShouldBeTrue();
        kms.TryDecrypt(handle, second, out byte[] b).ShouldBeTrue();
        a.ShouldBe(plaintext);
        b.ShouldBe(plaintext);
    }

    [Fact]
    public void ShredMakesTheHeldCiphertextPermanentlyUnrecoverable()
    {
        InMemoryKmsRedactionKeyStore kms = new();
        string handle = kms.CreateKey(Subject);
        byte[] ciphertext = kms.Encrypt(handle, Encoding.UTF8.GetBytes("original-audit-payload"));
        kms.HasKey(handle).ShouldBeTrue();

        kms.Shred(handle);

        // Crypto-shred: the key is gone, so the still-held ciphertext can never be decrypted again.
        kms.HasKey(handle).ShouldBeFalse();
        kms.TryDecrypt(handle, ciphertext, out byte[] afterShred).ShouldBeFalse();
        afterShred.ShouldBeEmpty();
    }

    [Fact]
    public void TamperedCiphertextIsRejectedByTheAuthenticationTag()
    {
        InMemoryKmsRedactionKeyStore kms = new();
        string handle = kms.CreateKey(Subject);
        byte[] ciphertext = kms.Encrypt(handle, Encoding.UTF8.GetBytes("original-audit-payload"));

        // Flip a byte in the AEAD body — the authentication tag must reject the forged ciphertext.
        ciphertext[^1] ^= 0xFF;

        kms.TryDecrypt(handle, ciphertext, out byte[] forged).ShouldBeFalse();
        forged.ShouldBeEmpty();
    }

    [Fact]
    public void DecryptWithUnknownHandleFailsClosedInsteadOfThrowing()
    {
        InMemoryKmsRedactionKeyStore kms = new();

        kms.TryDecrypt("rk-never-created", [1, 2, 3, 4], out byte[] plaintext).ShouldBeFalse();
        plaintext.ShouldBeEmpty();
        kms.HasKey("rk-never-created").ShouldBeFalse();
    }

    [Fact]
    public void EncryptWithUnknownHandleThrows()
    {
        InMemoryKmsRedactionKeyStore kms = new();

        _ = Should.Throw<InvalidOperationException>(() => kms.Encrypt("rk-never-created", [1, 2, 3]));
    }

    [Fact]
    public void ShreddingAnAlreadyShreddedHandleIsIdempotent()
    {
        InMemoryKmsRedactionKeyStore kms = new();
        string handle = kms.CreateKey(Subject);

        kms.Shred(handle);
        // A second shred (or a shred of a never-existing handle) must not throw — erasure is idempotent.
        Should.NotThrow(() => kms.Shred(handle));
        Should.NotThrow(() => kms.Shred("rk-never-created"));
    }

    [Fact]
    public void DistinctKeysGetDistinctOpaqueHandles()
    {
        InMemoryKmsRedactionKeyStore kms = new();

        string first = kms.CreateKey(Subject);
        string second = kms.CreateKey("subject-2");

        first.ShouldNotBe(second);
        first.ShouldStartWith("rk-");
        second.ShouldStartWith("rk-");
    }

    /// <summary>Returns the start index of <paramref name="needle"/> within <paramref name="haystack"/>, or -1.</summary>
    private static int IndexOfSubsequence(byte[] haystack, byte[] needle)
    {
        if (needle.Length == 0 || needle.Length > haystack.Length)
        {
            return -1;
        }

        for (int start = 0; start <= haystack.Length - needle.Length; start++)
        {
            bool match = true;
            for (int offset = 0; offset < needle.Length; offset++)
            {
                if (haystack[start + offset] != needle[offset])
                {
                    match = false;
                    break;
                }
            }

            if (match)
            {
                return start;
            }
        }

        return -1;
    }
}
