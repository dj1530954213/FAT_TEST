using FatFullVersion.Services.Interfaces;

namespace FatFullVersion.Services
{
    public class MessageService : IMessageService
    {
        public string GetMessage()
        {
            return "Hello from the Message Service";
        }
    }
} 