using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using SkillShareBackend.Data;
using SkillShareBackend.DTOs;
using SkillShareBackend.Models;
using SkillShareBackend.Services;

namespace SkillShareBackend.Tests;

/// <summary>
/// Crear Grupo de Estudio
/// Cubre la creación de grupos, validaciones de datos,
/// gestión de miembros y roles.
/// Incluye 4 pruebas unitarias y 2 pruebas integrales.
/// </summary>
[TestFixture]
public class CrearGrupoEstudio_Tests
{
    private const string TEST_PASSWORD = "12345678"; 
    private AppDbContext _context = null!;
    private GroupManagementService _groupManagementService = null!;
    private int _testUserId;

    [SetUp]
    public void Setup()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new AppDbContext(options);

        var logger = new Mock<ILogger<GroupManagementService>>().Object;
        _groupManagementService = new GroupManagementService(_context, logger);

        _testUserId = SeedUserAndGetId();
    }

    [TearDown]
    public void TearDown()
    {
        _context?.Dispose();
    }

    // ════════════════════════════════════════════════════════════════════════
    // PRUEBAS UNITARIAS - Validaciones y comportamientos básicos
    // ════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Test Unitario 1: Validación de propietario del grupo
    /// Verifica que IsGroupOwner retorna true solo para el creador
    /// </summary>
    [Test]
    [Category("Unitario")]
    [Description("Valida que solo el creador es propietario del grupo")]
    public async Task IsGroupOwner_UsuarioPropietario_RetornaTrue()
    {
        // Arrange
        var grupo = CrearGrupoDeEstudio(_testUserId);
        _context.StudyGroups.Add(grupo);
        await _context.SaveChangesAsync();

        // Act
        var esOwner = await _groupManagementService.IsGroupOwner(grupo.Id, _testUserId);

        // Assert
        Assert.That(esOwner, Is.True, "El creador debe ser propietario");
    }

    /// <summary>
    /// Test Unitario 2: Validación de no propietario
    /// Verifica que IsGroupOwner retorna false para usuarios que no crearon el grupo
    /// </summary>
    [Test]
    [Category("Unitario")]
    [Description("Valida que un usuario no propietario retorna false")]
    public async Task IsGroupOwner_UsuarioNoCreador_RetornaFalse()
    {
        // Arrange
        var grupo = CrearGrupoDeEstudio(_testUserId);
        _context.StudyGroups.Add(grupo);
        await _context.SaveChangesAsync();

        var otroUsuarioId = SeedUserAndGetId();

        // Act
        var esOwner = await _groupManagementService.IsGroupOwner(grupo.Id, otroUsuarioId);

        // Assert
        Assert.That(esOwner, Is.False, "Un usuario que no es creador no debe ser propietario");
    }

    /// <summary>
    /// Test Unitario 3: Verificación de rol de administrador
    /// Verifica que IsGroupAdmin identifica correctamente a los administradores
    /// </summary>
    [Test]
    [Category("Unitario")]
    [Description("Valida identificación de administradores")]
    public async Task IsGroupAdmin_MiembroConRolAdmin_RetornaTrue()
    {
        // Arrange
        var grupo = CrearGrupoDeEstudio(_testUserId);
        _context.StudyGroups.Add(grupo);
        await _context.SaveChangesAsync();

        var miembro = new GroupMember
        {
            GroupId = grupo.Id,
            UserId = _testUserId,
            Role = "member"
        };
        _context.GroupMembers.Add(miembro);
        await _context.SaveChangesAsync();

        // Act
        var esAdmin = await _groupManagementService.IsGroupAdmin(grupo.Id, _testUserId);

        // Assert
        Assert.That(esAdmin, Is.True, "Un miembro con rol admin debe ser identificado");
    }

    /// <summary>
    /// Test Unitario 4: Permisos de un usuario en el grupo
    /// Verifica que GetUserPermissions retorna los permisos correctos
    /// </summary>
    [Test]
    [Category("Unitario")]
    [Description("Valida permisos del propietario del grupo")]
    public async Task GetUserPermissions_Propietario_TieneTodosLosPermisos()
    {
        // Arrange
        var grupo = CrearGrupoDeEstudio(_testUserId);
        _context.StudyGroups.Add(grupo);

        var miembro = new GroupMember
        {
            GroupId = grupo.Id,
            UserId = _testUserId,
            Role = "admin"
        };
        _context.GroupMembers.Add(miembro);
        await _context.SaveChangesAsync();

        // Act
        var permisos = await _groupManagementService.GetUserPermissions(grupo.Id, _testUserId);

        // Assert
        Assert.That(permisos, Is.Not.Null);
        Assert.That(permisos.IsOwner, Is.True, "Debe ser propietario");
        Assert.That(permisos.IsAdmin, Is.True, "Debe ser administrador");
        Assert.That(permisos.IsMember, Is.True, "Debe ser miembro");
        Assert.That(permisos.CanDeleteGroup, Is.True, "Debe poder borrar el grupo");
        Assert.That(permisos.CanTransferOwnership, Is.True, "Debe poder transferir propiedad");
    }

    // ════════════════════════════════════════════════════════════════════════
    // PRUEBAS INTEGRALES - Interacción con base de datos
    // ════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Test Integral 1: Creación y persistencia de grupo
    /// Verifica que un grupo se cree correctamente y se persista en BD
    /// </summary>
    [Test]
    [Category("Integral")]
    [Description("Verifica creación y persistencia de grupo en BD")]
    public async Task CrearGrupo_ConDatos_PersisticaCorrectamente()
    {
        // Arrange
        var grupo = new StudyGroup
        {
            Name = "Grupo de Matemáticas Avanzadas",
            Description = "Grupo para estudiar cálculo y álgebra lineal",
            CreatedBy = _testUserId,
            CreatedAt = DateTime.UtcNow
        };

        // Act
        _context.StudyGroups.Add(grupo);
        await _context.SaveChangesAsync();

        var grupoRecuperado = await _context.StudyGroups
            .FirstOrDefaultAsync(g => g.Id == grupo.Id);

        // Assert
        Assert.That(grupoRecuperado, Is.Not.Null);
        Assert.That(grupoRecuperado!.Name, Is.EqualTo("Grupo de Matemáticas Avanzadas"));
        Assert.That(grupoRecuperado.CreatedBy, Is.EqualTo(_testUserId));
        Assert.That(grupoRecuperado.Description, Is.EqualTo("Grupo para estudiar cálculo y álgebra lineal"));
    }

    /// <summary>
    /// Test Integral 2: Transferencia de propiedad
    /// Verifica que la propiedad se transfiera correctamente entre miembros
    /// </summary>
    [Test]
    [Category("Integral")]
    [Description("Verifica transferencia de propiedad del grupo")]
    public async Task TransferirPropiedad_AUnMiembroExistente_CambiaElPropietario()
    {
        // Arrange
        var nuevoOwner = SeedUserAndGetId();
        var grupo = CrearGrupoDeEstudio(_testUserId);
        _context.StudyGroups.Add(grupo);

        var nuevoMiembro = new GroupMember
        {
            GroupId = grupo.Id,
            UserId = nuevoOwner,
            Role = "member"
        };
        _context.GroupMembers.Add(nuevoMiembro);
        await _context.SaveChangesAsync();

        // Act
        var resultado = await _groupManagementService.TransferOwnership(
            grupo.Id, 
            nuevoOwner, 
            _testUserId
        );

        var grupoActualizado = await _context.StudyGroups.FindAsync(grupo.Id);
        var miembroActualizado = await _context.GroupMembers
            .FirstOrDefaultAsync(m => m.UserId == nuevoOwner && m.GroupId == grupo.Id);

        // Assert
        Assert.That(resultado, Is.True, "La transferencia debe ser exitosa");
        Assert.That(grupoActualizado!.CreatedBy, Is.EqualTo(nuevoOwner), "El nuevo propietario debe estar asignado");
        Assert.That(miembroActualizado!.Role, Is.EqualTo("admin"), "El nuevo propietario debe ser admin");
    }

    // ════════════════════════════════════════════════════════════════════════
    // HELPERS
    // ════════════════════════════════════════════════════════════════════════

    private int SeedUserAndGetId()
    {
        var user = new User
        {
            Email = $"user_{Guid.NewGuid():N}@test.com",
            Password = BCrypt.Net.BCrypt.HashPassword(TEST_PASSWORD),
            CreatedAt = DateTime.UtcNow
        };
        _context.Users.Add(user);
        _context.SaveChanges();
        return user.UserId;
    }

    private StudyGroup CrearGrupoDeEstudio(int createdBy, string nombre = "Grupo Test")
    {
        return new StudyGroup
        {
            Name = nombre,
            Description = "Descripción de prueba",
            CreatedBy = createdBy,
            CreatedAt = DateTime.UtcNow
        };
    }
}
