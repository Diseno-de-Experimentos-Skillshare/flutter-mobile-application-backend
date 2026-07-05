using NUnit.Framework;
using Moq;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SkillShareBackend.Controllers;
using SkillShareBackend.Data;
using SkillShareBackend.DTOs;
using SkillShareBackend.Models;
using SkillShareBackend.Services;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace SkillShareBackend.Tests
{
    [TestFixture]
    public class FcmToken_NUnit_Tests
    {
        private AppDbContext _context = null!;
        private Mock<IAuthService> _authServiceMock = null!;
        private Mock<ILogger<AuthController>> _loggerMock = null!;
        private AuthController _controller = null!;

        [SetUp]
        public void Setup()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _context = new AppDbContext(options);

            _authServiceMock = new Mock<IAuthService>();
            _loggerMock = new Mock<ILogger<AuthController>>();

            _controller = new AuthController(_authServiceMock.Object, _loggerMock.Object, _context);

            // Mock user identity claims for UserId = 1
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "1"),
                new Claim("userId", "1")
            };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            var claimsPrincipal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = claimsPrincipal }
            };

            // Seed initial database user using correct fields
            _context.Users.Add(new User
            {
                UserId = 1,
                Email = "testuser@skillshare.com",
                Password = "hashedpassword123",
                FcmToken = "original-token"
            });
            _context.SaveChanges();
        }

        [TearDown]
        public void TearDown()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }

        [Test]
        public async Task UpdateFcmToken_WhenTokenIsEmpty_UpdatesFcmTokenToNullInDatabase()
        {
            // Arrange: FCM token sent as empty string (deactivation case)
            var dto = new UpdateFcmTokenDto
            {
                Token = "",
                SessionRemindersEnabled = false
            };

            // Act: Call controller endpoint
            var result = await _controller.UpdateFcmToken(dto);

            // Assert: Response is 200 OK and FCM token is null in database
            Assert.That(result, Is.InstanceOf<OkObjectResult>());
            var dbUser = await _context.Users.FirstOrDefaultAsync(u => u.UserId == 1);
            Assert.That(dbUser, Is.Not.Null);
            Assert.That(dbUser!.FcmToken, Is.Null);
        }

        [Test]
        public async Task UpdateFcmToken_WhenTokenIsNull_UpdatesFcmTokenToNullInDatabase()
        {
            // Arrange: FCM token sent as null
            var dto = new UpdateFcmTokenDto
            {
                Token = null,
                SessionRemindersEnabled = true
            };

            // Act: Call controller endpoint
            var result = await _controller.UpdateFcmToken(dto);

            // Assert: Response is 200 OK and FCM token is null in database
            Assert.That(result, Is.InstanceOf<OkObjectResult>());
            var dbUser = await _context.Users.FirstOrDefaultAsync(u => u.UserId == 1);
            Assert.That(dbUser, Is.Not.Null);
            Assert.That(dbUser!.FcmToken, Is.Null);
            Assert.That(dbUser.SessionRemindersEnabled, Is.True);
        }

        [Test]
        public async Task UpdateFcmToken_WhenTokenIsValid_UpdatesFcmTokenInDatabase()
        {
            // Arrange: A valid new token is sent
            var dto = new UpdateFcmTokenDto
            {
                Token = "new-valid-token-value",
                SessionRemindersEnabled = true
            };

            // Act: Call controller endpoint
            var result = await _controller.UpdateFcmToken(dto);

            // Assert: Response is 200 OK and database is updated
            Assert.That(result, Is.InstanceOf<OkObjectResult>());
            var dbUser = await _context.Users.FirstOrDefaultAsync(u => u.UserId == 1);
            Assert.That(dbUser, Is.Not.Null);
            Assert.That(dbUser!.FcmToken, Is.EqualTo("new-valid-token-value"));
        }
    }
}
