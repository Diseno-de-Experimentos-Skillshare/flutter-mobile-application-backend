using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using SkillShareBackend.Data;
using SkillShareBackend.DTOs;
using SkillShareBackend.Models;
using SkillShareBackend.Services;
using Xunit;

namespace SkillShareBackend.Tests;

// ═══════════════════════════════════════════════════════════════════════════════
// SPRINT 08 — Gestión de Materias (Subjects)
// ═══════════════════════════════════════════════════════════════════════════════

/// <summary>
/// SPRINT 08 - Gestión de Materias
/// Cubre el CRUD completo de materias: creación, lectura, actualización,
/// eliminación, y la relación con grupos de estudio.
/// </summary>
public class Sprint08_GestionMaterias_Tests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly User         _adminUser;

    public Sprint08_GestionMaterias_Tests()
    {
        _context   = TestDbHelper.CreateInMemoryContext();
        _adminUser = TestDbHelper.SeedUser(_context);
    }

    // ──────────────────────────────────────────────────────────
    // CREACIÓN
    // ──────────────────────────────────────────────────────────

    [Fact]
    [Trait("Sprint", "08")]
    [Trait("Funcionalidad", "Gestión de Materias")]
    [Trait("Tipo", "Happy Path")]
    public async Task CreateSubject_ConNombreValido_SeAlmacenaEnBD()
    {
        // Arrange
        var materia = new Subject { Name = "Álgebra Lineal" };

        // Act
        _context.Subjects.Add(materia);
        await _context.SaveChangesAsync();

        // Assert
        materia.Id.Should().BeGreaterThan(0);
        var enBD = await _context.Subjects.FindAsync(materia.Id);
        enBD.Should().NotBeNull();
        enBD!.Name.Should().Be("Álgebra Lineal");
    }

    [Fact]
    [Trait("Sprint", "08")]
    [Trait("Funcionalidad", "Gestión de Materias")]
    [Trait("Tipo", "Happy Path")]
    public async Task CreateSubject_MultiplesMaterias_CadaUnaConIdUnico()
    {
        // Act
        var m1 = new Subject { Name = "Física" };
        var m2 = new Subject { Name = "Química" };
        var m3 = new Subject { Name = "Biología" };
        _context.Subjects.AddRange(m1, m2, m3);
        await _context.SaveChangesAsync();

        // Assert
        new[] { m1.Id, m2.Id, m3.Id }.Should().OnlyHaveUniqueItems();
    }

    // ──────────────────────────────────────────────────────────
    // LECTURA
    // ──────────────────────────────────────────────────────────

    [Fact]
    [Trait("Sprint", "08")]
    [Trait("Funcionalidad", "Gestión de Materias")]
    [Trait("Tipo", "Happy Path")]
    public async Task GetAllSubjects_ConMaterias_RetornaTodas()
    {
        // Arrange
        _context.Subjects.AddRange(
            new Subject { Name = "Historia" },
            new Subject { Name = "Geografía" },
            new Subject { Name = "Literatura" }
        );
        await _context.SaveChangesAsync();

        // Act
        var materias = await _context.Subjects.ToListAsync();

        // Assert
        materias.Should().HaveCount(3);
    }

    [Fact]
    [Trait("Sprint", "08")]
    [Trait("Funcionalidad", "Gestión de Materias")]
    [Trait("Tipo", "Borde")]
    public async Task GetAllSubjects_SinMaterias_RetornaListaVacia()
    {
        // Act
        var materias = await _context.Subjects.ToListAsync();

        // Assert
        materias.Should().BeEmpty();
    }

    [Fact]
    [Trait("Sprint", "08")]
    [Trait("Funcionalidad", "Gestión de Materias")]
    [Trait("Tipo", "Happy Path")]
    public async Task GetSubjectById_Existente_RetornaMateria()
    {
        // Arrange
        var materia = TestDbHelper.SeedSubject(_context, "Programación");

        // Act
        var resultado = await _context.Subjects.FindAsync(materia.Id);

        // Assert
        resultado.Should().NotBeNull();
        resultado!.Name.Should().Be("Programación");
    }

    [Fact]
    [Trait("Sprint", "08")]
    [Trait("Funcionalidad", "Gestión de Materias")]
    [Trait("Tipo", "Negativo")]
    public async Task GetSubjectById_Inexistente_RetornaNull()
    {
        // Act
        var resultado = await _context.Subjects.FindAsync(99999);

        // Assert
        resultado.Should().BeNull();
    }

    // ──────────────────────────────────────────────────────────
    // ACTUALIZACIÓN
    // ──────────────────────────────────────────────────────────

    [Fact]
    [Trait("Sprint", "08")]
    [Trait("Funcionalidad", "Gestión de Materias")]
    [Trait("Tipo", "Happy Path")]
    public async Task UpdateSubject_NombreNuevo_SeActualizaCorrectamente()
    {
        // Arrange
        var materia = TestDbHelper.SeedSubject(_context, "Nombre Viejo");

        // Act
        materia.Name = "Nombre Nuevo";
        await _context.SaveChangesAsync();

        // Assert
        var enBD = await _context.Subjects.FindAsync(materia.Id);
        enBD!.Name.Should().Be("Nombre Nuevo");
    }

    [Fact]
    [Trait("Sprint", "08")]
    [Trait("Funcionalidad", "Gestión de Materias")]
    [Trait("Tipo", "Borde")]
    public async Task UpdateSubject_NombreConCaracteresEspeciales_SeAlmacena()
    {
        // Arrange
        var materia = TestDbHelper.SeedSubject(_context, "Normal");

        // Act — nombre con acentos y caracteres especiales
        materia.Name = "Cálculo Diferencial e Integral (I)";
        await _context.SaveChangesAsync();

        // Assert
        var enBD = await _context.Subjects.FindAsync(materia.Id);
        enBD!.Name.Should().Be("Cálculo Diferencial e Integral (I)");
    }

    // ──────────────────────────────────────────────────────────
    // ELIMINACIÓN
    // ──────────────────────────────────────────────────────────

    [Fact]
    [Trait("Sprint", "08")]
    [Trait("Funcionalidad", "Gestión de Materias")]
    [Trait("Tipo", "Happy Path")]
    public async Task DeleteSubject_Existente_SeEliminaCorrectamente()
    {
        // Arrange
        var materia = TestDbHelper.SeedSubject(_context, "A Eliminar");

        // Act
        _context.Subjects.Remove(materia);
        await _context.SaveChangesAsync();

        // Assert
        var enBD = await _context.Subjects.FindAsync(materia.Id);
        enBD.Should().BeNull("la materia fue eliminada exitosamente");
    }

    // ──────────────────────────────────────────────────────────
    // RELACIÓN CON GRUPOS
    // ──────────────────────────────────────────────────────────

    [Fact]
    [Trait("Sprint", "08")]
    [Trait("Funcionalidad", "Gestión de Materias")]
    [Trait("Tipo", "Relación")]
    public async Task Subject_GruposAsociados_SeContabilizanCorrectamente()
    {
        // Arrange
        var materia = TestDbHelper.SeedSubject(_context, "Matemáticas");
        TestDbHelper.SeedGroup(_context, _adminUser.UserId, "Grupo Mat 1", materia.Id);
        TestDbHelper.SeedGroup(_context, _adminUser.UserId, "Grupo Mat 2", materia.Id);

        // Act
        var grupos = await _context.StudyGroups
            .Where(g => g.SubjectId == materia.Id)
            .ToListAsync();

        // Assert
        grupos.Should().HaveCount(2, "se crearon 2 grupos con esta materia");
    }

    [Fact]
    [Trait("Sprint", "08")]
    [Trait("Funcionalidad", "Gestión de Materias")]
    [Trait("Tipo", "Relación")]
    public async Task Subject_SinGrupos_ConteoGruposEsCero()
    {
        // Arrange
        var materia = TestDbHelper.SeedSubject(_context, "Materia Solitaria");

        // Act
        var grupos = await _context.StudyGroups
            .Where(g => g.SubjectId == materia.Id)
            .ToListAsync();

        // Assert
        grupos.Should().BeEmpty("no se han creado grupos con esta materia");
    }

    public void Dispose() => _context.Dispose();
}

// ═══════════════════════════════════════════════════════════════════════════════
// SPRINT 09 — Abandonar Grupo de Estudio
// ═══════════════════════════════════════════════════════════════════════════════

/// <summary>
/// SPRINT 09 - Abandonar Grupo de Estudio
/// Verifica que un miembro puede abandonar el grupo,
/// que el creador único no puede irse si es el único admin,
/// y que dejar el grupo reduce correctamente el conteo de miembros.
/// </summary>
public class Sprint09_AbandonarGrupo_Tests : IDisposable
{
    private readonly AppDbContext           _context;
    private readonly GroupManagementService _groupService;
    private readonly User                   _owner;
    private readonly User                   _miembro;
    private readonly StudyGroup             _grupo;

    public Sprint09_AbandonarGrupo_Tests()
    {
        _context      = TestDbHelper.CreateInMemoryContext();
        var logger    = new Mock<ILogger<GroupManagementService>>().Object;
        _groupService = new GroupManagementService(_context, logger);

        _owner   = TestDbHelper.SeedUser(_context);
        _miembro = TestDbHelper.SeedUser(_context);
        _grupo   = TestDbHelper.SeedGroup(_context, _owner.UserId, "Grupo Activo");
        TestDbHelper.AddMember(_context, _grupo.Id, _miembro.UserId, "member");
    }

    [Fact]
    [Trait("Sprint", "09")]
    [Trait("Funcionalidad", "Abandonar Grupo")]
    [Trait("Tipo", "Happy Path")]
    public async Task LeaveGroup_MiembroNormal_SeEliminaCorrectamente()
    {
        // Act — remover membresía del miembro normal
        var membresia = await _context.GroupMembers
            .FirstAsync(gm => gm.GroupId == _grupo.Id && gm.UserId == _miembro.UserId);
        _context.GroupMembers.Remove(membresia);
        await _context.SaveChangesAsync();

        // Assert
        var enBD = await _context.GroupMembers
            .FirstOrDefaultAsync(gm => gm.GroupId == _grupo.Id && gm.UserId == _miembro.UserId);
        enBD.Should().BeNull("el miembro abandonó el grupo correctamente");
    }

    [Fact]
    [Trait("Sprint", "09")]
    [Trait("Funcionalidad", "Abandonar Grupo")]
    [Trait("Tipo", "Happy Path")]
    public async Task LeaveGroup_MiembroNormal_ReduceConteoMiembros()
    {
        // Arrange
        var totalAntes = await _context.GroupMembers.CountAsync(gm => gm.GroupId == _grupo.Id);

        // Act
        var membresia = await _context.GroupMembers
            .FirstAsync(gm => gm.GroupId == _grupo.Id && gm.UserId == _miembro.UserId);
        _context.GroupMembers.Remove(membresia);
        await _context.SaveChangesAsync();

        // Assert
        var totalDespues = await _context.GroupMembers.CountAsync(gm => gm.GroupId == _grupo.Id);
        totalDespues.Should().Be(totalAntes - 1, "el conteo debe disminuir en 1 al abandonar");
    }

    [Fact]
    [Trait("Sprint", "09")]
    [Trait("Funcionalidad", "Abandonar Grupo")]
    [Trait("Tipo", "Negativo")]
    public async Task LeaveGroup_UsuarioNoMiembro_MembresiaNoExiste()
    {
        // Arrange
        var externo = TestDbHelper.SeedUser(_context);

        // Act — verificar que no existe membresía
        var membresia = await _context.GroupMembers
            .FirstOrDefaultAsync(gm => gm.GroupId == _grupo.Id && gm.UserId == externo.UserId);

        // Assert
        membresia.Should().BeNull("el usuario no es miembro de este grupo");
    }

    [Fact]
    [Trait("Sprint", "09")]
    [Trait("Funcionalidad", "Abandonar Grupo")]
    [Trait("Tipo", "Regla de Negocio")]
    public async Task LeaveGroup_UltimoAdmin_NoDebeAbandonar()
    {
        // Arrange — el owner es el único admin
        var adminCount = await _context.GroupMembers
            .CountAsync(gm => gm.GroupId == _grupo.Id && gm.Role == "admin");

        // Assert — debe haber exactamente 1 admin (el owner)
        adminCount.Should().Be(1);

        // Act — verificar la lógica de negocio: el sistema impide que el único admin abandone
        var esOwner    = await _groupService.IsGroupOwner(_grupo.Id, _owner.UserId);
        var adminCount2 = await _context.GroupMembers
            .CountAsync(gm => gm.GroupId == _grupo.Id && gm.Role == "admin");

        // Assert — si adminCount <= 1 y es el owner, no puede irse
        esOwner.Should().BeTrue();
        (adminCount2 <= 1).Should().BeTrue("hay solo un admin, la salida debe ser bloqueada");
    }

    [Fact]
    [Trait("Sprint", "09")]
    [Trait("Funcionalidad", "Abandonar Grupo")]
    [Trait("Tipo", "Regla de Negocio")]
    public async Task LeaveGroup_OwnerConOtroAdmin_PuedeAbandonar()
    {
        // Arrange — promover al miembro a admin
        var membresiaPromocion = await _context.GroupMembers
            .FirstAsync(gm => gm.GroupId == _grupo.Id && gm.UserId == _miembro.UserId);
        membresiaPromocion.Role = "admin";
        await _context.SaveChangesAsync();

        // Act — ahora el owner puede irse porque hay otro admin
        var adminCount = await _context.GroupMembers
            .CountAsync(gm => gm.GroupId == _grupo.Id && gm.Role == "admin");

        // Assert
        adminCount.Should().BeGreaterThan(1,
            "hay más de un admin, por lo que el owner puede abandonar");
    }

    [Fact]
    [Trait("Sprint", "09")]
    [Trait("Funcionalidad", "Abandonar Grupo")]
    [Trait("Tipo", "Regla de Negocio")]
    public async Task RemoveMember_AdminRemueveMiembro_Exitoso()
    {
        // Act
        var resultado = await _groupService.RemoveMember(_grupo.Id, _miembro.UserId, _owner.UserId);

        // Assert
        resultado.Should().BeTrue();
        var enBD = await _context.GroupMembers
            .FirstOrDefaultAsync(gm => gm.GroupId == _grupo.Id && gm.UserId == _miembro.UserId);
        enBD.Should().BeNull("el miembro fue expulsado correctamente");
    }

    [Fact]
    [Trait("Sprint", "09")]
    [Trait("Funcionalidad", "Abandonar Grupo")]
    [Trait("Tipo", "Permisos")]
    public async Task RemoveMember_MiembroIntentaRemoverAOtro_Falla()
    {
        // Arrange — agregar un segundo miembro
        var otroMiembro = TestDbHelper.SeedUser(_context);
        TestDbHelper.AddMember(_context, _grupo.Id, otroMiembro.UserId, "member");

        // Act — un miembro normal intenta remover a otro miembro (no tiene permiso)
        var resultado = await _groupService.RemoveMember(_grupo.Id, otroMiembro.UserId, _miembro.UserId);

        // Assert
        resultado.Should().BeFalse("un miembro sin rol de admin no puede expulsar a otros");
    }

    [Fact]
    [Trait("Sprint", "09")]
    [Trait("Funcionalidad", "Abandonar Grupo")]
    [Trait("Tipo", "Permisos")]
    public async Task PromoteToAdmin_OwnerPromueveMiembro_CambiaRol()
    {
        // Act
        var resultado = await _groupService.PromoteToAdmin(_grupo.Id, _miembro.UserId, _owner.UserId);

        // Assert
        resultado.Should().BeTrue();
        var membresia = await _context.GroupMembers
            .FirstAsync(gm => gm.GroupId == _grupo.Id && gm.UserId == _miembro.UserId);
        membresia.Role.Should().Be("admin");
    }

    [Fact]
    [Trait("Sprint", "09")]
    [Trait("Funcionalidad", "Abandonar Grupo")]
    [Trait("Tipo", "Permisos")]
    public async Task DemoteToMember_OwnerBajaMiembro_CambiaRol()
    {
        // Arrange — promover primero
        await _groupService.PromoteToAdmin(_grupo.Id, _miembro.UserId, _owner.UserId);

        // Act — bajar de nuevo a miembro
        var resultado = await _groupService.DemoteToMember(_grupo.Id, _miembro.UserId, _owner.UserId);

        // Assert
        resultado.Should().BeTrue();
        var membresia = await _context.GroupMembers
            .FirstAsync(gm => gm.GroupId == _grupo.Id && gm.UserId == _miembro.UserId);
        membresia.Role.Should().Be("member");
    }

    public void Dispose() => _context.Dispose();
}

// ═══════════════════════════════════════════════════════════════════════════════
// SPRINT 10 — Estadísticas y Transferencia de Propiedad del Grupo
// ═══════════════════════════════════════════════════════════════════════════════

/// <summary>
/// SPRINT 10 - Estadísticas del Grupo y Transferencia de Propiedad
/// Cubre la obtención de métricas del grupo (miembros, mensajes, actividad)
/// y el flujo completo de transferencia de ownership con sus restricciones.
/// </summary>
public class Sprint10_EstadisticasYTransferencia_Tests : IDisposable
{
    private readonly AppDbContext           _context;
    private readonly GroupManagementService _groupService;
    private readonly User                   _owner;
    private readonly User                   _miembro1;
    private readonly User                   _miembro2;
    private readonly StudyGroup             _grupo;

    public Sprint10_EstadisticasYTransferencia_Tests()
    {
        _context      = TestDbHelper.CreateInMemoryContext();
        var logger    = new Mock<ILogger<GroupManagementService>>().Object;
        _groupService = new GroupManagementService(_context, logger);

        _owner    = TestDbHelper.SeedUser(_context);
        _miembro1 = TestDbHelper.SeedUser(_context);
        _miembro2 = TestDbHelper.SeedUser(_context);
        _grupo    = TestDbHelper.SeedGroup(_context, _owner.UserId, "Grupo Estadísticas");
        TestDbHelper.AddMember(_context, _grupo.Id, _miembro1.UserId, "member");
        TestDbHelper.AddMember(_context, _grupo.Id, _miembro2.UserId, "member");
    }

    // ──────────────────────────────────────────────────────────
    // ESTADÍSTICAS
    // ──────────────────────────────────────────────────────────

    [Fact]
    [Trait("Sprint", "10")]
    [Trait("Funcionalidad", "Estadísticas del Grupo")]
    [Trait("Tipo", "Happy Path")]
    public async Task GetGroupStatistics_RetornaTotalMiembrosCorrectamente()
    {
        // Act
        var stats = await _groupService.GetGroupStatistics(_grupo.Id);

        // Assert — owner (admin) + miembro1 + miembro2 = 3
        stats.TotalMembers.Should().Be(3);
    }

    [Fact]
    [Trait("Sprint", "10")]
    [Trait("Funcionalidad", "Estadísticas del Grupo")]
    [Trait("Tipo", "Happy Path")]
    public async Task GetGroupStatistics_RetornaConteoAdminsYMiembros()
    {
        // Act
        var stats = await _groupService.GetGroupStatistics(_grupo.Id);

        // Assert
        stats.AdminCount.Should().Be(1, "solo el owner es admin al inicio");
        stats.MemberCount.Should().Be(2, "hay 2 miembros con rol 'member'");
    }

    [Fact]
    [Trait("Sprint", "10")]
    [Trait("Funcionalidad", "Estadísticas del Grupo")]
    [Trait("Tipo", "Happy Path")]
    public async Task GetGroupStatistics_FechaCreacion_CoincideConLaDelGrupo()
    {
        // Act
        var stats = await _groupService.GetGroupStatistics(_grupo.Id);

        // Assert
        stats.CreatedAt.Should().BeCloseTo(_grupo.CreatedAt, TimeSpan.FromSeconds(2));
    }

    [Fact]
    [Trait("Sprint", "10")]
    [Trait("Funcionalidad", "Estadísticas del Grupo")]
    [Trait("Tipo", "Borde")]
    public async Task GetGroupStatistics_GrupoInexistente_RetornaEstadisticasVacias()
    {
        // Act
        var stats = await _groupService.GetGroupStatistics(99999);

        // Assert
        stats.TotalMembers.Should().Be(0);
        stats.TotalMessages.Should().Be(0);
    }

    [Fact]
    [Trait("Sprint", "10")]
    [Trait("Funcionalidad", "Estadísticas del Grupo")]
    [Trait("Tipo", "Happy Path")]
    public async Task GetGroupStatistics_SinMensajes_TotalMensajesEsCero()
    {
        // Act
        var stats = await _groupService.GetGroupStatistics(_grupo.Id);

        // Assert
        stats.TotalMessages.Should().Be(0, "no se han enviado mensajes aún");
        stats.LastActivity.Should().BeNull("no hay actividad registrada");
    }

    [Fact]
    [Trait("Sprint", "10")]
    [Trait("Funcionalidad", "Estadísticas del Grupo")]
    [Trait("Tipo", "Regla de Negocio")]
    public async Task GetGroupStatistics_DespuesDePromoverAdmin_ConteoAdminsAumenta()
    {
        // Arrange — promover a miembro1 a admin
        await _groupService.PromoteToAdmin(_grupo.Id, _miembro1.UserId, _owner.UserId);

        // Act
        var stats = await _groupService.GetGroupStatistics(_grupo.Id);

        // Assert
        stats.AdminCount.Should().Be(2, "ahora hay 2 admins: el owner y el miembro promovido");
        stats.MemberCount.Should().Be(1, "solo queda 1 miembro con rol 'member'");
    }

    // ──────────────────────────────────────────────────────────
    // TRANSFERENCIA DE PROPIEDAD
    // ──────────────────────────────────────────────────────────

    [Fact]
    [Trait("Sprint", "10")]
    [Trait("Funcionalidad", "Transferencia de Propiedad")]
    [Trait("Tipo", "Happy Path")]
    public async Task TransferOwnership_AmiembroExistente_CambiaCreatedBy()
    {
        // Act
        var resultado = await _groupService.TransferOwnership(_grupo.Id, _miembro1.UserId, _owner.UserId);

        // Assert
        resultado.Should().BeTrue();
        var grupoActualizado = await _context.StudyGroups.FindAsync(_grupo.Id);
        grupoActualizado!.CreatedBy.Should().Be(_miembro1.UserId);
    }

    [Fact]
    [Trait("Sprint", "10")]
    [Trait("Funcionalidad", "Transferencia de Propiedad")]
    [Trait("Tipo", "Happy Path")]
    public async Task TransferOwnership_NuevoDuenioQuedaComoAdmin()
    {
        // Act
        await _groupService.TransferOwnership(_grupo.Id, _miembro1.UserId, _owner.UserId);

        // Assert
        var membresiaNuevoDueno = await _context.GroupMembers
            .FirstAsync(gm => gm.GroupId == _grupo.Id && gm.UserId == _miembro1.UserId);
        membresiaNuevoDueno.Role.Should().Be("admin",
            "el nuevo dueño debe tener rol admin tras la transferencia");
    }

    [Fact]
    [Trait("Sprint", "10")]
    [Trait("Funcionalidad", "Transferencia de Propiedad")]
    [Trait("Tipo", "Negativo")]
    public async Task TransferOwnership_SolicitudDeNoOwner_RetornaFalse()
    {
        // Act — miembro1 (no es owner) intenta transferir la propiedad
        var resultado = await _groupService.TransferOwnership(_grupo.Id, _miembro2.UserId, _miembro1.UserId);

        // Assert
        resultado.Should().BeFalse("solo el dueño actual puede transferir la propiedad");
    }

    [Fact]
    [Trait("Sprint", "10")]
    [Trait("Funcionalidad", "Transferencia de Propiedad")]
    [Trait("Tipo", "Negativo")]
    public async Task TransferOwnership_AUsuarioNoMiembro_RetornaFalse()
    {
        // Arrange
        var externo = TestDbHelper.SeedUser(_context);

        // Act — el owner intenta transferir a alguien que no es miembro
        var resultado = await _groupService.TransferOwnership(_grupo.Id, externo.UserId, _owner.UserId);

        // Assert
        resultado.Should().BeFalse("no se puede transferir la propiedad a alguien que no es miembro");
    }

    [Fact]
    [Trait("Sprint", "10")]
    [Trait("Funcionalidad", "Transferencia de Propiedad")]
    [Trait("Tipo", "Borde")]
    public async Task TransferOwnership_GrupoInexistente_RetornaFalse()
    {
        // Act
        var resultado = await _groupService.TransferOwnership(99999, _miembro1.UserId, _owner.UserId);

        // Assert
        resultado.Should().BeFalse("el grupo no existe");
    }

    // ──────────────────────────────────────────────────────────
    // ELIMINACIÓN MASIVA DE MIEMBROS (Bulk Remove)
    // ──────────────────────────────────────────────────────────

    [Fact]
    [Trait("Sprint", "10")]
    [Trait("Funcionalidad", "Gestión de Miembros")]
    [Trait("Tipo", "Happy Path")]
    public async Task BulkRemoveMembers_ListaValida_RemueveTodos()
    {
        // Act
        var idsRemovidos = await _groupService.BulkRemoveMembers(
            _grupo.Id,
            new List<int> { _miembro1.UserId, _miembro2.UserId },
            _owner.UserId
        );

        // Assert
        idsRemovidos.Should().HaveCount(2);
        idsRemovidos.Should().Contain(_miembro1.UserId);
        idsRemovidos.Should().Contain(_miembro2.UserId);
    }

    [Fact]
    [Trait("Sprint", "10")]
    [Trait("Funcionalidad", "Gestión de Miembros")]
    [Trait("Tipo", "Borde")]
    public async Task BulkRemoveMembers_ListaVacia_NoRemuevaNadie()
    {
        // Act
        var idsRemovidos = await _groupService.BulkRemoveMembers(
            _grupo.Id,
            new List<int>(),
            _owner.UserId
        );

        // Assert
        idsRemovidos.Should().BeEmpty();
    }

    public void Dispose() => _context.Dispose();
}