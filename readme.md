 # RabbitHarness
 Small abstraction around common operations on RabbitMQ.

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
