using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using SkillShareBackend.Data;
using SkillShareBackend.DTOs;
using SkillShareBackend.Models;
using SkillShareBackend.Services;
using Xunit;

namespace SkillShareBackend.Tests;

/// <summary>
/// SPRINT 01 - Iniciar Sesión
/// Cubre todos los escenarios funcionales, de borde y negativos
/// del proceso de autenticación de usuarios.
/// </summary>
public class Sprint01_IniciarSesion_Tests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly AuthService _authService;
    private readonly Mock<ILogger<AuthService>> _loggerMock;
    private readonly IConfiguration _configuration;

    // ─── Datos de prueba base ───────────────────────────────────────────────
    private const string ValidEmail    = "usuario@skillshare.com";
    private const string ValidPassword = "Password123!";

    public Sprint01_IniciarSesion_Tests()
    {
        // Base de datos en memoria — aislada por cada instancia de test
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new AppDbContext(options);

        _loggerMock = new Mock<ILogger<AuthService>>();

        // Configuración mínima requerida para generar JWT
        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"]                = "SuperSecretKeyForTestingPurposes1234!",
                ["Jwt:Issuer"]             = "SkillShareTest",
                ["Jwt:Audience"]           = "SkillShareTestUsers",
                ["Jwt:DurationInMinutes"]  = "60"
            })
            .Build();

        _authService = new AuthService(_context, _configuration, _loggerMock.Object);

        // Sembrar un usuario válido con contraseña hasheada
        SeedValidUser();
    }

    // ════════════════════════════════════════════════════════════════════════
    // CASOS EXITOSOS
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    [Trait("Sprint", "01")]
    [Trait("Funcionalidad", "Iniciar Sesión")]
    [Trait("Tipo", "Happy Path")]
    public async Task Login_ConCredencialesValidas_RetornaExitoConToken()
    {
        // Arrange
        var request = new LoginRequestDto { Email = ValidEmail, Password = ValidPassword };

        // Act
        var result = await _authService.LoginAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue("las credenciales proporcionadas son correctas");
        result.Token.Should().NotBeNullOrWhiteSpace("se debe generar un JWT al autenticar exitosamente");
        result.User.Should().NotBeNull("la respuesta debe incluir datos del usuario");
        result.User!.Email.Should().Be(ValidEmail);
        result.Message.Should().Be("Login successful");
    }

    [Fact]
    [Trait("Sprint", "01")]
    [Trait("Funcionalidad", "Iniciar Sesión")]
    [Trait("Tipo", "Happy Path")]
    public async Task Login_ConCredencialesValidas_TokenContieneInformacionDelUsuario()
    {
        // Arrange
        var request = new LoginRequestDto { Email = ValidEmail, Password = ValidPassword };

        // Act
        var result = await _authService.LoginAsync(request);

        // Assert
        result.Success.Should().BeTrue();
        result.Token.Should().NotBeNullOrWhiteSpace();

        // Decodificar el token para verificar los claims
        var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(result.Token);

        jwtToken.Claims.Should().Contain(c => c.Type == "email" || c.Value == ValidEmail,
            "el token debe incluir el email del usuario");
    }

    [Fact]
    [Trait("Sprint", "01")]
    [Trait("Funcionalidad", "Iniciar Sesión")]
    [Trait("Tipo", "Happy Path")]
    public async Task Login_ConCredencialesValidas_RetornaIdDeUsuarioCorrecto()
    {
        // Arrange
        var request = new LoginRequestDto { Email = ValidEmail, Password = ValidPassword };
        var usuarioEnBD = await _context.Users.FirstAsync(u => u.Email == ValidEmail);

        // Act
        var result = await _authService.LoginAsync(request);

        // Assert
        result.Success.Should().BeTrue();
        result.User!.UserId.Should().Be(usuarioEnBD.UserId);
    }

    // ════════════════════════════════════════════════════════════════════════
    // CASOS NEGATIVOS — CREDENCIALES INVÁLIDAS
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    [Trait("Sprint", "01")]
    [Trait("Funcionalidad", "Iniciar Sesión")]
    [Trait("Tipo", "Negativo")]
    public async Task Login_ConEmailInexistente_RetornaFracasoSinRevelarDetalles()
    {
        // Arrange — email que no existe en la BD
        var request = new LoginRequestDto
        {
            Email    = "noexiste@skillshare.com",
            Password = ValidPassword
        };

        // Act
        var result = await _authService.LoginAsync(request);

        // Assert
        result.Success.Should().BeFalse("el email no está registrado");
        result.Token.Should().BeNullOrWhiteSpace("no se debe emitir token con credenciales inválidas");
        result.Message.Should().Be("Invalid credentials",
            "el mensaje NO debe distinguir si falló el email o la contraseña (seguridad)");
    }

    [Fact]
    [Trait("Sprint", "01")]
    [Trait("Funcionalidad", "Iniciar Sesión")]
    [Trait("Tipo", "Negativo")]
    public async Task Login_ConPasswordIncorrecta_RetornaFracasoSinRevelarDetalles()
    {
        // Arrange
        var request = new LoginRequestDto
        {
            Email    = ValidEmail,
            Password = "ContraseñaEquivocada99!"
        };

        // Act
        var result = await _authService.LoginAsync(request);

        // Assert
        result.Success.Should().BeFalse("la contraseña no coincide con el hash almacenado");
        result.Token.Should().BeNullOrWhiteSpace();
        result.Message.Should().Be("Invalid credentials");
    }

    [Xunit.Theory]
    [Trait("Sprint", "01")]
    [Trait("Funcionalidad", "Iniciar Sesión")]
    [Trait("Tipo", "Borde")]
    [InlineData("", "Password123!")]
    [InlineData("usuario@skillshare.com", "")]
    [InlineData("", "")]
    public async Task Login_ConCamposVacios_RetornaFracaso(string email, string password)
    {
        // Arrange
        var request = new LoginRequestDto { Email = email, Password = password };

        // Act
        var result = await _authService.LoginAsync(request);

        // Assert
        result.Success.Should().BeFalse("los campos vacíos no deben autenticar a nadie");
        result.Token.Should().BeNullOrWhiteSpace();
    }

    [Fact]
    [Trait("Sprint", "01")]
    [Trait("Funcionalidad", "Iniciar Sesión")]
    [Trait("Tipo", "Borde")]
    public async Task Login_ConPasswordCaseSensitive_FallaConDistintaCapitalizacion()
    {
        // Arrange — misma contraseña pero con diferente capitalización
        var request = new LoginRequestDto
        {
            Email    = ValidEmail,
            Password = ValidPassword.ToUpper()  // "PASSWORD123!"
        };

        // Act
        var result = await _authService.LoginAsync(request);

        // Assert
        result.Success.Should().BeFalse("BCrypt es sensible a mayúsculas/minúsculas");
    }

    [Fact]
    [Trait("Sprint", "01")]
    [Trait("Funcionalidad", "Iniciar Sesión")]
    [Trait("Tipo", "Borde")]
    public async Task Login_ConEmailConEspaciosExtraños_RetornaFracaso()
    {
        // Arrange
        var request = new LoginRequestDto
        {
            Email    = " usuario@skillshare.com ",  // espacios al inicio/final
            Password = ValidPassword
        };

        // Act
        var result = await _authService.LoginAsync(request);

        // Assert
        result.Success.Should().BeFalse("los emails con espacios no deben coincidir con ningún registro");
    }

    // ════════════════════════════════════════════════════════════════════════
    // VALIDACIÓN DE JWT
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    [Trait("Sprint", "01")]
    [Trait("Funcionalidad", "Iniciar Sesión")]
    [Trait("Tipo", "Seguridad")]
    public async Task Login_TokenGenerado_TieneExpiracionCorrecta()
    {
        // Arrange
        var request = new LoginRequestDto { Email = ValidEmail, Password = ValidPassword };

        // Act
        var result = await _authService.LoginAsync(request);

        // Assert
        var handler  = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(result.Token);

        jwtToken.ValidTo.Should().BeAfter(DateTime.UtcNow,
            "el token no debe estar ya expirado al emitirse");
        jwtToken.ValidTo.Should().BeBefore(DateTime.UtcNow.AddHours(2),
            "el token no debe tener una vigencia excesivamente larga");
    }

    [Fact]
    [Trait("Sprint", "01")]
    [Trait("Funcionalidad", "Iniciar Sesión")]
    [Trait("Tipo", "Seguridad")]
    public async Task ValidateUser_ConCredencialesValidas_RetornaTrue()
    {
        // Act
        var resultado = await _authService.ValidateUserAsync(ValidEmail, ValidPassword);

        // Assert
        resultado.Should().BeTrue();
    }

    [Fact]
    [Trait("Sprint", "01")]
    [Trait("Funcionalidad", "Iniciar Sesión")]
    [Trait("Tipo", "Seguridad")]
    public async Task ValidateUser_ConCredencialesInvalidas_RetornaFalse()
    {
        // Act
        var resultado = await _authService.ValidateUserAsync(ValidEmail, "ContraseñaMala!");

        // Assert
        resultado.Should().BeFalse();
    }

    // ════════════════════════════════════════════════════════════════════════
    // HELPERS PRIVADOS
    // ════════════════════════════════════════════════════════════════════════

    private void SeedValidUser()
    {
        var user = new User
        {
            Email     = ValidEmail,
            Password  = BCrypt.Net.BCrypt.HashPassword(ValidPassword),
            CreatedAt = DateTime.UtcNow
        };
        _context.Users.Add(user);
        _context.SaveChanges();
    }

    public void Dispose() => _context.Dispose();
}