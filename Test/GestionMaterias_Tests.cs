using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using SkillShareBackend.Data;
using SkillShareBackend.Models;

namespace SkillShareBackend.Tests;

/// <summary>
/// Gestión de Materias
/// Cubre la creación de materias, validaciones de datos,
/// asociación con grupos de estudio y operaciones CRUD.
/// Incluye 4 pruebas unitarias y 2 pruebas integrales.
/// </summary>
public class GestionMaterias_Tests : IDisposable
{
    private const string TEST_PASSWORD = "12345678";
    private AppDbContext _context = null!;
    private int _testUserId;

    public GestionMaterias_Tests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new AppDbContext(options);

        _testUserId = SeedUserAndGetId();
    }

    public void Dispose()
    {
        _context?.Dispose();
    }

    // ════════════════════════════════════════════════════════════════════════
    // PRUEBAS UNITARIAS - Validaciones y comportamientos básicos
    // ════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Test Unitario 1: Creación de materia con nombre válido
    /// Verifica que una materia se cree correctamente con un nombre válido
    /// </summary>
    [Fact]
    [Trait("Category", "Unitario")]
    [Trait("Description", "Crea una materia con nombre válido")]
    public void CrearMateria_ConNombreValido_CreaExitosamente()
    {
        // Arrange
        var materia = new Subject
        {
            Name = "Matemáticas Avanzadas"
        };

        // Act
        _context.Subjects.Add(materia);
        _context.SaveChanges();

        // Assert
        Assert.True(materia.Id > 0, "La materia debe tener un ID asignado");
        Assert.Equal("Matemáticas Avanzadas", materia.Name);
    }

    /// <summary>
    /// Test Unitario 2: Actualización de materia existente
    /// Verifica que se pueda actualizar una materia existente
    /// </summary>
    [Fact]
    [Trait("Category", "Unitario")]
    [Trait("Description", "Actualiza una materia existente")]
    public void ActualizarMateria_ConNombreNuevo_ActualizaCorrectamente()
    {
        // Arrange
        var materia = new Subject { Name = "Física Clásica" };
        _context.Subjects.Add(materia);
        _context.SaveChanges();
        var id = materia.Id;

        // Act
        var materiaRecuperada = _context.Subjects.Find(id);
        if (materiaRecuperada != null)
        {
            materiaRecuperada.Name = "Física Moderna";
            _context.SaveChanges();
        }

        // Assert
        var materiaActualizada = _context.Subjects.Find(id);
        Assert.NotNull(materiaActualizada);
        Assert.Equal("Física Moderna", materiaActualizada!.Name);
    }

    /// <summary>
    /// Test Unitario 3: Búsqueda de materia por ID
    /// Verifica que se pueda buscar una materia por su ID
    /// </summary>
    [Fact]
    [Trait("Category", "Unitario")]
    [Trait("Description", "Busca una materia por ID")]
    public async Task BuscarMateriaPorId_MateriaExistente_RetornaMaterial()
    {
        // Arrange
        var materia = new Subject { Name = "Física" };
        _context.Subjects.Add(materia);
        await _context.SaveChangesAsync();

        // Act
        var materiaEncontrada = await _context.Subjects
            .FirstOrDefaultAsync(s => s.Id == materia.Id);

        // Assert
        Assert.NotNull(materiaEncontrada);
        Assert.Equal("Física", materiaEncontrada!.Name);
        Assert.Equal(materia.Id, materiaEncontrada.Id);
    }

    /// <summary>
    /// Test Unitario 4: Búsqueda de materia por nombre
    /// Verifica que se pueda buscar una materia por su nombre
    /// </summary>
    [Fact]
    [Trait("Category", "Unitario")]
    [Trait("Description", "Busca una materia por nombre")]
    public async Task BuscarMateriaPorNombre_MateriaExistente_RetornaMaterial()
    {
        // Arrange
        var nombreMateria = "Química Orgánica";
        var materia = new Subject { Name = nombreMateria };
        _context.Subjects.Add(materia);
        await _context.SaveChangesAsync();

        // Act
        var materiaEncontrada = await _context.Subjects
            .FirstOrDefaultAsync(s => s.Name == nombreMateria);

        // Assert
        Assert.NotNull(materiaEncontrada);
        Assert.Equal(nombreMateria, materiaEncontrada!.Name);
    }

    // ════════════════════════════════════════════════════════════════════════
    // PRUEBAS INTEGRALES - Interacción con base de datos
    // ════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Test Integral 1: Asociación de grupo con materia
    /// Verifica que un grupo de estudio se pueda asociar correctamente a una materia
    /// </summary>
    [Fact]
    [Trait("Category", "Integral")]
    [Trait("Description", "Verifica asociación entre grupo y materia")]
    public async Task CrearGrupoConMateria_GrupoAsociadoAMateria_PersisticaCorrectamente()
    {
        // Arrange
        var materia = new Subject { Name = "Programación en C#" };
        _context.Subjects.Add(materia);
        await _context.SaveChangesAsync();

        var grupo = new StudyGroup
        {
            Name = "Grupo C# Principiantes",
            Description = "Grupo para aprender C#",
            CreatedBy = _testUserId,
            SubjectId = materia.Id,
            CreatedAt = DateTime.UtcNow
        };
        _context.StudyGroups.Add(grupo);
        await _context.SaveChangesAsync();

        // Act
        var grupoRecuperado = await _context.StudyGroups
            .Include(g => g.Subject)
            .FirstOrDefaultAsync(g => g.Id == grupo.Id);

        // Assert
        Assert.NotNull(grupoRecuperado);
        Assert.Equal(materia.Id, grupoRecuperado!.SubjectId);
        Assert.NotNull(grupoRecuperado.Subject);
        Assert.Equal("Programación en C#", grupoRecuperado.Subject!.Name);
    }

    /// <summary>
    /// Test Integral 2: Listado de grupos por materia
    /// Verifica que se pueda obtener todos los grupos asociados a una materia
    /// </summary>
    [Fact]
    [Trait("Category", "Integral")]
    [Trait("Description", "Verifica listado de grupos por materia")]
    public async Task ObtenerGruposPorMateria_MateriasConMultiplesGrupos_RetornaTodasLosGrupos()
    {
        // Arrange
        var materia = new Subject { Name = "Historia" };
        _context.Subjects.Add(materia);
        await _context.SaveChangesAsync();

        var grupo1 = new StudyGroup
        {
            Name = "Historia Antigua",
            CreatedBy = _testUserId,
            SubjectId = materia.Id,
            CreatedAt = DateTime.UtcNow
        };

        var grupo2 = new StudyGroup
        {
            Name = "Historia Moderna",
            CreatedBy = _testUserId,
            SubjectId = materia.Id,
            CreatedAt = DateTime.UtcNow
        };

        _context.StudyGroups.Add(grupo1);
        _context.StudyGroups.Add(grupo2);
        await _context.SaveChangesAsync();

        // Act
        var gruposDeMateria = await _context.StudyGroups
            .Where(g => g.SubjectId == materia.Id)
            .ToListAsync();

        // Assert
        Assert.Equal(2, gruposDeMateria.Count);
        Assert.Contains(gruposDeMateria, g => g.Name == "Historia Antigua");
        Assert.Contains(gruposDeMateria, g => g.Name == "Historia Moderna");
        Assert.All(gruposDeMateria, g => Assert.Equal(materia.Id, g.SubjectId));
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
}