using Domain.Entities;
using Domain.Entities.Base;
using Domain.Exceptions;
using Application.Common.Exceptions;

namespace UnitTests.Domain;

public class DomainTests
{
    [Fact]
    public void ApiKey_ShouldInheritDefaultEntityValues()
    {
        var before = DateTime.UtcNow.AddSeconds(-1);

        var apiKey = new ApiKey
        {
            Key = "my-key",
            Revoked = false
        };

        apiKey.Id.Should().NotBeEmpty();
        apiKey.CreatedAt.Should().BeAfter(before);
        apiKey.Key.Should().Be("my-key");
        apiKey.Revoked.Should().BeFalse();
    }

    [Fact]
    public void Entity_TestSubclass_ShouldCreateDefaultValues()
    {
        var entity = new TestEntity();

        entity.Id.Should().NotBeEmpty();
        entity.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void DomainException_ShouldStoreMessageAndInnerException()
    {
        var innerException = new InvalidOperationException("inner");

        var exception = new DomainException("domain-error", innerException);

        exception.Message.Should().Be("domain-error");
        exception.InnerException.Should().Be(innerException);
    }

    [Fact]
    public void UnauthorizedException_ShouldUseDefaultMessage()
    {
        var exception = new UnauthorizedException();

        exception.Message.Should().Be("Não autorizado");
    }

    private sealed class TestEntity : Entity
    {
    }
}
