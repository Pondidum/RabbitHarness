using System;
using System.Runtime.Remoting.Contexts;
using System.Text;
using System.Threading;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Shouldly;
using Xunit.Abstractions;

namespace RabbitHarness.Tests
{
	public class RabbitConnectorTests : TestBase
	{
		private ITestOutputHelper _output;

		public RabbitConnectorTests(ITestOutputHelper output)
		{
			_output = output;
		}

		[RequiresRabbitFact(Host)]
		public void When_creating_a_queue_and_listening()
		{
			var connector = new RabbitConnector(Factory);

			var reset = new AutoResetEvent(false);
			var received = "";

			var unsubscribe = connector.ListenTo(
				QueueName,
				queue =>
				{
					queue.AutoDelete();
					queue.DeclareQueue();
				},
				(props, json) =>
				{
					received = json;
					reset.Set();
					return true;
				});

			connector.Send(QueueName, props => { }, new { Name = "Test" });

			reset.WaitOne(TimeSpan.FromSeconds(10));

			received.ShouldBe("{\"Name\":\"Test\"}");

			unsubscribe();
		}

		[RequiresRabbitFact(Host)]
		public void When_a_reply_is_sent()
		{

			Action<QueueDeclaration> declare = queue =>
			{
				queue.AutoDelete();
				queue.DeclareQueue();
			};

			var connector = new RabbitConnector(Factory);

			var reset = new AutoResetEvent(false);
			var received = "";

			connector.ListenTo(QueueName, declare, (props, json) =>
			{
				connector.Send(
					props.ReplyTo,
					rp => { rp.CorrelationId = props.CorrelationId; },
					new { Name = "Reply" });

				return true;
			});

			connector.Query(
				QueueName,
				declare,
				props => { },
				new { Name = "QueryTest" },
				(props, json) =>
				{
					received = json;
					reset.Set();
					return true;
				});

			reset.WaitOne(TimeSpan.FromSeconds(10));
			received.ShouldBe("{\"Name\":\"Reply\"}");
		}

		[RequiresRabbitFact(Host)]
		public void When_nothing_is_listening()
		{
			var reset = new AutoResetEvent(false);
			var message = new { Message = "message" };
			var received = false;

			var connector = new RabbitConnector(Factory);

			connector.Query(QueueName, declare => { declare.AutoDelete(); declare.DeclareQueue(); }, props => { }, message, (props, json) =>
			{
				received = true;
				reset.Set();
				return true;
			});

			reset.WaitOne(TimeSpan.FromSeconds(5));
			received.ShouldBe(false);
		}

		[RequiresRabbitFact(Host)]
		public void When_something_else_responds()
		{
			CreateResponder(p => p.CorrelationId = Guid.NewGuid().ToString());

			var reset = new AutoResetEvent(false);
			var message = new { Message = "message" };
			var received = false;

			var connector = new RabbitConnector(Factory);

			connector.Query(QueueName, declare => { declare.AutoDelete(); declare.DeclareQueue(); }, props => { }, message, (props, json) =>
			{
				received = true;
				reset.Set();
				return true;
			});

			reset.WaitOne(TimeSpan.FromSeconds(5));
			received.ShouldBe(false);
		}
	}
}
