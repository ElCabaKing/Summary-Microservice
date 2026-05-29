using SummaryService.Infrastructure.Encryption;

namespace SummaryService.UnitTests.Infrastructure;

public class ApiKeyHashServiceTests
{
    private readonly ApiKeyHashService _service = new();

    [Fact]
    public void GenerateApiKey_ReturnsStringStartingWithSmm()
    {
        var key = _service.GenerateApiKey();

        Assert.StartsWith("smm_", key);
    }

    [Fact]
    public void GenerateApiKey_ReturnsKeyOfExpectedLength()
    {
        var key = _service.GenerateApiKey();

        Assert.Equal(68, key.Length);
    }

    [Fact]
    public void GenerateApiKey_ReturnsUniqueKeys()
    {
        var key1 = _service.GenerateApiKey();
        var key2 = _service.GenerateApiKey();

        Assert.NotEqual(key1, key2);
    }

    [Fact]
    public void ComputeHash_Returns64HexChars()
    {
        var hash = _service.ComputeHash("test-key");

        Assert.Equal(64, hash.Length);
        Assert.Matches("^[0-9a-f]{64}$", hash);
    }

    [Fact]
    public void ComputeHash_SameInput_ReturnsSameHash()
    {
        var hash1 = _service.ComputeHash("test-key");
        var hash2 = _service.ComputeHash("test-key");

        Assert.Equal(hash1, hash2);
    }

    [Fact]
    public void ComputeHash_DifferentInput_ReturnsDifferentHash()
    {
        var hash1 = _service.ComputeHash("key-one");
        var hash2 = _service.ComputeHash("key-two");

        Assert.NotEqual(hash1, hash2);
    }

    [Fact]
    public void VerifyKey_ValidKey_ReturnsTrue()
    {
        var rawKey = _service.GenerateApiKey();
        var hash = _service.ComputeHash(rawKey);

        var result = _service.VerifyKey(rawKey, hash);

        Assert.True(result);
    }

    [Fact]
    public void VerifyKey_InvalidKey_ReturnsFalse()
    {
        var hash = _service.ComputeHash("real-key");

        var result = _service.VerifyKey("wrong-key", hash);

        Assert.False(result);
    }

    [Fact]
    public void VerifyKey_IsCaseInsensitive()
    {
        var rawKey = _service.GenerateApiKey();
        var hash = _service.ComputeHash(rawKey);

        var result = _service.VerifyKey(rawKey, hash.ToUpperInvariant());

        Assert.True(result);
    }
}
