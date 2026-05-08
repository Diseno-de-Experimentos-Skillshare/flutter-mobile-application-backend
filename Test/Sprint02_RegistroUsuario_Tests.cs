using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using SkillShareBackend.Data;
using SkillShareBackend.Models;
using SkillShareBackend.Services;
using Xunit;

namespace SkillShareBackend.Tests;

/// <summary>
/// SPRINT 02 - Registro de Usuario
/// Verifica el flujo completo de creación de cuenta: validaciones,
/// unicidad de email, hash de contraseña y persistencia en BD.
/// </summary>
public class Sprint02_RegistroUsuario_Tests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly AuthService  _authService;

    public Sprint02_RegistroUsuario_Tests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new AppDbContext(options);

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"]               = "SuperSecretKeyForTestingPurposes1234!",
                ["Jwt:Issuer"]            = "SkillShareTest",
                ["Jwt:Audience"]          = "SkillShareTestUsers",
                ["Jwt:DurationInMinutes"] = "60"
            })
            .Build();

        var logger = new Mock<ILogger<AuthService>>().Object;
        _authService = new AuthService(_context, config, logger);
    }

    // ════════════════════════════════════════════════════════════════════════
    // CASOS EXITOSOS
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    [Trait("Sprint", "02")]
    [Trait("Funcionalidad", "Registro de Usuario")]
    [Trait("Tipo", "Happy Path")]
    public async Task Register_ConDatosValidos_CreaUsuarioEnBaseDeDatos()
    {
        // Arrange
        var nuevoUsuario = CrearUsuarioPrueba("nuevo@skillshare.com", "SecurePass123!");

        // Act
        var resultado = await _authService.RegisterAsync(nuevoUsuario);

        // Assert
        resultado.Should().NotBeNull();
        resultado.UserId.Should().BeGreaterThan(0, "la BD debe asignar un ID auto-incremental");

        var enBD = await _context.Users.FindAsync(resultado.UserId);
        enBD.Should().NotBeNull("el usuario debe haberse persistido en la base de datos");
        enBD!.Email.Should().Be("nuevo@skillshare.com");
    }

    [Fact]
    [Trait("Sprint", "02")]
    [Trait("Funcionalidad", "Registro de Usuario")]
    [Trait("Tipo", "Seguridad")]
    public async Task Register_ConDatosValidos_AlmacenaPasswordHasheada()
    {
        // Arrange
        const string passwordOriginal = "MiPasswordSegura456!";
        var usuario = CrearUsuarioPrueba("hash@skillshare.com", passwordOriginal);

        // Act
        await _authService.RegisterAsync(usuario);

        // Assert
        var enBD = await _context.Users.FirstAsync(u => u.Email == "hash@skillshare.com");
        enBD.Password.Should().NotBe(passwordOriginal,
            "la contraseña NUNCA debe almacenarse en texto plano");
        BCrypt.Net.BCrypt.Verify(passwordOriginal, enBD.Password).Should().BeTrue(
            "el hash debe verificarse correctamente con la contraseña original");
    }

    [Fact]
    [Trait("Sprint", "02")]
    [Trait("Funcionalidad", "Registro de Usuario")]
    [Trait("Tipo", "Happy Path")]
    public async Task Register_ConDatosValidos_AsignaFechaCreacionUtc()
    {
        // Arrange
        var antes   = DateTime.UtcNow.AddSeconds(-1);
        var usuario = CrearUsuarioPrueba("fecha@skillshare.com", "Pass12345!");

        // Act
        var resultado = await _authService.RegisterAsync(usuario);

        // Assert
        resultado.CreatedAt.Should().BeAfter(antes,
            "la fecha de creación debe establecerse en el momento del registro");
        resultado.CreatedAt.Should().BeBefore(DateTime.UtcNow.AddSeconds(5));
    }

    // ════════════════════════════════════════════════════════════════════════
    // CASOS NEGATIVOS — DUPLICADOS Y RESTRICCIONES
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    [Trait("Sprint", "02")]
    [Trait("Funcionalidad", "Registro de Usuario")]
    [Trait("Tipo", "Negativo")]
    public async Task Register_ConEmailDuplicado_LanzaInvalidOperationException()
    {
        // Arrange
        var usuario1 = CrearUsuarioPrueba("duplicado@skillshare.com", "Pass1234!");
        var usuario2 = CrearUsuarioPrueba("duplicado@skillshare.com", "OtroPass99!");
        await _authService.RegisterAsync(usuario1);

        // Act & Assert
        var accion = () => _authService.RegisterAsync(usuario2);
        await accion.Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage("Email already exists",
                "el sistema no debe permitir dos cuentas con el mismo email");
    }

    [Fact]
    [Trait("Sprint", "02")]
    [Trait("Funcionalidad", "Registro de Usuario")]
    [Trait("Tipo", "Negativo")]
    public async Task Register_ConEmailDuplicado_NoIncrementaConteoDeUsuarios()
    {
        // Arrange
        var usuario1 = CrearUsuarioPrueba("unico@skillshare.com", "Pass1234!");
        var usuario2 = CrearUsuarioPrueba("unico@skillshare.com", "OtroPass5678!");
        await _authService.RegisterAsync(usuario1);

        // Act — el segundo registro debe fallar
        try { await _authService.RegisterAsync(usuario2); } catch { /* esperado */ }

        // Assert
        var totalUsuarios = await _context.Users.CountAsync();
        totalUsuarios.Should().Be(1, "el usuario duplicado no debe persistirse");
    }

    // ════════════════════════════════════════════════════════════════════════
    // CASOS DE BORDE
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    [Trait("Sprint", "02")]
    [Trait("Funcionalidad", "Registro de Usuario")]
    [Trait("Tipo", "Borde")]
    public async Task Register_ConEmailEnMayusculas_SeAlmacenaComoSeRecibe()
    {
        // Arrange
        var usuario = CrearUsuarioPrueba("MAYUSCULAS@SKILLSHARE.COM", "Pass1234!");

        // Act
        var resultado = await _authService.RegisterAsync(usuario);

        // Assert — el sistema guarda el email tal cual llega
        resultado.Email.Should().Be("MAYUSCULAS@SKILLSHARE.COM");
    }

    [Fact]
    [Trait("Sprint", "02")]
    [Trait("Funcionalidad", "Registro de Usuario")]
    [Trait("Tipo", "Borde")]
    public async Task Register_MultipleUsuariosDistintos_CadaUnoTieneIdUnico()
    {
        // Arrange & Act
        var u1 = await _authService.RegisterAsync(CrearUsuarioPrueba("user1@test.com", "Pass11!"));
        var u2 = await _authService.RegisterAsync(CrearUsuarioPrueba("user2@test.com", "Pass22!"));
        var u3 = await _authService.RegisterAsync(CrearUsuarioPrueba("user3@test.com", "Pass33!"));

        // Assert
        new[] { u1.UserId, u2.UserId, u3.UserId }
            .Should().OnlyHaveUniqueItems("cada usuario debe tener un ID único");
    }

    [Fact]
    [Trait("Sprint", "02")]
    [Trait("Funcionalidad", "Registro de Usuario")]
    [Trait("Tipo", "Borde")]
    public async Task Register_PasswordConCaracteresEspeciales_SeHasheaCorrectamente()
    {
        // Arrange
        const string passwordEspecial = "P@$$w0rd!#%&*()";
        var usuario = CrearUsuarioPrueba("especial@test.com", passwordEspecial);

        // Act
        await _authService.RegisterAsync(usuario);

        // Assert
        var enBD = await _context.Users.FirstAsync(u => u.Email == "especial@test.com");
        BCrypt.Net.BCrypt.Verify(passwordEspecial, enBD.Password).Should().BeTrue(
            "los caracteres especiales en la contraseña deben manejarse correctamente");
    }

    [Fact]
    [Trait("Sprint", "02")]
    [Trait("Funcionalidad", "Registro de Usuario")]
    [Trait("Tipo", "Integración")]
    public async Task Register_LuegoLogin_FlujoCompletoExitoso()
    {
        // Arrange
        const string email    = "flujo@skillshare.com";
        const string password = "FlujoCorrecto123!";
        var usuario = CrearUsuarioPrueba(email, password);

        // Act — registrar y luego iniciar sesión
        await _authService.RegisterAsync(usuario);
        var loginResult = await _authService.LoginAsync(
            new DTOs.LoginRequestDto { Email = email, Password = password });

        // Assert
        loginResult.Success.Should().BeTrue("después del registro debe ser posible iniciar sesión");
        loginResult.Token.Should().NotBeNullOrWhiteSpace();
    }

    // ════════════════════════════════════════════════════════════════════════
    // HELPERS
    // ════════════════════════════════════════════════════════════════════════

    private static User CrearUsuarioPrueba(string email, string password) => new()
    {
        Email    = email,
        Password = password   // AuthService se encarga del hash
    };

    public void Dispose() => _context.Dispose();
}