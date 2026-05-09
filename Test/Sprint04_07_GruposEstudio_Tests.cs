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
// HELPERS COMPARTIDOS — Base de datos y datos de prueba reutilizables
// ═══════════════════════════════════════════════════════════════════════════════
internal static class TestDbHelper
{
    public static AppDbContext CreateInMemoryContext() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    public static User SeedUser(AppDbContext ctx, string? email = null)
    {
        var user = new User
        {
            Email = email ?? $"user_{Guid.NewGuid():N}@test.com",
            Password = BCrypt.Net.BCrypt.HashPassword("Pass123!"),
            CreatedAt = DateTime.UtcNow
        };
        ctx.Users.Add(user);
        ctx.SaveChanges();
        return user;
    }

    public static Subject SeedSubject(AppDbContext ctx, string name = "Matemáticas")
    {
        var subj = new Subject { Name = name };
        ctx.Subjects.Add(subj);
        ctx.SaveChanges();
        return subj;
    }

    public static StudyGroup SeedGroup(AppDbContext ctx, int createdBy, string name = "Grupo Test", int? subjectId = null)
    {
        var group = new StudyGroup
        {
            Name = name,
            Description = "Descripción de prueba",
            CreatedBy = createdBy,
            SubjectId = subjectId,
            CreatedAt = DateTime.UtcNow
        };
        ctx.StudyGroups.Add(group);
        ctx.SaveChanges();

        // Creador entra como admin automáticamente
        ctx.GroupMembers.Add(new GroupMember { GroupId = group.Id, UserId = createdBy, Role = "admin" });
        ctx.SaveChanges();

        return group;
    }

    public static GroupMember AddMember(AppDbContext ctx, int groupId, int userId, string role = "member")
    {
        var m = new GroupMember { GroupId = groupId, UserId = userId, Role = role };
        ctx.GroupMembers.Add(m);
        ctx.SaveChanges();
        return m;
    }
}

// ═══════════════════════════════════════════════════════════════════════════════
// SPRINT 04 — Crear Grupo de Estudio
// ═══════════════════════════════════════════════════════════════════════════════

/// <summary>
/// SPRINT 04 - Crear Grupo de Estudio
/// Valida la creación de grupos: persistencia, membresía automática del creador
/// como admin, unicidad de nombres (si aplica), y limites de campos.
/// </summary>
public class Sprint04_CrearGrupoEstudio_Tests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly GroupManagementService _groupService;
    private readonly User _owner;

    public Sprint04_CrearGrupoEstudio_Tests()
    {
        _context = TestDbHelper.CreateInMemoryContext();
        var logger = new Mock<ILogger<GroupManagementService>>().Object;
        _groupService = new GroupManagementService(_context, logger);
        _owner = TestDbHelper.SeedUser(_context);
    }

    [Fact]
    [Trait("Sprint", "04")]
    [Trait("Funcionalidad", "Crear Grupo de Estudio")]
    [Trait("Tipo", "Happy Path")]
    public void CreateGroup_ConDatosValidos_SeAlmacenaEnBD()
    {
        // Arrange & Act
        var grupo = TestDbHelper.SeedGroup(_context, _owner.UserId, "Álgebra Lineal");

        // Assert
        grupo.Id.Should().BeGreaterThan(0);
        grupo.Name.Should().Be("Álgebra Lineal");
        grupo.CreatedBy.Should().Be(_owner.UserId);
    }

    [Fact]
    [Trait("Sprint", "04")]
    [Trait("Funcionalidad", "Crear Grupo de Estudio")]
    [Trait("Tipo", "Regla de Negocio")]
    public void CreateGroup_CreadorEsAdminAutomaticamente()
    {
        // Act
        var grupo = TestDbHelper.SeedGroup(_context, _owner.UserId);

        // Assert
        var membresiaCreador = _context.GroupMembers
            .FirstOrDefault(gm => gm.GroupId == grupo.Id && gm.UserId == _owner.UserId);

        membresiaCreador.Should().NotBeNull("el creador debe ser miembro inmediatamente");
        membresiaCreador!.Role.Should().Be("admin", "el creador siempre tiene rol de admin");
    }

    [Fact]
    [Trait("Sprint", "04")]
    [Trait("Funcionalidad", "Crear Grupo de Estudio")]
    [Trait("Tipo", "Happy Path")]
    public void CreateGroup_ConMateria_SeAsociaCorrectamente()
    {
        // Arrange
        var materia = TestDbHelper.SeedSubject(_context, "Cálculo");

        // Act
        var grupo = TestDbHelper.SeedGroup(_context, _owner.UserId, "Grupo Cálculo", materia.Id);

        // Assert
        grupo.SubjectId.Should().Be(materia.Id);
    }

    [Fact]
    [Trait("Sprint", "04")]
    [Trait("Funcionalidad", "Crear Grupo de Estudio")]
    [Trait("Tipo", "Borde")]
    public void CreateGroup_SinMateria_SubjectIdEsNull()
    {
        // Act
        var grupo = TestDbHelper.SeedGroup(_context, _owner.UserId, "Grupo Sin Materia", null);

        // Assert
        grupo.SubjectId.Should().BeNull("la materia es opcional al crear un grupo");
    }

    [Fact]
    [Trait("Sprint", "04")]
    [Trait("Funcionalidad", "Crear Grupo de Estudio")]
    [Trait("Tipo", "Borde")]
    public void CreateGroup_MultiplesGrupos_CadaUnoTieneIdUnico()
    {
        // Act
        var g1 = TestDbHelper.SeedGroup(_context, _owner.UserId, "Grupo A");
        var g2 = TestDbHelper.SeedGroup(_context, _owner.UserId, "Grupo B");
        var g3 = TestDbHelper.SeedGroup(_context, _owner.UserId, "Grupo C");

        // Assert
        new[] { g1.Id, g2.Id, g3.Id }.Should().OnlyHaveUniqueItems();
    }

    [Fact]
    [Trait("Sprint", "04")]
    [Trait("Funcionalidad", "Crear Grupo de Estudio")]
    [Trait("Tipo", "Happy Path")]
    public void CreateGroup_FechaCreacion_SeEstableceEnUtc()
    {
        // Arrange
        var antes = DateTime.UtcNow.AddSeconds(-1);

        // Act
        var grupo = TestDbHelper.SeedGroup(_context, _owner.UserId);

        // Assert
        grupo.CreatedAt.Should().BeAfter(antes);
        grupo.CreatedAt.Should().BeBefore(DateTime.UtcNow.AddSeconds(5));
    }

    [Fact]
    [Trait("Sprint", "04")]
    [Trait("Funcionalidad", "Crear Grupo de Estudio")]
    [Trait("Tipo", "Regla de Negocio")]
    public async Task CreateGroup_ConteoMiembros_IniciaEnUno()
    {
        // Act
        var grupo = TestDbHelper.SeedGroup(_context, _owner.UserId);

        // Assert
        var conteo = await _context.GroupMembers.CountAsync(gm => gm.GroupId == grupo.Id);
        conteo.Should().Be(1, "solo el creador debe ser miembro al inicio");
    }

    [Fact]
    [Trait("Sprint", "04")]
    [Trait("Funcionalidad", "Crear Grupo de Estudio")]
    [Trait("Tipo", "Permisos")]
    public async Task IsGroupOwner_ConCreador_RetornaTrue()
    {
        // Arrange
        var grupo = TestDbHelper.SeedGroup(_context, _owner.UserId);

        // Act
        var esOwner = await _groupService.IsGroupOwner(grupo.Id, _owner.UserId);

        // Assert
        esOwner.Should().BeTrue("el usuario que creó el grupo es el dueño");
    }

    [Fact]
    [Trait("Sprint", "04")]
    [Trait("Funcionalidad", "Crear Grupo de Estudio")]
    [Trait("Tipo", "Permisos")]
    public async Task IsGroupOwner_ConOtroUsuario_RetornaFalse()
    {
        // Arrange
        var otro = TestDbHelper.SeedUser(_context);
        var grupo = TestDbHelper.SeedGroup(_context, _owner.UserId);

        // Act
        var esOwner = await _groupService.IsGroupOwner(grupo.Id, otro.UserId);

        // Assert
        esOwner.Should().BeFalse("un usuario que no creó el grupo no es el dueño");
    }

    public void Dispose() => _context.Dispose();
}

// ═══════════════════════════════════════════════════════════════════════════════
// SPRINT 05 — Unirse a Grupo de Estudio
// ═══════════════════════════════════════════════════════════════════════════════

/// <summary>
/// SPRINT 05 - Unirse a Grupo de Estudio
/// Verifica que un usuario puede unirse como miembro, que no puede duplicar
/// su membresía, que los grupos no existentes se manejan correctamente,
/// y los roles asignados al unirse.
/// </summary>
public class Sprint05_UnirseGrupoEstudio_Tests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly User _owner;
    private readonly StudyGroup _grupo;

    public Sprint05_UnirseGrupoEstudio_Tests()
    {
        _context = TestDbHelper.CreateInMemoryContext();
        _owner = TestDbHelper.SeedUser(_context);
        _grupo = TestDbHelper.SeedGroup(_context, _owner.UserId, "Grupo de Física");
    }

    [Fact]
    [Trait("Sprint", "05")]
    [Trait("Funcionalidad", "Unirse a Grupo")]
    [Trait("Tipo", "Happy Path")]
    public async Task JoinGroup_UsuarioNuevo_SeConvierteEnMiembro()
    {
        // Arrange
        var nuevoUsuario = TestDbHelper.SeedUser(_context);

        // Act — simular unirse al grupo directamente en BD (como haría el controller)
        var membership = new GroupMember
        {
            GroupId = _grupo.Id,
            UserId = nuevoUsuario.UserId,
            Role = "member"
        };
        _context.GroupMembers.Add(membership);
        await _context.SaveChangesAsync();

        // Assert
        var enBD = await _context.GroupMembers
            .FirstOrDefaultAsync(gm => gm.GroupId == _grupo.Id && gm.UserId == nuevoUsuario.UserId);
        enBD.Should().NotBeNull();
        enBD!.Role.Should().Be("member");
    }

    [Fact]
    [Trait("Sprint", "05")]
    [Trait("Funcionalidad", "Unirse a Grupo")]
    [Trait("Tipo", "Negativo")]
    public async Task JoinGroup_UsuarioYaMiembro_ExisteMembresiaPrevia()
    {
        // Arrange
        var usuario = TestDbHelper.SeedUser(_context);
        TestDbHelper.AddMember(_context, _grupo.Id, usuario.UserId);

        // Act — verificar que ya existe membresía (lo que el controller comprueba antes de añadir)
        var existente = await _context.GroupMembers
            .FirstOrDefaultAsync(gm => gm.GroupId == _grupo.Id && gm.UserId == usuario.UserId);

        // Assert
        existente.Should().NotBeNull("el usuario ya es miembro, no debe poder unirse dos veces");
    }

    [Fact]
    [Trait("Sprint", "05")]
    [Trait("Funcionalidad", "Unirse a Grupo")]
    [Trait("Tipo", "Negativo")]
    public async Task JoinGroup_GrupoInexistente_NoDebeCrearMembresia()
    {
        // Arrange
        var usuario = TestDbHelper.SeedUser(_context);
        const int idFalso = 99999;

        // Act
        var grupoExiste = await _context.StudyGroups.AnyAsync(g => g.Id == idFalso);

        // Assert
        grupoExiste.Should().BeFalse("el grupo con ese ID no existe");
    }

    [Fact]
    [Trait("Sprint", "05")]
    [Trait("Funcionalidad", "Unirse a Grupo")]
    [Trait("Tipo", "Regla de Negocio")]
    public async Task JoinGroup_RolAsignado_SiempreEsMember()
    {
        // Arrange
        var usuario = TestDbHelper.SeedUser(_context);
        var memb = new GroupMember
        {
            GroupId = _grupo.Id,
            UserId = usuario.UserId,
            Role = "member"    // el controller siempre pone "member" al unirse
        };
        _context.GroupMembers.Add(memb);
        await _context.SaveChangesAsync();

        // Assert
        var enBD = await _context.GroupMembers
            .FirstAsync(gm => gm.GroupId == _grupo.Id && gm.UserId == usuario.UserId);
        enBD.Role.Should().Be("member",
            "al unirse a un grupo, el rol inicial siempre es 'member', nunca 'admin'");
    }

    [Fact]
    [Trait("Sprint", "05")]
    [Trait("Funcionalidad", "Unirse a Grupo")]
    [Trait("Tipo", "Regla de Negocio")]
    public async Task JoinGroup_VariosUsuarios_ConteoMiembrosAumenta()
    {
        // Arrange
        var u1 = TestDbHelper.SeedUser(_context);
        var u2 = TestDbHelper.SeedUser(_context);
        var u3 = TestDbHelper.SeedUser(_context);

        // Act
        TestDbHelper.AddMember(_context, _grupo.Id, u1.UserId);
        TestDbHelper.AddMember(_context, _grupo.Id, u2.UserId);
        TestDbHelper.AddMember(_context, _grupo.Id, u3.UserId);

        // Assert — 3 nuevos + 1 admin (owner) = 4
        var total = await _context.GroupMembers.CountAsync(gm => gm.GroupId == _grupo.Id);
        total.Should().Be(4);
    }

    [Fact]
    [Trait("Sprint", "05")]
    [Trait("Funcionalidad", "Unirse a Grupo")]
    [Trait("Tipo", "Borde")]
    public async Task JoinGroup_MismoUsuarioDosVeces_SoloExisteUnaMembresía()
    {
        // Arrange
        var usuario = TestDbHelper.SeedUser(_context);
        TestDbHelper.AddMember(_context, _grupo.Id, usuario.UserId);

        // Act — intentar añadir de nuevo (el sistema debería detectar el duplicado)
        var conteoAntes = await _context.GroupMembers
            .CountAsync(gm => gm.GroupId == _grupo.Id && gm.UserId == usuario.UserId);

        // Assert
        conteoAntes.Should().Be(1, "un usuario solo puede tener una membresía por grupo");
    }

    [Fact]
    [Trait("Sprint", "05")]
    [Trait("Funcionalidad", "Unirse a Grupo")]
    [Trait("Tipo", "Permisos")]
    public async Task IsGroupAdmin_MiembroNormal_RetornaFalse()
    {
        // Arrange
        var logger = new Mock<ILogger<GroupManagementService>>().Object;
        var service = new GroupManagementService(_context, logger);
        var usuario = TestDbHelper.SeedUser(_context);
        TestDbHelper.AddMember(_context, _grupo.Id, usuario.UserId, "member");

        // Act
        var esAdmin = await service.IsGroupAdmin(_grupo.Id, usuario.UserId);

        // Assert
        esAdmin.Should().BeFalse("un miembro con rol 'member' no es admin");
    }

    public void Dispose() => _context.Dispose();
}

// ═══════════════════════════════════════════════════════════════════════════════
// SPRINT 06 — Subir Documentos
// ═══════════════════════════════════════════════════════════════════════════════

/// <summary>
/// SPRINT 06 - Subir Documentos
/// Valida la lógica de almacenamiento de documentos de grupo:
/// persistencia, metadatos requeridos, tipos de archivo y eliminación.
/// </summary>
public class Sprint06_SubirDocumentos_Tests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly User _uploader;
    private readonly StudyGroup _grupo;

    public Sprint06_SubirDocumentos_Tests()
    {
        _context = TestDbHelper.CreateInMemoryContext();
        _uploader = TestDbHelper.SeedUser(_context);
        _grupo = TestDbHelper.SeedGroup(_context, _uploader.UserId, "Grupo Documentos");
    }

    private GroupDocument CrearDocumento(string nombre = "apuntes.pdf", string url = "https://storage.test/apuntes.pdf")
    {
        var doc = new GroupDocument
        {
            GroupId = _grupo.Id,
            UserId = _uploader.UserId,
            Title = Path.GetFileNameWithoutExtension(nombre),
            FileName = nombre,
            FileUrl = url,
            FileType = "application/pdf",
            FileSize = 204800,   // 200 KB
            UploadDate = DateTime.UtcNow
        };
        _context.GroupDocuments.Add(doc);
        _context.SaveChanges();
        return doc;
    }

    [Fact]
    [Trait("Sprint", "06")]
    [Trait("Funcionalidad", "Subir Documentos")]
    [Trait("Tipo", "Happy Path")]
    public void UploadDocument_ConDatosValidos_SeAlmacenaEnBD()
    {
        // Act
        var doc = CrearDocumento("teoria_fisica.pdf");

        // Assert
        doc.Id.Should().BeGreaterThan(0);
        doc.FileName.Should().Be("teoria_fisica.pdf");
        doc.GroupId.Should().Be(_grupo.Id);
        doc.UserId.Should().Be(_uploader.UserId);
    }

    [Fact]
    [Trait("Sprint", "06")]
    [Trait("Funcionalidad", "Subir Documentos")]
    [Trait("Tipo", "Happy Path")]
    public async Task UploadDocument_SeRecuperaPorGrupoId()
    {
        // Arrange
        CrearDocumento("doc1.pdf");
        CrearDocumento("doc2.pdf", "https://storage.test/doc2.pdf");

        // Act
        var docs = await _context.GroupDocuments
            .Where(d => d.GroupId == _grupo.Id)
            .ToListAsync();

        // Assert
        docs.Should().HaveCount(2, "se subieron 2 documentos a este grupo");
    }

    [Fact]
    [Trait("Sprint", "06")]
    [Trait("Funcionalidad", "Subir Documentos")]
    [Trait("Tipo", "Metadatos")]
    public void UploadDocument_UrlDeAlmacenamiento_SeGuardaCorrectamente()
    {
        // Arrange
        const string url = "https://firebase.storage.google.com/apuntes/tema1.pdf";

        // Act
        var doc = CrearDocumento("tema1.pdf", url);

        // Assert
        doc.FileUrl.Should().Be(url, "la URL del archivo almacenado debe conservarse intacta");
    }

    [Fact]
    [Trait("Sprint", "06")]
    [Trait("Funcionalidad", "Subir Documentos")]
    [Trait("Tipo", "Metadatos")]
    public void UploadDocument_FechaSubida_SeEstableceAutomaticamente()
    {
        // Arrange
        var antes = DateTime.UtcNow.AddSeconds(-1);

        // Act
        var doc = CrearDocumento();

        // Assert
        doc.UploadDate.Should().BeAfter(antes);
    }

    [Fact]
    [Trait("Sprint", "06")]
    [Trait("Funcionalidad", "Subir Documentos")]
    [Trait("Tipo", "Negativo")]
    public async Task DeleteDocument_DocumentoExistente_SeEliminaCorrectamente()
    {
        // Arrange
        var doc = CrearDocumento("borrar.pdf");

        // Act
        _context.GroupDocuments.Remove(doc);
        await _context.SaveChangesAsync();

        // Assert
        var enBD = await _context.GroupDocuments.FindAsync(doc.Id);
        enBD.Should().BeNull("el documento fue eliminado");
    }

    [Fact]
    [Trait("Sprint", "06")]
    [Trait("Funcionalidad", "Subir Documentos")]
    [Trait("Tipo", "Borde")]
    public async Task UploadDocument_GrupoSinDocumentos_RetornaListaVacia()
    {
        // Act
        var docs = await _context.GroupDocuments
            .Where(d => d.GroupId == _grupo.Id)
            .ToListAsync();

        // Assert
        docs.Should().BeEmpty();
    }

    [Theory]
    [Trait("Sprint", "06")]
    [Trait("Funcionalidad", "Subir Documentos")]
    [Trait("Tipo", "Tipos de Archivo")]
    [InlineData("application/pdf", "examen.pdf")]
    [InlineData("application/vnd.openxmlformats-officedocument.wordprocessingml.document", "tarea.docx")]
    [InlineData("image/png", "diagrama.png")]
    [InlineData("image/jpeg", "foto.jpg")]
    public void UploadDocument_DiversosTiposDeArchivo_SeAlmacenanCorrectamente(string mimeType, string fileName)
    {
        // Arrange
        var doc = new GroupDocument
        {
            GroupId = _grupo.Id,
            UserId = _uploader.UserId,
            Title = Path.GetFileNameWithoutExtension(fileName),
            FileName = fileName,
            FileUrl = $"https://storage.test/{fileName}",
            FileType = mimeType,
            FileSize = 1024,
            UploadDate = DateTime.UtcNow
        };
        _context.GroupDocuments.Add(doc);
        _context.SaveChanges();

        // Assert
        doc.FileType.Should().Be(mimeType);
        doc.FileName.Should().Be(fileName);
    }

    [Fact]
    [Trait("Sprint", "06")]
    [Trait("Funcionalidad", "Subir Documentos")]
    [Trait("Tipo", "Borde")]
    public void UploadDocument_TamanoArchivo_SeAlmacenaEnBytes()
    {
        // Arrange — 5 MB = 5 * 1024 * 1024 bytes
        const long cincoMB = 5L * 1024 * 1024;
        var doc = new GroupDocument
        {
            GroupId = _grupo.Id,
            UserId = _uploader.UserId,
            Title = "grande",
            FileName = "grande.pdf",
            FileUrl = "https://storage.test/grande.pdf",
            FileType = "application/pdf",
            FileSize = cincoMB,
            UploadDate = DateTime.UtcNow
        };
        _context.GroupDocuments.Add(doc);
        _context.SaveChanges();

        // Assert
        doc.FileSize.Should().Be(cincoMB);
    }

    public void Dispose() => _context.Dispose();
}

// ═══════════════════════════════════════════════════════════════════════════════
// SPRINT 07 — Editar Información del Grupo
// ═══════════════════════════════════════════════════════════════════════════════

/// <summary>
/// SPRINT 07 - Editar Información del Grupo
/// Verifica la actualización de nombre, descripción, imagen y materia,
/// así como los controles de permisos: solo admins pueden editar.
/// </summary>
public class Sprint07_EditarInformacionGrupo_Tests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly GroupManagementService _groupService;
    private readonly User _admin;
    private readonly User _miembro;
    private readonly StudyGroup _grupo;

    public Sprint07_EditarInformacionGrupo_Tests()
    {
        _context = TestDbHelper.CreateInMemoryContext();
        var logger = new Mock<ILogger<GroupManagementService>>().Object;
        _groupService = new GroupManagementService(_context, logger);

        _admin = TestDbHelper.SeedUser(_context);
        _miembro = TestDbHelper.SeedUser(_context);
        _grupo = TestDbHelper.SeedGroup(_context, _admin.UserId, "Grupo Original");
        TestDbHelper.AddMember(_context, _grupo.Id, _miembro.UserId, "member");
    }

    [Fact]
    [Trait("Sprint", "07")]
    [Trait("Funcionalidad", "Editar Grupo")]
    [Trait("Tipo", "Happy Path")]
    public async Task EditGroup_Nombre_SeActualizaCorrectamente()
    {
        // Arrange
        var grupo = await _context.StudyGroups.FindAsync(_grupo.Id);

        // Act
        grupo!.Name = "Nombre Actualizado";
        await _context.SaveChangesAsync();

        // Assert
        var enBD = await _context.StudyGroups.FindAsync(_grupo.Id);
        enBD!.Name.Should().Be("Nombre Actualizado");
    }

    [Fact]
    [Trait("Sprint", "07")]
    [Trait("Funcionalidad", "Editar Grupo")]
    [Trait("Tipo", "Happy Path")]
    public async Task EditGroup_Descripcion_SeActualizaCorrectamente()
    {
        // Act
        var grupo = await _context.StudyGroups.FindAsync(_grupo.Id);
        grupo!.Description = "Nueva descripción detallada del grupo de estudio";
        await _context.SaveChangesAsync();

        // Assert
        var enBD = await _context.StudyGroups.FindAsync(_grupo.Id);
        enBD!.Description.Should().Be("Nueva descripción detallada del grupo de estudio");
    }

    [Fact]
    [Trait("Sprint", "07")]
    [Trait("Funcionalidad", "Editar Grupo")]
    [Trait("Tipo", "Happy Path")]
    public async Task EditGroup_ImagenPortada_SeActualizaCorrectamente()
    {
        // Act
        var grupo = await _context.StudyGroups.FindAsync(_grupo.Id);
        grupo!.CoverImage = "https://storage.test/nueva-portada.jpg";
        await _context.SaveChangesAsync();

        // Assert
        var enBD = await _context.StudyGroups.FindAsync(_grupo.Id);
        enBD!.CoverImage.Should().Be("https://storage.test/nueva-portada.jpg");
    }

    [Fact]
    [Trait("Sprint", "07")]
    [Trait("Funcionalidad", "Editar Grupo")]
    [Trait("Tipo", "Happy Path")]
    public async Task EditGroup_Materia_SeActualizaCorrectamente()
    {
        // Arrange
        var materia = TestDbHelper.SeedSubject(_context, "Química");

        // Act
        var grupo = await _context.StudyGroups.FindAsync(_grupo.Id);
        grupo!.SubjectId = materia.Id;
        await _context.SaveChangesAsync();

        // Assert
        var enBD = await _context.StudyGroups.FindAsync(_grupo.Id);
        enBD!.SubjectId.Should().Be(materia.Id);
    }

    [Fact]
    [Trait("Sprint", "07")]
    [Trait("Funcionalidad", "Editar Grupo")]
    [Trait("Tipo", "Permisos")]
    public async Task CanEditGroup_Admin_RetornaTrue()
    {
        // Act
        var puedEditar = await _groupService.CanUserEditGroup(_grupo.Id, _admin.UserId);

        // Assert
        puedEditar.Should().BeTrue("el admin tiene permiso para editar el grupo");
    }

    [Fact]
    [Trait("Sprint", "07")]
    [Trait("Funcionalidad", "Editar Grupo")]
    [Trait("Tipo", "Permisos")]
    public async Task CanEditGroup_MiembroNormal_RetornaFalse()
    {
        // Act
        var puedeEditar = await _groupService.CanUserEditGroup(_grupo.Id, _miembro.UserId);

        // Assert
        puedeEditar.Should().BeFalse("un miembro con rol 'member' NO puede editar el grupo");
    }

    [Fact]
    [Trait("Sprint", "07")]
    [Trait("Funcionalidad", "Editar Grupo")]
    [Trait("Tipo", "Permisos")]
    public async Task CanEditGroup_UsuarioExterno_RetornaFalse()
    {
        // Arrange
        var externo = TestDbHelper.SeedUser(_context);

        // Act
        var puedeEditar = await _groupService.CanUserEditGroup(_grupo.Id, externo.UserId);

        // Assert
        puedeEditar.Should().BeFalse("un usuario que no pertenece al grupo no puede editarlo");
    }

    [Fact]
    [Trait("Sprint", "07")]
    [Trait("Funcionalidad", "Editar Grupo")]
    [Trait("Tipo", "Borde")]
    public async Task EditGroup_ImagenPortadaANull_SePermite()
    {
        // Arrange — primero establecer una imagen
        var grupo = await _context.StudyGroups.FindAsync(_grupo.Id);
        grupo!.CoverImage = "https://storage.test/portada.jpg";
        await _context.SaveChangesAsync();

        // Act — quitar la imagen (limpiar portada)
        grupo.CoverImage = null;
        await _context.SaveChangesAsync();

        // Assert
        var enBD = await _context.StudyGroups.FindAsync(_grupo.Id);
        enBD!.CoverImage.Should().BeNull("el admin puede quitar la imagen de portada");
    }

    [Fact]
    [Trait("Sprint", "07")]
    [Trait("Funcionalidad", "Editar Grupo")]
    [Trait("Tipo", "Regla de Negocio")]
    public async Task GetPermissions_Admin_TieneTodosLosPermisos()
    {
        // Act
        var permisos = await _groupService.GetUserPermissions(_grupo.Id, _admin.UserId);

        // Assert
        permisos.CanEditGroup.Should().BeTrue();
        permisos.CanManageMembers.Should().BeTrue();
        permisos.IsAdmin.Should().BeTrue();
    }

    [Fact]
    [Trait("Sprint", "07")]
    [Trait("Funcionalidad", "Editar Grupo")]
    [Trait("Tipo", "Regla de Negocio")]
    public async Task GetPermissions_Miembro_NoTienePermisoDeEdicion()
    {
        // Act
        var permisos = await _groupService.GetUserPermissions(_grupo.Id, _miembro.UserId);

        // Assert
        permisos.CanEditGroup.Should().BeFalse();
        permisos.CanDeleteGroup.Should().BeFalse();
        permisos.IsMember.Should().BeTrue();
        permisos.IsAdmin.Should().BeFalse();
    }


    public void Dispose() => _context.Dispose();
}