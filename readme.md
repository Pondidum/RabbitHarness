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

You can also listen to Exchanges (creating an autoqueue, or specified) and specify routing keys for exchange listeners.


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
