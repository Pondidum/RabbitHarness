 # RabbitHarness
 Small abstraction around common operations on RabbitMQ.

# Usage

## Listening to a Queue

```CSharp
public class RabbitListener : IDisposable
{
    private readonly IRabbitConnector _connector;
    private readonly Action _unsubscribe;

    public RabbitListener()
    {
        _connector = new RabbitConnector(new ConnectionFactory
        {
            HostName = "localhost"
        });

        _unsubscribe = connector.ListenTo(
            new QueueDefintion { Name = "TestQueue", AutoDelete = true },
            new LambdaMessageHandler<MessageDto>(OnReceive, OnException)
        );
    }

    private void OnReceive(IBasicProperties props, MessageDto message)
    {
    }

    private void OnException(Exception ex)
    {      
    }

    public void Dispose()
    {
        _unsubscribe();
    }
}
```

You can also listen to Exchanges (and optionally binding with a specifed queue.) You can also specify routing keys for exchange listeners.

If you call `.ListenTo<byte[]>`, RabbitHarness will just return the raw byte content of the message recieved, rather than attempting json deserialization (if you'd rather use something other than Json, see the Customisation - Serialization section below.)

## Sending a Message

The objects passed in the message parameter of `SendTo` are serialized as JSON, and sent to the queue or exchange specified.

```CSharp
object message = new { Name = "Dave", Location = "Home" };

connector.SendTo(
    new QueueDefintion { Name = "Test" },
    props => {},
    message
);

connector.SendTo(
    new ExchangeDefiniton("Test", ExchangeTypes.Fanout),
    props => {},
    message
);

connector.SendTo(
    new ExchangeDefiniton("Test", ExchangeTypes.Fanout),
    "some.routing.key",
    props => {},
    message
);
```

You can also customise the properties, setting things like `correlationid` and `timestamp`.  Note by using the `TimeCache` class, you get a timestamp based off of `pool.ntp.org`.

```CSharp
connector.SendTo(
    new QueueDefintion { Name = "Test" },
    props => props.Timestamp = TimeCache.Now(),
    message
);
```

## Querying A Queue or Exchange

The `Query` method will allow you to get a single response from a Queue or Exchange.  It handles the setting of correlationids and response queues to do this for you.

You can also set your own correlationid using the props lambda if you wish - but make sure it's a unique value!

```CSharp
_connector
    .Query<int>(queue, props => { }, message)
    .ContinueWith(response =>
    {
        var props = response.Result.Properties;
        var message = response.Result.Message;
    })
    .Wait(); //Don't call wait() if you want this to be completely async!
```

As with `.ListenTo()`, this works with exchanges and exchanges with routing keys.

# Customisation

## Serialization

You can override the default Json Serializer with your own implementation.

```CSharp
//normal construction
_connector = new RabbitConnector(factory);

//extended construction
_connector = new RabbitConnector(config => {
    config.Factory = factory;
    config.Serializer = new CustomSerializer();
});
```

As an example, the default implementation of the `DefaultMessageSerializer` is as follows:

```CSharp
public class DefaultMessageSerializer : IMessageSerializer
{
    private readonly JsonSerializerSettings _jsonSettings;

    public DefaultMessageSerializer() : this(new JsonSerializerSettings())
    {
    }

    public DefaultMessageSerializer(JsonSerializerSettings jsonSettings)
    {
        _jsonSettings = jsonSettings;
    }

    public byte[] Serialize(object message)
    {
        var json = JsonConvert.SerializeObject(message, _jsonSettings);
        return Encoding.UTF8.GetBytes(json);
    }

    public TMessage Deserialize<TMessage>(byte[] bytes)
    {
        var json = Encoding.UTF8.GetString(bytes);
        return JsonConvert.DeserializeObject<TMessage>(json, _jsonSettings);
    }
}
```
