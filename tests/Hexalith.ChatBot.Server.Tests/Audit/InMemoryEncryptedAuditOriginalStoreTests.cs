using Hexalith.ChatBot.Server.Audit;

using Shouldly;

namespace Hexalith.ChatBot.Server.Tests.Audit;

/// <summary>
/// Story 9.1 (AC3) direct coverage for <see cref="InMemoryEncryptedAuditOriginalStore"/>: opaque ciphertext stored by
/// key handle round-trips on read; an unknown handle fails closed; and the store hands back defensive copies so a caller
/// mutating the returned bytes cannot corrupt the at-rest ciphertext (the only carrier of original content).
/// </summary>
public sealed class InMemoryEncryptedAuditOriginalStoreTests
{
    private const string Handle = "rk-abc123";

    [Fact]
    public void StoredCiphertextRoundTripsByHandle()
    {
        InMemoryEncryptedAuditOriginalStore store = new();
        byte[] ciphertext = [1, 2, 3, 4, 5];

        store.Store(Handle, ciphertext);

        store.TryGet(Handle, out byte[] retrieved).ShouldBeTrue();
        retrieved.ShouldBe(ciphertext);
    }

    [Fact]
    public void UnknownHandleFailsClosed()
    {
        InMemoryEncryptedAuditOriginalStore store = new();

        store.TryGet("rk-never-stored", out byte[] retrieved).ShouldBeFalse();
        retrieved.ShouldBeEmpty();
    }

    [Fact]
    public void StoreTakesADefensiveCopyOfTheCiphertext()
    {
        InMemoryEncryptedAuditOriginalStore store = new();
        byte[] ciphertext = [1, 2, 3, 4, 5];
        store.Store(Handle, ciphertext);

        // Mutating the caller's array after Store must not change what the store holds.
        ciphertext[0] = 0xFF;

        store.TryGet(Handle, out byte[] retrieved).ShouldBeTrue();
        retrieved[0].ShouldBe((byte)1);
    }

    [Fact]
    public void ReturnedCiphertextIsADefensiveCopy()
    {
        InMemoryEncryptedAuditOriginalStore store = new();
        store.Store(Handle, [1, 2, 3, 4, 5]);

        store.TryGet(Handle, out byte[] first).ShouldBeTrue();
        first[0] = 0xFF;

        // A mutation of the first read must not leak into a subsequent read.
        store.TryGet(Handle, out byte[] second).ShouldBeTrue();
        second[0].ShouldBe((byte)1);
    }
}
