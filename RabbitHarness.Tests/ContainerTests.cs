using RabbitMQ.Client;
using Shouldly;
using StructureMap;
using StructureMap.Graph;
using Xunit;

namespace RabbitHarness.Tests
{
	public class ContainerTests : TestBase
	{
		[Fact]
		public void When_constructed_by_structuremap_with_no_special_config()
		{
			var container = new Container(c =>
			{
				c.For<ConnectionFactory>().Use(Factory);
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
					new QueueDefinition { Name = QueueName, AutoDelete = true },
					new LambdaMessageHandler<int>((props, message) => true));

				unsub();
			});
		} 
	}
}
