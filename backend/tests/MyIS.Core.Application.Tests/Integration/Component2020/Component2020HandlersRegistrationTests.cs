using Microsoft.Extensions.DependencyInjection;
using Moq;
using MyIS.Core.Application.DependencyInjection;
using MyIS.Core.Application.Integration.Component2020.Abstractions;
using MyIS.Core.Application.Integration.Component2020.Handlers;

namespace MyIS.Core.Application.Tests.Integration.Component2020;

public class Component2020HandlersRegistrationTests
{
    [Fact]
    public void AddApplication_Registers_GetComponent2020SyncRunErrorsHandler()
    {
        var services = new ServiceCollection();
        services.AddApplication();

        services.AddScoped(_ => new Mock<IComponent2020SyncRunRepository>().Object);

        using var provider = services.BuildServiceProvider();
        var handler = provider.GetRequiredService<GetComponent2020SyncRunErrorsHandler>();

        handler.Should().NotBeNull();
    }
}

