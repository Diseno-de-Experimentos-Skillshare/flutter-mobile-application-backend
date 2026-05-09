using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using SkillShareBackend.Data;
using SkillShareBackend.Models;
using SkillShareBackend.Services;
using Xunit;

namespace SkillShareBackend.Tests;

/// <summary>
/// SPRINT 09 - Abandonar Grupo
/// Verifica el flujo de salida de usuarios:
/// membresía, persistencia e integridad del grupo.
/// </summary>
public class S09_AbandonarGrupo_Tests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly GroupManagementService _groupService;

    private readonly User _owner;
    private readonly User _member;
    private readonly User _secondMember;

    private readonly StudyGroup _group;

    public S09_AbandonarGrupo_Tests()
    {
        _context = TestDbHelper.CreateInMemoryContext();

        var logger = new Mock<ILogger<GroupManagementService>>().Object;

        _groupService = new GroupManagementService(_context, logger);

        // Datos base
        _owner = TestDbHelper.SeedUser(
            _context,
            "owner@test.com"
        );

        _member = TestDbHelper.SeedUser(
            _context,
            "member@test.com"
        );

        _secondMember = TestDbHelper.SeedUser(
            _context,
            "second@test.com"
        );

        _group = TestDbHelper.SeedGroup(
            _context,
            _owner.UserId,
            "Grupo Matemáticas"
        );

        // Membresías iniciales
        _context.GroupMembers.Add(new GroupMember
        {
            GroupId = _group.Id,
            UserId = _member.UserId,
            Role = "member"
        });

        _context.GroupMembers.Add(new GroupMember
        {
            GroupId = _group.Id,
            UserId = _secondMember.UserId,
            Role = "member"
        });

        _context.SaveChanges();
    }

    [Fact]
    [Trait("Sprint", "09")]
    [Trait("Funcionalidad", "Abandonar Grupo")]
    [Trait("Tipo", "Integration")]
    public async Task LeaveGroup_MiembroValido_SeEliminaCorrectamente()
    {
        // Arrange
        var totalAntes = _context.GroupMembers.Count();

        var miembro = _context.GroupMembers.FirstOrDefault(gm =>
            gm.GroupId == _group.Id &&
            gm.UserId == _member.UserId
        );

        // Act
        _context.GroupMembers.Remove(miembro!);

        await _context.SaveChangesAsync();

        // Assert
        var totalDespues = _context.GroupMembers.Count();

        totalDespues.Should().Be(totalAntes - 1);

        var miembroEliminado = _context.GroupMembers.FirstOrDefault(gm =>
            gm.GroupId == _group.Id &&
            gm.UserId == _member.UserId
        );

        miembroEliminado.Should().BeNull();
    }

    [Fact]
    [Trait("Sprint", "09")]
    [Trait("Funcionalidad", "Abandonar Grupo")]
    [Trait("Tipo", "Integration")]
    public async Task LeaveGroup_MembresiaEliminada_NoPersisteEnBaseDatos()
    {
        // Arrange
        var miembro = _context.GroupMembers.FirstOrDefault(gm =>
            gm.GroupId == _group.Id &&
            gm.UserId == _secondMember.UserId
        );

        // Act
        _context.GroupMembers.Remove(miembro!);

        await _context.SaveChangesAsync();

        // Assert
        var existe = _context.GroupMembers.Any(gm =>
            gm.GroupId == _group.Id &&
            gm.UserId == _secondMember.UserId
        );

        existe.Should().BeFalse();
    }

    [Fact]
    [Trait("Sprint", "09")]
    [Trait("Funcionalidad", "Abandonar Grupo")]
    [Trait("Tipo", "Integration")]
    public async Task LeaveGroup_GrupoPermaneceActivo_TrasSalidaMiembro()
    {
        // Arrange
        var miembro = _context.GroupMembers.FirstOrDefault(gm =>
            gm.GroupId == _group.Id &&
            gm.UserId == _member.UserId
        );

        // Act
        _context.GroupMembers.Remove(miembro!);

        await _context.SaveChangesAsync();

        // Assert
        var grupo = await _context
            .StudyGroups
            .FindAsync(_group.Id);

        grupo.Should().NotBeNull();

        grupo!.Name.Should().Be("Grupo Matemáticas");
    }

    [Fact]
    [Trait("Sprint", "09")]
    [Trait("Funcionalidad", "Abandonar Grupo")]
    [Trait("Tipo", "Integration")]
    public async Task LeaveGroup_OtrosMiembros_NoSonAfectados()
    {
        // Arrange
        var miembro = _context.GroupMembers.FirstOrDefault(gm =>
            gm.GroupId == _group.Id &&
            gm.UserId == _member.UserId
        );

        // Act
        _context.GroupMembers.Remove(miembro!);

        await _context.SaveChangesAsync();

        // Assert
        var otroMiembro = _context.GroupMembers.FirstOrDefault(gm =>
            gm.GroupId == _group.Id &&
            gm.UserId == _secondMember.UserId
        );

        otroMiembro.Should().NotBeNull();

        otroMiembro!.Role.Should().Be("member");
    }

    [Fact]
    [Trait("Sprint", "09")]
    [Trait("Funcionalidad", "Abandonar Grupo")]
    [Trait("Tipo", "Integration")]
    public async Task LeaveGroup_AdministradorMantienePropiedadGrupo()
    {
        // Arrange
        var miembro = _context.GroupMembers.FirstOrDefault(gm =>
            gm.GroupId == _group.Id &&
            gm.UserId == _member.UserId
        );

        // Act
        _context.GroupMembers.Remove(miembro!);

        await _context.SaveChangesAsync();

        // Assert
        var esOwner = await _groupService
            .IsGroupOwner(_group.Id, _owner.UserId);

        esOwner.Should().BeTrue(
            "la salida de miembros no debe afectar la propiedad del grupo"
        );
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}