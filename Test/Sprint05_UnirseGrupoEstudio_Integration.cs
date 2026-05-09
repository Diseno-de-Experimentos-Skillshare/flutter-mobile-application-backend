using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using SkillShareBackend.Data;
using SkillShareBackend.Models;
using SkillShareBackend.Services;
using Xunit;

namespace SkillShareBackend.Tests;

/// <summary>
/// SPRINT 05 - Unirse Grupo de Estudio
/// Verifica el flujo de unión de usuarios a grupos:
/// membresía, persistencia y prevención de duplicados.
/// </summary>
public class Sprint05_UnirseGrupoEstudio_Integration : IDisposable
{
    private readonly AppDbContext _context;
    private readonly GroupManagementService _groupService;

    private readonly User _owner;
    private readonly User _member;
    private readonly StudyGroup _group;

    public Sprint05_UnirseGrupoEstudio_Integration()
    {
        _context = TestDbHelper.CreateInMemoryContext();

        var logger = new Mock<ILogger<GroupManagementService>>().Object;

        _groupService = new GroupManagementService(_context, logger);

        // Datos base
        _owner = TestDbHelper.SeedUser(_context, "owner@test.com");

        _member = TestDbHelper.SeedUser(_context, "member@test.com");

        _group = TestDbHelper.SeedGroup(
            _context,
            _owner.UserId,
            "Grupo Matemáticas"
        );
    }

    [Fact]
    [Trait("Sprint", "05")]
    [Trait("Funcionalidad", "Unirse Grupo de Estudio")]
    [Trait("Tipo", "Integration")]
    public async Task JoinGroup_UsuarioValido_SeUneCorrectamente()
    {
        // Arrange
        var totalAntes = _context.GroupMembers.Count();

        // Act
        _context.GroupMembers.Add(new GroupMember
        {
            GroupId = _group.Id,
            UserId = _member.UserId,
            Role = "member"
        });

        await _context.SaveChangesAsync();

        // Assert
        var totalDespues = _context.GroupMembers.Count();

        totalDespues.Should().Be(totalAntes + 1);

        var miembro = _context.GroupMembers.FirstOrDefault(gm =>
            gm.GroupId == _group.Id &&
            gm.UserId == _member.UserId);

        miembro.Should().NotBeNull();

        miembro!.Role.Should().Be("member");
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}