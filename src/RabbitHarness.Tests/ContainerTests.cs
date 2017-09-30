using NSubstitute;
using RabbitMQ.Client;
using Shouldly;
using StructureMap;
using StructureMap.Graph;
using Xunit;

namespace RabbitHarness.Tests
{
	public class ContainerTests
	{
		[Fact]
		public void When_constructed_by_structuremap_with_no_special_config()
		{
			var factory = Substitute.For<IConnectionFactory>();
			var connection = Substitute.For<IConnection>();
			factory.CreateConnection().Returns(connection);

			var container = new Container(c =>
			{
				c.For<IConnectionFactory>().Use(factory);
				c.Scan(a =>
				{
					a.TheCallingAssembly();
					a.AssemblyContainingType<IRabbitConnector>();
					a.WithDefaultConventions();
				});
			});

			//if the wrong ctor was called, the factory would be null, and throw a null-ref exception
			Should.NotThrow(() =>
			{
				var connector = container.GetInstance<RabbitConnector>();

				var unsub = connector.ListenTo(
					new QueueDefinition { Name = "test", AutoDelete = true },
					new LambdaMessageHandler<int>((props, message) => true));

				unsub();
			});

			factory.Received().CreateConnection();
			connection.Received().CreateModel();
		} 
	}
}
