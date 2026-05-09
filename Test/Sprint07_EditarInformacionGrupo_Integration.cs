using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using SkillShareBackend.Data;
using SkillShareBackend.Models;
using SkillShareBackend.Services;
using Xunit;

namespace SkillShareBackend.Tests;

/// <summary>
/// SPRINT 07 - Editar Información Grupo
/// Verifica actualización de información del grupo:
/// persistencia, integridad y mantenimiento de propiedad.
/// </summary>
public class S07_EditarInformacionGrupo_Tests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly GroupManagementService _groupService;

    private readonly User _owner;
    private readonly User _member;
    private readonly StudyGroup _group;

    public S07_EditarInformacionGrupo_Tests()
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

        _group = TestDbHelper.SeedGroup(
            _context,
            _owner.UserId,
            "Grupo Matemáticas"
        );
    }

    [Fact]
    [Trait("Sprint", "07")]
    [Trait("Funcionalidad", "Editar Información Grupo")]
    [Trait("Tipo", "Happy Path")]
    public async Task EditGroup_CambiarNombre_SePersisteCorrectamente()
    {
        // Arrange
        var nuevoNombre = "Grupo Física";

        // Act
        _group.Name = nuevoNombre;

        await _context.SaveChangesAsync();

        // Assert
        var grupoActualizado = await _context
            .StudyGroups
            .FindAsync(_group.Id);

        grupoActualizado.Should().NotBeNull();

        grupoActualizado!.Name.Should().Be(nuevoNombre);
    }

    [Fact]
    [Trait("Sprint", "07")]
    [Trait("Funcionalidad", "Editar Información Grupo")]
    [Trait("Tipo", "Happy Path")]
    public async Task EditGroup_CambiarDescripcion_SeActualizaCorrectamente()
    {
        // Arrange
        var nuevaDescripcion =
            "Grupo orientado a ejercicios avanzados de física.";

        // Act
        _group.Description = nuevaDescripcion;

        await _context.SaveChangesAsync();

        // Assert
        var grupoActualizado = await _context
            .StudyGroups
            .FindAsync(_group.Id);

        grupoActualizado.Should().NotBeNull();

        grupoActualizado!.Description.Should().Be(nuevaDescripcion);
    }

    [Fact]
    [Trait("Sprint", "07")]
    [Trait("Funcionalidad", "Editar Información Grupo")]
    [Trait("Tipo", "Permisos")]
    public async Task EditGroup_UsuarioCreador_SigueSiendoOwner()
    {
        // Arrange
        _group.Name = "Grupo Editado";

        await _context.SaveChangesAsync();

        // Act
        var esOwner = await _groupService
            .IsGroupOwner(_group.Id, _owner.UserId);

        // Assert
        esOwner.Should().BeTrue(
            "editar información no debe alterar propiedad del grupo"
        );
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}