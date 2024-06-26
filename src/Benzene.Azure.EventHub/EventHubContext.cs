﻿using Azure.Messaging.EventHubs;

namespace Benzene.Azure.EventHub;

public class EventHubContext// : IHasMessageResult
{
    private EventHubContext(EventData eventData)
    {
        EventData = eventData;
    }
    public static EventHubContext CreateInstance(EventData eventData)
    {
        return new EventHubContext(eventData);
    }
    
    public EventData EventData { get; }
    // public IMessageResult? MessageResult { get; set; }
}
