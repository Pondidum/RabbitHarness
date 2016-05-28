using System.Text;
using Newtonsoft.Json;
using Shouldly;
using Xunit;

namespace RabbitHarness.Tests
{
	public class RawMessageHandlerDecoratorTests
	{
		private readonly RawMessageHandlerDecorator _rawHandler;

		public RawMessageHandlerDecoratorTests()
		{
			_rawHandler = new RawMessageHandlerDecorator(new DefaultMessageHandler());
		}

		[Fact]
		public void When_serializing_and_the_type_is_bytes()
		{
			var input = new byte[] { 1, 2, 3, 4, 5 };

			var serialized = _rawHandler.Serialize(input);

			serialized.ShouldBe(input);
		}

		[Fact]
		public void When_serializing_and_the_type_is_not_bytes()
		{
			var input = new Dto { Name = "Dave" };

			var serialized = _rawHandler.Serialize(input);

			var expected = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(input));
			serialized.ShouldBe(expected);
		}

		[Fact]
		public void When_deserializing_and_the_type_is_bytes()
		{
			var input = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(new { Name = "Dave" }));

			var deserialized = _rawHandler.Deserialize<byte[]>(input);

			deserialized.ShouldBe(input);
		}

		[Fact]
		public void When_deserializing_and_the_type_is_not_bytes()
		{
			var input = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(new { Name = "Dave" }));

			var deserialized = _rawHandler.Deserialize<Dto>(input);

			deserialized.Name.ShouldBe("Dave");
		}

		private class Dto
		{
			public string Name { get; set; }
		}
	}
}