using FluentAssertions;
using Formations.Application.Auth.DTOs;
using Formations.Application.Auth.Services;
using Formations.Application.Common.Interfaces;
using Formations.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Moq;
using Xunit;

namespace Formations.Tests.Unit.Auth;

public class AuthServiceTests
{
    private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
    private readonly Mock<IJwtService> _jwtServiceMock;
    private readonly Mock<IEmailService> _emailServiceMock;
    private readonly Mock<IApplicationDbContext> _contextMock;
    private readonly AuthService _sut;

    public AuthServiceTests()
    {
        var store = new Mock<IUserStore<ApplicationUser>>();
        _userManagerMock = new Mock<UserManager<ApplicationUser>>(store.Object, null!, null!, null!, null!, null!, null!, null!, null!);
        _jwtServiceMock = new Mock<IJwtService>();
        _emailServiceMock = new Mock<IEmailService>();
        _contextMock = new Mock<IApplicationDbContext>();

        _sut = new AuthService(_userManagerMock.Object, _jwtServiceMock.Object, _emailServiceMock.Object, _contextMock.Object);
    }

    [Fact]
    public async Task Register_WithExistingEmail_ReturnsFailure()
    {
        // Arrange
        var request = new RegisterRequest("Jean Dupont", "jean@test.com", "Password@1", "Password@1");
        _userManagerMock.Setup(m => m.FindByEmailAsync(request.Email))
            .ReturnsAsync(new ApplicationUser { Email = request.Email });

        // Act
        var result = await _sut.RegisterAsync(request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("compte existe déjà");
    }

    [Fact]
    public async Task Register_WithNewEmail_ReturnsSuccess()
    {
        // Arrange
        var request = new RegisterRequest("Jean Dupont", "jean@test.com", "Password@1", "Password@1");

        _userManagerMock.Setup(m => m.FindByEmailAsync(request.Email)).ReturnsAsync((ApplicationUser?)null);
        _userManagerMock.Setup(m => m.CreateAsync(It.IsAny<ApplicationUser>(), request.Password))
            .ReturnsAsync(IdentityResult.Success);
        _userManagerMock.Setup(m => m.AddToRoleAsync(It.IsAny<ApplicationUser>(), "Apprenant"))
            .ReturnsAsync(IdentityResult.Success);
        _userManagerMock.Setup(m => m.GetRolesAsync(It.IsAny<ApplicationUser>()))
            .ReturnsAsync(new List<string> { "Apprenant" });
        _userManagerMock.Setup(m => m.UpdateAsync(It.IsAny<ApplicationUser>()))
            .ReturnsAsync(IdentityResult.Success);

        _jwtServiceMock.Setup(j => j.GenerateAccessToken(It.IsAny<ApplicationUser>(), It.IsAny<IList<string>>()))
            .Returns("mock-access-token");
        _jwtServiceMock.Setup(j => j.GenerateRefreshToken()).Returns("mock-refresh-token");

        _emailServiceMock.Setup(e => e.SendWelcomeAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _contextMock.Setup(c => c.ActivityLogs).Returns(MockDbSet<ActivityLog>());
        _contextMock.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        // Act
        var result = await _sut.RegisterAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Email.Should().Be(request.Email);
        result.Data.AccessToken.Should().Be("mock-access-token");
    }

    [Theory]
    [InlineData("Formateur", "Formateur")]
    [InlineData("Apprenant", "Apprenant")]
    [InlineData("Admin", "Apprenant")]       // Admin ne peut pas être auto-attribué
    [InlineData("n'importe quoi", "Apprenant")]
    public async Task Register_AssignsOnlyWhitelistedRole(string roleDemande, string roleAttendu)
    {
        // Arrange
        var request = new RegisterRequest("Jean Dupont", "jean@test.com", "Password@1", "Password@1", roleDemande);
        SetupSuccessfulRegistration(roleAttendu);
        _contextMock.Setup(c => c.Formateurs).Returns(MockDbSet<Formateur>());

        // Act
        var result = await _sut.RegisterAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _userManagerMock.Verify(m => m.AddToRoleAsync(It.IsAny<ApplicationUser>(), roleAttendu), Times.Once);
        _userManagerMock.Verify(m => m.AddToRoleAsync(It.IsAny<ApplicationUser>(), "Admin"), Times.Never);
    }

    private void SetupSuccessfulRegistration(string role)
    {
        _userManagerMock.Setup(m => m.FindByEmailAsync(It.IsAny<string>())).ReturnsAsync((ApplicationUser?)null);
        _userManagerMock.Setup(m => m.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success);
        _userManagerMock.Setup(m => m.AddToRoleAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success);
        _userManagerMock.Setup(m => m.GetRolesAsync(It.IsAny<ApplicationUser>()))
            .ReturnsAsync(new List<string> { role });
        _userManagerMock.Setup(m => m.UpdateAsync(It.IsAny<ApplicationUser>())).ReturnsAsync(IdentityResult.Success);

        _jwtServiceMock.Setup(j => j.GenerateAccessToken(It.IsAny<ApplicationUser>(), It.IsAny<IList<string>>()))
            .Returns("mock-access-token");
        _jwtServiceMock.Setup(j => j.GenerateRefreshToken()).Returns("mock-refresh-token");
        _emailServiceMock.Setup(e => e.SendWelcomeAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _contextMock.Setup(c => c.ActivityLogs).Returns(MockDbSet<ActivityLog>());
        _contextMock.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
    }

    [Fact]
    public async Task Login_WithWrongPassword_ReturnsFailure()
    {
        // Arrange
        var request = new LoginRequest("jean@test.com", "WrongPassword");
        var user = new ApplicationUser { Email = request.Email, IsActive = true };

        _userManagerMock.Setup(m => m.FindByEmailAsync(request.Email)).ReturnsAsync(user);
        _userManagerMock.Setup(m => m.CheckPasswordAsync(user, request.Password)).ReturnsAsync(false);

        // Act
        var result = await _sut.LoginAsync(request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("incorrect");
    }

    [Fact]
    public async Task Login_InactiveUser_ReturnsFailure()
    {
        // Arrange
        var request = new LoginRequest("jean@test.com", "Password@1");
        var user = new ApplicationUser { Email = request.Email, IsActive = false };

        _userManagerMock.Setup(m => m.FindByEmailAsync(request.Email)).ReturnsAsync(user);

        // Act
        var result = await _sut.LoginAsync(request);

        // Assert
        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task ForgotPassword_UnknownEmail_ReturnsSuccess()
    {
        // ForgotPassword doit toujours retourner succès pour ne pas révéler si l'email existe
        _userManagerMock.Setup(m => m.FindByEmailAsync("unknown@test.com")).ReturnsAsync((ApplicationUser?)null);

        var result = await _sut.ForgotPasswordAsync("unknown@test.com", "https://app.local");

        result.IsSuccess.Should().BeTrue();
    }

    private static Microsoft.EntityFrameworkCore.DbSet<T> MockDbSet<T>() where T : class
    {
        var mockSet = new Mock<Microsoft.EntityFrameworkCore.DbSet<T>>();
        return mockSet.Object;
    }
}
