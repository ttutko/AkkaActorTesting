namespace AkkaActorTesting.Messages
{
    public class MessageQueueMessage
    {
        public string Content {get;set;}

        public MessageQueueMessage(string content)
        {
            Content = content;
        }
    }
}