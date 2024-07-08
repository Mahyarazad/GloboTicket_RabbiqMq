using System;

namespace GloboTicket.Integration.Messages
{
    public static class ConsoleHelper
    {
        public enum MessageType
        {
            Recieved, Published
        }

        public static void WriteLine(string text, MessageType type)
        {
            if(type == MessageType.Published)
            {
                Console.BackgroundColor = ConsoleColor.Black;
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine("***********   Published   *************");
            }
            else
            {
                Console.BackgroundColor = ConsoleColor.White;
                Console.ForegroundColor = ConsoleColor.Black;
                Console.WriteLine("***********   Received   *************");
            }

            Console.WriteLine(text);
            Console.WriteLine("**************************************");
            Console.ResetColor();
        }
    }
}
