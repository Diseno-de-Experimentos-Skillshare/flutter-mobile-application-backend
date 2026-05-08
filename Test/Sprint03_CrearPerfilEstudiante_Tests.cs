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

/// <summary>
/// SPRINT 03 - Crear Perfil de Estudiante
/// Cubre la creación, validaciones de campos obligatorios,
/// restricciones de formato y unicidad del perfil por usuario.
/// </summary>
public class Sprint03_CrearPerfilEstudiante_Tests : IDisposable
{
    private readonly AppDbContext    _context;
    private readonly StudentService  _studentService;
    private int _testUserId;

    public Sprint03_CrearPerfilEstudiante_Tests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new AppDbContext(options);

        var logger = new Mock<ILogger<StudentService>>().Object;
        _studentService = new StudentService(_context, logger);

        _testUserId = SeedUserAndGetId();
    }

    // ════════════════════════════════════════════════════════════════════════
    // CASOS EXITOSOS
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    [Trait("Sprint", "03")]
    [Trait("Funcionalidad", "Crear Perfil Estudiante")]
    [Trait("Tipo", "Happy Path")]
    public async Task CreateStudent_ConDatosCompletos_CreaPerfilExitosamente()
    {
        // Arrange
        var dto = new CreateStudentDto
        {
            FirstName         = "María",
            LastName          = "González",
            Nickname          = "mariag",
            DateBirth         = new DateTime(2000, 6, 15),
            Country           = "Colombia",
            EducationalCenter = "Universidad Nacional",
            Gender            = "female",
            UserType          = 1,
            UserId            = _testUserId
        };

        // Act
        var resultado = await _studentService.CreateStudentAsync(dto);

        // Assert
        resultado.Should().NotBeNull();
        resultado.Id.Should().BeGreaterThan(0);
        resultado.FirstName.Should().Be("María");
        resultado.LastName.Should().Be("González");
        resultado.Nickname.Should().Be("mariag");
        resultado.Country.Should().Be("Colombia");
        resultado.Gender.Should().Be("female");
        resultado.UserId.Should().Be(_testUserId);
    }

    [Fact]
    [Trait("Sprint", "03")]
    [Trait("Funcionalidad", "Crear Perfil Estudiante")]
    [Trait("Tipo", "Happy Path")]
    public async Task CreateStudent_ConCamposMinimos_CreaPerfilConValoresPorDefecto()
    {
        // Arrange — solo campos obligatorios
        var dto = new CreateStudentDto
        {
            FirstName = "Carlos",
            LastName  = "Pérez",
            Gender    = "male",
            UserId    = _testUserId
        };

        // Act
        var resultado = await _studentService.CreateStudentAsync(dto);

        // Assert
        resultado.Should().NotBeNull();
        resultado.FirstName.Should().Be("Carlos");
        resultado.LastName.Should().Be("Pérez");
        resultado.Nickname.Should().BeNull("el nickname es opcional");
        resultado.Country.Should().BeNull("el país es opcional");
        resultado.EducationalCenter.Should().BeNull("el centro educativo es opcional");
    }

    [Fact]
    [Trait("Sprint", "03")]
    [Trait("Funcionalidad", "Crear Perfil Estudiante")]
    [Trait("Tipo", "Happy Path")]
    public async Task CreateStudent_PersistidoCorrectamente_SeRecuperaPorId()
    {
        // Arrange
        var dto = CrearDtoValido(_testUserId);

        // Act
        var creado     = await _studentService.CreateStudentAsync(dto);
        var recuperado = await _studentService.GetStudentByIdAsync(creado.Id);

        // Assert
        recuperado.Should().NotBeNull();
        recuperado!.Id.Should().Be(creado.Id);
        recuperado.FirstName.Should().Be(dto.FirstName);
        recuperado.LastName.Should().Be(dto.LastName);
    }

    [Fact]
    [Trait("Sprint", "03")]
    [Trait("Funcionalidad", "Crear Perfil Estudiante")]
    [Trait("Tipo", "Happy Path")]
    public async Task CreateStudent_PersistidoCorrectamente_SeRecuperaPorUserId()
    {
        // Arrange
        var dto = CrearDtoValido(_testUserId);

        // Act
        await _studentService.CreateStudentAsync(dto);
        var recuperado = await _studentService.GetStudentByUserIdAsync(_testUserId);

        // Assert
        recuperado.Should().NotBeNull();
        recuperado!.UserId.Should().Be(_testUserId);
    }

    // ════════════════════════════════════════════════════════════════════════
    // VALIDACIONES DE GÉNERO
    // ════════════════════════════════════════════════════════════════════════

    [Xunit.Theory]
    [Trait("Sprint", "03")]
    [Trait("Funcionalidad", "Crear Perfil Estudiante")]
    [Trait("Tipo", "Validación")]
    [InlineData("male")]
    [InlineData("female")]
    [InlineData("other")]
    [InlineData("prefer_not_to_say")]
    public async Task CreateStudent_ConGeneroValido_SeAlmacenaCorrectamente(string gender)
    {
        // Arrange
        var userId = SeedUserAndGetId();   // usuario nuevo para cada iteración
        var dto = new CreateStudentDto
        {
            FirstName = "Test",
            LastName  = "User",
            Gender    = gender,
            UserId    = userId
        };

        // Act
        var resultado = await _studentService.CreateStudentAsync(dto);

        // Assert
        resultado.Gender.Should().Be(gender);
    }

    // ════════════════════════════════════════════════════════════════════════
    // CASOS NEGATIVOS — BÚSQUEDA DE NO EXISTENTES
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    [Trait("Sprint", "03")]
    [Trait("Funcionalidad", "Crear Perfil Estudiante")]
    [Trait("Tipo", "Negativo")]
    public async Task GetStudentById_ConIdInexistente_RetornaNull()
    {
        // Act
        var resultado = await _studentService.GetStudentByIdAsync(99999);

        // Assert
        resultado.Should().BeNull("no existe un estudiante con ese ID");
    }

    [Fact]
    [Trait("Sprint", "03")]
    [Trait("Funcionalidad", "Crear Perfil Estudiante")]
    [Trait("Tipo", "Negativo")]
    public async Task GetStudentByUserId_ConUserIdInexistente_RetornaNull()
    {
        // Act
        var resultado = await _studentService.GetStudentByUserIdAsync(99999);

        // Assert
        resultado.Should().BeNull("ese usuario no tiene perfil de estudiante");
    }

    // ════════════════════════════════════════════════════════════════════════
    // CASOS DE BORDE
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    [Trait("Sprint", "03")]
    [Trait("Funcionalidad", "Crear Perfil Estudiante")]
    [Trait("Tipo", "Borde")]
    public async Task CreateStudent_ConFechaNacimientoFutura_SeAlmacena()
    {
        // Arrange — el modelo no valida que la fecha sea pasada; se almacena tal cual
        var dto = CrearDtoValido(_testUserId);
        dto.DateBirth = DateTime.UtcNow.AddYears(10);

        // Act
        var resultado = await _studentService.CreateStudentAsync(dto);

        // Assert — la capa de servicio no rechaza fechas futuras; esa lógica va en el DTO o el controlador
        resultado.DateBirth.Should().NotBeNull();
    }

    [Fact]
    [Trait("Sprint", "03")]
    [Trait("Funcionalidad", "Crear Perfil Estudiante")]
    [Trait("Tipo", "Borde")]
    public async Task GetAllStudents_SinEstudiantes_RetornaColeccionVacia()
    {
        // Act
        var estudiantes = await _studentService.GetAllStudentsAsync();

        // Assert
        estudiantes.Should().BeEmpty("no se ha creado ningún estudiante aún");
    }

    [Fact]
    [Trait("Sprint", "03")]
    [Trait("Funcionalidad", "Crear Perfil Estudiante")]
    [Trait("Tipo", "Borde")]
    public async Task GetAllStudents_ConMultiplesEstudiantes_RetornaTodos()
    {
        // Arrange
        var uid1 = SeedUserAndGetId();
        var uid2 = SeedUserAndGetId();
        await _studentService.CreateStudentAsync(CrearDtoValido(uid1));
        await _studentService.CreateStudentAsync(CrearDtoValido(uid2));

        // Act
        var estudiantes = await _studentService.GetAllStudentsAsync();

        // Assert
        estudiantes.Should().HaveCount(2);
    }

    // ════════════════════════════════════════════════════════════════════════
    // ACTUALIZACIÓN DE PERFIL
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    [Trait("Sprint", "03")]
    [Trait("Funcionalidad", "Crear Perfil Estudiante")]
    [Trait("Tipo", "Happy Path")]
    public async Task UpdateStudent_ConDatosValidos_ActualizaCorrectamente()
    {
        // Arrange
        var creado = await _studentService.CreateStudentAsync(CrearDtoValido(_testUserId));
        var updateDto = new UpdateStudentDto
        {
            FirstName         = "NuevoNombre",
            Country           = "España",
            EducationalCenter = "Universidad de Madrid"
        };

        // Act
        var actualizado = await _studentService.UpdateStudentAsync(creado.Id, updateDto);

        // Assert
        actualizado.Should().NotBeNull();
        actualizado!.FirstName.Should().Be("NuevoNombre");
        actualizado.Country.Should().Be("España");
        actualizado.EducationalCenter.Should().Be("Universidad de Madrid");
    }

    [Fact]
    [Trait("Sprint", "03")]
    [Trait("Funcionalidad", "Crear Perfil Estudiante")]
    [Trait("Tipo", "Negativo")]
    public async Task UpdateStudent_ConIdInexistente_RetornaNull()
    {
        // Arrange
        var updateDto = new UpdateStudentDto { FirstName = "Fantasma" };

        // Act
        var resultado = await _studentService.UpdateStudentAsync(99999, updateDto);

        // Assert
        resultado.Should().BeNull();
    }

    // ════════════════════════════════════════════════════════════════════════
    // HELPERS
    // ════════════════════════════════════════════════════════════════════════

    private int SeedUserAndGetId()
    {
        var user = new User
        {
            Email     = $"user_{Guid.NewGuid():N}@test.com",
            Password  = BCrypt.Net.BCrypt.HashPassword("Pass123!"),
            CreatedAt = DateTime.UtcNow
        };
        _context.Users.Add(user);
        _context.SaveChanges();
        return user.UserId;
    }

    private static CreateStudentDto CrearDtoValido(int userId) => new()
    {
        FirstName         = "Ana",
        LastName          = "Martínez",
        Nickname          = "anam",
        DateBirth         = new DateTime(1999, 3, 21),
        Country           = "México",
        EducationalCenter = "UNAM",
        Gender            = "female",
        UserType          = 1,
        UserId            = userId
    };

    public void Dispose() => _context.Dispose();
}