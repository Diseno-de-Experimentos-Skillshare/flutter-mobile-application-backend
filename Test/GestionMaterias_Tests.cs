using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using SkillShareBackend.Data;
using SkillShareBackend.Models;

namespace SkillShareBackend.Tests;

/// <summary>
/// Gestión de Materias
/// Cubre la creación de materias, validaciones de datos,
/// asociación con grupos de estudio y operaciones CRUD.
/// Incluye 4 pruebas unitarias y 2 pruebas integrales.
/// </summary>
[TestFixture]
public class GestionMaterias_Tests
{
    private const string TEST_PASSWORD = "12345678";
    private AppDbContext _context = null!;
    private int _testUserId;

    [SetUp]
    public void Setup()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new AppDbContext(options);

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
    /// Test Unitario 1: Creación de materia con nombre válido
    /// Verifica que una materia se cree correctamente con un nombre válido
    /// </summary>
    [Test]
    [Category("Unitario")]
    [Description("Crea una materia con nombre válido")]
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
        Assert.That(materia.Id, Is.GreaterThan(0), "La materia debe tener un ID asignado");
        Assert.That(materia.Name, Is.EqualTo("Matemáticas Avanzadas"));
    }

    /// <summary>
    /// Test Unitario 2: Validación de nombre de materia
    /// Verifica que el nombre de la materia sea requerido y no nulo
    /// </summary>
    [Test]
    [Category("Unitario")]
    [Description("Valida que el nombre es requerido")]
    public void CrearMateria_SinNombre_LanzaExcepcion()
    {
        // Arrange
        var materia = new Subject
        {
            Name = "" // Nombre vacío
        };

        // Act & Assert
        _context.Subjects.Add(materia);
        
        // Esperamos que lance DbUpdateException al guardar
        Assert.Throws<DbUpdateException>(new NUnit.Framework.TestDelegate(() => 
            _context.SaveChanges()
        ));
    }

    /// <summary>
    /// Test Unitario 3: Búsqueda de materia por ID
    /// Verifica que se pueda buscar una materia por su ID
    /// </summary>
    [Test]
    [Category("Unitario")]
    [Description("Busca una materia por ID")]
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
        Assert.That(materiaEncontrada, Is.Not.Null);
        Assert.That(materiaEncontrada!.Name, Is.EqualTo("Física"));
        Assert.That(materiaEncontrada.Id, Is.EqualTo(materia.Id));
    }

    /// <summary>
    /// Test Unitario 4: Búsqueda de materia por nombre
    /// Verifica que se pueda buscar una materia por su nombre
    /// </summary>
    [Test]
    [Category("Unitario")]
    [Description("Busca una materia por nombre")]
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
        Assert.That(materiaEncontrada, Is.Not.Null);
        Assert.That(materiaEncontrada!.Name, Is.EqualTo(nombreMateria));
    }

    // ════════════════════════════════════════════════════════════════════════
    // PRUEBAS INTEGRALES - Interacción con base de datos
    // ════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Test Integral 1: Asociación de grupo con materia
    /// Verifica que un grupo de estudio se pueda asociar correctamente a una materia
    /// </summary>
    [Test]
    [Category("Integral")]
    [Description("Verifica asociación entre grupo y materia")]
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
        Assert.That(grupoRecuperado, Is.Not.Null);
        Assert.That(grupoRecuperado!.SubjectId, Is.EqualTo(materia.Id));
        Assert.That(grupoRecuperado.Subject, Is.Not.Null);
        Assert.That(grupoRecuperado.Subject!.Name, Is.EqualTo("Programación en C#"));
    }

    /// <summary>
    /// Test Integral 2: Listado de grupos por materia
    /// Verifica que se pueda obtener todos los grupos asociados a una materia
    /// </summary>
    [Test]
    [Category("Integral")]
    [Description("Verifica listado de grupos por materia")]
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
        Assert.That(gruposDeMateria.Count, Is.EqualTo(2), "Debe haber 2 grupos para esta materia");
        Assert.That(gruposDeMateria.Any(g => g.Name == "Historia Antigua"), Is.True);
        Assert.That(gruposDeMateria.Any(g => g.Name == "Historia Moderna"), Is.True);
        Assert.That(gruposDeMateria.All(g => g.SubjectId == materia.Id), Is.True);
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
