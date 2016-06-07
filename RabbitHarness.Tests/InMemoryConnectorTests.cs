using System;
using System.Threading;
using RabbitMQ.Client;
using Shouldly;
using Xunit;

namespace RabbitHarness.Tests
{
	public class InMemoryConnectorTests
	{
		private InMemoryConnector _connector;
		private AutoResetEvent _reset;
		private readonly ExchangeDefinition _exchangeDefinition;
		private readonly QueueDefinition _queueDefinition;

		public InMemoryConnectorTests()
		{
			_reset = new AutoResetEvent(false);
			_connector = new InMemoryConnector();

			_exchangeDefinition = new ExchangeDefinition("Exchange" + Guid.NewGuid().ToString(), ExchangeType.Direct)
			{
				AutoDelete = true
			};

			_queueDefinition = new QueueDefinition
			{
				Name = "Queue" + Guid.NewGuid().ToString(),
				AutoDelete = true,
				RoutingKeys = new[] { "some.key" }
			};
		}

		[Fact]
		public void When_using_a_routing_key_which_matches_exactly()
		{
			int recieved = 0;

			_connector.ListenTo<int>(
				_exchangeDefinition,
				_queueDefinition,
				(props, json) =>
				{
					recieved = json;
					_reset.Set();
					return true;
				});

			_connector.SendTo(_exchangeDefinition, "some.key", props => { }, 123);
			_reset.WaitOne(TimeSpan.FromSeconds(5));

			recieved.ShouldBe(123);
		}

		[Fact]
		public void When_using_a_routing_key_which_doesnt_match()
		{
			int recieved = 0;

			_connector.ListenTo<int>(
				_exchangeDefinition,
				_queueDefinition,
				(props, json) =>
				{
					recieved = json;
					_reset.Set();
					return true;
				});

			_connector.SendTo(_exchangeDefinition, "another.thing", props => { }, 123);
			_reset.WaitOne(TimeSpan.FromSeconds(5));

			recieved.ShouldBe(0);
		}

		[Fact]
		public void When_using_a_routing_key_with_a_wildcard()
		{
			int recieved = 0;

			var queue = new QueueDefinition {
				Name = "Queue" + Guid.NewGuid().ToString(),
				AutoDelete = true,
				RoutingKeys = new[] { "prefix.*" }
			};

			_connector.ListenTo<int>(
				_exchangeDefinition,
				queue,
				(props, message) =>
				{
					recieved = message;
					_reset.Set();
					return true;
				});

			_connector.SendTo(_exchangeDefinition, "prefix.key", props => { }, 123);
			_reset.WaitOne(TimeSpan.FromSeconds(5));

			recieved.ShouldBe(123);
		}

		[Theory]
		[InlineData("exact.key", "exact.key", true)]
		[InlineData("exact.key", "exact.other", false)]
		[InlineData("exact.key", "other.key", false)]
		[InlineData("#", "exact.key", true)]
		[InlineData("exact.*", "exact.key", true)]
		[InlineData("exact.*", "other.key", false)]
		[InlineData("exact.*.key", "exact.matching.key", true)]
		[InlineData("exact.*.key", "exact.non.matching", false)]
		public void Routing_key_tests(string handlerKey, string messageKey, bool matches)
		{
			InMemoryConnector.RoutingKeyMatches(handlerKey, messageKey).ShouldBe(matches);
		}
	}
}