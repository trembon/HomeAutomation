using HomeAutomation.Entities;
using HomeAutomation.Entities.Action;
using HomeAutomation.Models.Actions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace HomeAutomation.Tests.Entities.Action
{
    public class DelayActionTests
    {
        private DelayAction delayAction;

        public DelayActionTests()
        {
            this.delayAction = new DelayAction();
        }

        [Fact]
        public void Execute_Should_Not_Execute_Anything_On_Empty_Actions()
        {
            var arguments = new Mock<IActionExecutionArguments>();
            arguments.SetupAllProperties();

            delayAction.Execute(arguments.Object);

            arguments.VerifyGet(x => x.Source, Times.Never);
            arguments.Verify(x => x.GetService<IServiceScopeFactory>(), Times.Never);
        }

        [Fact]
        public void Execute_Should_Be_Called_After_Delay()
        {
            var serviceScopeMock = CreateServiceScopeFactoryMock();
            var arguments = CreateActionExecutionArgumentsMock(serviceScopeMock);

            delayAction.Delay = TimeSpan.FromMilliseconds(10);
            delayAction.Actions = new int[0];

            delayAction.Execute(arguments.Object);

            Thread.Sleep(5);

            serviceScopeMock.Verify(x => x.CreateScope(), Times.AtLeastOnce);

            Thread.Sleep(20);

            arguments.VerifyGet(x => x.Source, Times.Once);
            arguments.Verify(x => x.GetService<IServiceScopeFactory>(), Times.Once);
            serviceScopeMock.Verify(x => x.CreateScope(), Times.Exactly(2));
        }

        [Fact]
        public void Execute_Should_Extend_Delay()
        {
            var serviceScopeMock = CreateServiceScopeFactoryMock();
            var arguments = CreateActionExecutionArgumentsMock(serviceScopeMock);

            delayAction.Delay = TimeSpan.FromMilliseconds(10);
            delayAction.Actions = new int[0];
            delayAction.Extend = true;

            delayAction.Execute(arguments.Object);
            delayAction.Execute(arguments.Object);

            Thread.Sleep(5);

            serviceScopeMock.Verify(x => x.CreateScope(), Times.AtLeast(2));

            Thread.Sleep(30);

            serviceScopeMock.Verify(x => x.CreateScope(), Times.Exactly(3));
        }

        private Mock<IActionExecutionArguments> CreateActionExecutionArgumentsMock(Mock<IServiceScopeFactory> serviceScopeFactoryMock)
        {
            var entity = new Mock<IEntity>();
            entity.SetupAllProperties();

            var arguments = new Mock<IActionExecutionArguments>();
            arguments.SetupGet(x => x.Source).Returns(entity.Object);
            arguments.Setup(x => x.GetService<IServiceScopeFactory>()).Returns(serviceScopeFactoryMock.Object);

            return arguments;
        }

        private Mock<IServiceScopeFactory> CreateServiceScopeFactoryMock()
        {
            var logger = new Mock<ILogger<DelayAction>>();

            var serviceProvider = new Mock<IServiceProvider>();
            serviceProvider.Setup(x => x.GetService(It.Is<Type>(x => x.Name.Contains("ILogger")))).Returns(logger.Object);

            var serviceScope = new Mock<IServiceScope>();
            serviceScope.SetupGet(x => x.ServiceProvider).Returns(serviceProvider.Object);

            var mock = new Mock<IServiceScopeFactory>();
            mock.Setup(x => x.CreateScope()).Returns(serviceScope.Object);

            return mock;
        }
    }
}
