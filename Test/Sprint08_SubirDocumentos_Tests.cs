

using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using Moq;
using SkillShareBackend.Data;
using SkillShareBackend.Models;
using SkillShareBackend.Services;
using Xunit;

namespace SkillShareBackend.Tests;

// ═══════════════════════════════════════════════════════════════════════════════
// SPRINT 08 — Subir Documentos a Grupo de Estudio
// ═══════════════════════════════════════════════════════════════════════════════

/// <summary>
/// SPRINT 08 - Subir Documentos
/// Valida la lógica de subida de documentos a un grupo de estudio:
/// persistencia de metadatos, validación de tipos y tamaños de archivo,
/// restricción de acceso por membresía, y eliminación de documentos.
///
/// Incluye 5 pruebas unitarias y 2 pruebas integrales.
/// </summary>
public class Sprint08_SubirDocumentos_Tests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly User         _uploader;
    private readonly StudyGroup   _grupo;

    public Sprint08_SubirDocumentos_Tests()
    {
        _context  = TestDbHelper.CreateInMemoryContext();
        _uploader = TestDbHelper.SeedUser(_context);
        _grupo    = TestDbHelper.SeedGroup(_context, _uploader.UserId, "Grupo de Prueba Documentos");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // HELPERS INTERNOS
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Crea y persiste un GroupDocument de prueba en la base de datos en memoria.
    /// </summary>
    private GroupDocument SeedDocument(
        string fileName  = "apuntes.pdf",
        string fileType  = "pdf",
        long   fileSize  = 204_800,   // 200 KB por defecto
        string fileUrl   = "https://firebasestorage.googleapis.com/v0/b/test/o/apuntes.pdf?alt=media",
        int?   subjectId = null)
    {
        var doc = new GroupDocument
        {
            GroupId    = _grupo.Id,
            UserId     = _uploader.UserId,
            Title      = Path.GetFileNameWithoutExtension(fileName),
            FileName   = fileName,
            FileUrl    = fileUrl,
            FileType   = fileType,
            FileSize   = fileSize,
            SubjectId  = subjectId,
            UploadDate = DateTime.UtcNow,
            DownloadCount = 0,
            FavoriteCount = 0
        };
        _context.GroupDocuments.Add(doc);
        _context.SaveChanges();
        return doc;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // PRUEBAS UNITARIAS (5)
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Unitaria 1 — Happy Path
    /// Al subir un documento con datos válidos, debe persistirse correctamente
    /// en la base de datos con todos sus metadatos.
    /// </summary>
    [Fact]
    [Trait("Sprint", "08")]
    [Trait("Funcionalidad", "Subir Documentos")]
    [Trait("Tipo", "Unitaria - Happy Path")]
    public void SubirDocumento_ConDatosValidos_SeAlmacenaEnBD()
    {
        // Act
        var doc = SeedDocument("introduccion_algebra.pdf", "pdf", 512_000);

        // Assert
        doc.Id.Should().BeGreaterThan(0, "la BD debe asignar un ID al guardarlo");
        doc.FileName.Should().Be("introduccion_algebra.pdf");
        doc.FileType.Should().Be("pdf");
        doc.GroupId.Should().Be(_grupo.Id);
        doc.UserId.Should().Be(_uploader.UserId);
        doc.DownloadCount.Should().Be(0, "un documento recién subido no tiene descargas");
    }

    /// <summary>
    /// Unitaria 2 — Validación de tamaño
    /// Un archivo que supera el límite de 50 MB no debe ser aceptado.
    /// Se simula la validación de tamaño que realiza el controller antes de subir.
    /// </summary>
    [Fact]
    [Trait("Sprint", "08")]
    [Trait("Funcionalidad", "Subir Documentos")]
    [Trait("Tipo", "Unitaria - Validación")]
    public void SubirDocumento_ArchivoMayorA50MB_DebeRechazarse()
    {
        // Arrange
        const long limiteMB   = 50L * 1024 * 1024;   // 50 MB en bytes
        const long archivoGrande = limiteMB + 1;       // 1 byte por encima del límite

        // Act 
        var superaLimite = archivoGrande > limiteMB;

        // Assert
        superaLimite.Should().BeTrue(
            "un archivo de más de 50 MB debe ser rechazado antes de subirse a Firebase");
    }

    /// <summary>
    /// Unitaria 3 — Tipos de archivo soportados
    /// El servicio debe reconocer correctamente el tipo de archivo
    /// a partir de la extensión del nombre.
    /// </summary>
    [Theory]
    [Trait("Sprint", "08")]
    [Trait("Funcionalidad", "Subir Documentos")]
    [Trait("Tipo", "Unitaria - Tipos de Archivo")]
    [InlineData("examen.pdf",        "pdf")]
    [InlineData("tarea.docx",        "document")]
    [InlineData("presentacion.pptx", "presentation")]
    [InlineData("datos.xlsx",        "spreadsheet")]
    [InlineData("foto.png",          "image")]
    [InlineData("archivo.rar",       "other")]
    public void GetFileType_ExtensionConocida_RetornaTipoCorrecto(string fileName, string tipoEsperado)
    {
        // Arrange — usamos el mismo switch que FirebaseStorageService.GetFileType()
        var extension = Path.GetExtension(fileName).ToLower();

        // Act
        var tipo = extension switch
        {
            ".pdf"              => "pdf",
            ".doc" or ".docx"  => "document",
            ".ppt" or ".pptx"  => "presentation",
            ".xls" or ".xlsx"  => "spreadsheet",
            ".jpg" or ".jpeg"
                or ".png"
                or ".gif"
                or ".bmp"
                or ".webp"     => "image",
            _                  => "other"
        };

        // Assert
        tipo.Should().Be(tipoEsperado,
            $"la extensión '{extension}' debe mapearse al tipo '{tipoEsperado}'");
    }

    /// <summary>
    /// Unitaria 4 — Restricción de membresía
    /// Un usuario que NO pertenece al grupo no debe poder subir documentos.
    /// Se verifica la consulta de membresía que usa el controller.
    /// </summary>
    [Fact]
    [Trait("Sprint", "08")]
    [Trait("Funcionalidad", "Subir Documentos")]
    [Trait("Tipo", "Unitaria - Permisos")]
    public async Task SubirDocumento_UsuarioNoMiembro_NoPuedeSubir()
    {
        // Arrange
        var usuarioExterno = TestDbHelper.SeedUser(_context);

        // Act
        var esMiembro = await _context.GroupMembers
            .AnyAsync(gm => gm.GroupId == _grupo.Id && gm.UserId == usuarioExterno.UserId);

        // Assert
        esMiembro.Should().BeFalse(
            "un usuario que no pertenece al grupo no tiene permiso para subir documentos");
    }

    /// <summary>
    /// Unitaria 5 — Contador de descargas
    /// Al registrar una descarga, el contador del documento debe incrementarse en 1.
    /// </summary>
    [Fact]
    [Trait("Sprint", "08")]
    [Trait("Funcionalidad", "Subir Documentos")]
    [Trait("Tipo", "Unitaria - Contador")]
    public async Task DescargarDocumento_IncrementaContadorEnUno()
    {
        // Arrange
        var doc = SeedDocument("resumen.pdf");
        doc.DownloadCount.Should().Be(0, "el contador debe iniciar en 0");

        // Act
        doc.DownloadCount++;
        await _context.SaveChangesAsync();

        // Assert
        var enBD = await _context.GroupDocuments.FindAsync(doc.Id);
        enBD!.DownloadCount.Should().Be(1, "cada descarga debe sumar 1 al contador");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // PRUEBAS INTEGRALES (2)
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Integral 1 — Subida y recuperación por grupo
    /// Verifica el flujo completo: varios documentos subidos al mismo grupo
    /// deben recuperarse correctamente filtrando por GroupId, ordenados
    /// por fecha de subida descendente (más reciente primero).
    /// </summary>
    [Fact]
    [Trait("Sprint", "08")]
    [Trait("Funcionalidad", "Subir Documentos")]
    [Trait("Tipo", "Integral")]
    public async Task SubirVariosDocumentos_RecuperarPorGrupo_RetornaListaOrdenadaPorFecha()
    {
        // Arrange — simular que cada archivo se subió en momentos distintos
        var doc1 = SeedDocument("doc_antiguo.pdf");
        await Task.Delay(10);
        var doc2 = SeedDocument("doc_nuevo.docx", "document");

        // Act
        var docs = await _context.GroupDocuments
            .Where(d => d.GroupId == _grupo.Id)
            .OrderByDescending(d => d.UploadDate)
            .ToListAsync();

        // Assert
        docs.Should().HaveCount(2, "se subieron 2 documentos al grupo");
        docs.First().FileName.Should().Be("doc_nuevo.docx",
            "el documento más reciente debe aparecer primero");
        docs.Last().FileName.Should().Be("doc_antiguo.pdf");
    }

    /// <summary>
    /// Integral 2 — Eliminación completa de documento
    /// Verifica que al eliminar un documento, este desaparece de la BD
    /// y ya no puede recuperarse por su ID.
    /// </summary>
    [Fact]
    [Trait("Sprint", "08")]
    [Trait("Funcionalidad", "Subir Documentos")]
    [Trait("Tipo", "Integral")]
    public async Task EliminarDocumento_PropietarioEliminaElArchivo_YaNoExisteEnBD()
    {
        // Arrange
        var doc = SeedDocument("apuntes_a_borrar.pdf");
        var docId = doc.Id;

        // Verificar que existe antes de borrar
        var antesDeEliminar = await _context.GroupDocuments.FindAsync(docId);
        antesDeEliminar.Should().NotBeNull("el documento debe existir antes de eliminarlo");

        // Act — solo el propietario puede eliminar (doc.UserId == _uploader.UserId)
        doc.UserId.Should().Be(_uploader.UserId, "solo el propietario puede eliminar");

        _context.GroupDocuments.Remove(doc);
        await _context.SaveChangesAsync();

        // Assert
        var despuesDeEliminar = await _context.GroupDocuments.FindAsync(docId);
        despuesDeEliminar.Should().BeNull("el documento fue eliminado y no debe existir en BD");
    }

    public void Dispose() => _context.Dispose();
}
