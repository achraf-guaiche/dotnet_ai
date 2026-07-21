using FluentAssertions;
using Formations.Domain.Entities;
using Formations.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Formations.Tests.Unit.Services;

public class JwtServiceTests
{
    private readonly JwtService _sut;
    private readonly IConfiguration _config;

    public JwtServiceTests()
    {
        var configValues = new Dictionary<string, string?>
        {
            ["Jwt:Key"] = "TestKeyForUnitTests32CharactersMinimum!",
            ["Jwt:Issuer"] = "TestIssuer",
            ["Jwt:Audience"] = "TestAudience",
            ["Jwt:AccessTokenExpirationMinutes"] = "60"
        };

        _config = new ConfigurationBuilder()
            .AddInMemoryCollection(configValues)
            .Build();

        _sut = new JwtService(_config);
    }

    [Fact]
    public void GenerateAccessToken_ValidUser_ReturnsToken()
    {
        // Arrange
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid().ToString(),
            Email = "test@test.com",
            FullName = "Test User"
        };
        var roles = new List<string> { "Apprenant" };

        // Act
        var token = _sut.GenerateAccessToken(user, roles);

        // Assert
        token.Should().NotBeNullOrEmpty();
        token.Split('.').Should().HaveCount(3);
    }

    [Fact]
    public void GenerateRefreshToken_ReturnsDifferentTokensEachTime()
    {
        var t1 = _sut.GenerateRefreshToken();
        var t2 = _sut.GenerateRefreshToken();

        t1.Should().NotBe(t2);
        t1.Length.Should().BeGreaterThan(40);
    }

    [Fact]
    public void GetUserIdFromExpiredToken_ValidToken_ReturnsUserId()
    {
        var userId = Guid.NewGuid().ToString();
        var user = new ApplicationUser
        {
            Id = userId,
            Email = "test@test.com",
            FullName = "Test"
        };

        var token = _sut.GenerateAccessToken(user, ["Apprenant"]);
        var extractedUserId = _sut.GetUserIdFromExpiredToken(token);

        extractedUserId.Should().Be(userId);
    }

    [Fact]
    public void GetUserIdFromExpiredToken_InvalidToken_ReturnsNull()
    {
        var result = _sut.GetUserIdFromExpiredToken("not.a.valid.jwt.token");
        result.Should().BeNull();
    }
}
