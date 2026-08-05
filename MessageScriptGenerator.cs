using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BloomScriptConverter
{

    public sealed class MessageScriptGenerator
    {
        public static MessageScriptGenerator Instance = new MessageScriptGenerator();
        string[] msgTypes =
        {
            "msg MSG",
            "msg TRV",
            "msg MND",
            "sel SEL",
            "msg SYS",
            "msg DVL"
        };
        const int lipflapMinimum = 4;

        public string CreateMessageString()
        {
            string script = "";
            var messages = MessageParser.instance.parsedMessages;
            
            foreach (var message in messages)
            {
                script += GenerateMessage(message);
            }

            return script;
        }

        public string GenerateMessage(in Message message)
        {
            string generatedMessage = "";
            string messageType = msgTypes[(int)message.type];

            if (message.id != null)
                generatedMessage = $"\n\n[{messageType}_{message.id:D3}_0_0 [{message.nameplate}]]\n" +
                    $"[s]";
            else
                generatedMessage = $"\n[s]";


            bool isMessage = message.type == MessageType.MSG ? true : false;

            if (message.bup != null && isMessage)
            {
                var bup = message.bup;
                char phoneValue = bup.Value.phone ? '1' : '0';
                generatedMessage += $"[bup 0 {bup.Value.character} {bup.Value.bustupID} {bup.Value.outfit} {phoneValue}]";
            }

            if (message.points != null && isMessage)
            {
                var points = message.points;
                generatedMessage += $"[f 5 13 {points.Value.confidantID} {points.Value.confidantPoints * 5} 1]";
            }

            generatedMessage += message.type == MessageType.SEL ? $"{message.msg}" : $"{message.msg}[n]";

            if (isMessage && message.msg.Length >= lipflapMinimum)
            {
                generatedMessage += $"[f 4 10 65535 0 0]";
            }

            generatedMessage += message.type == MessageType.SEL ? "[e]" : "[w][e]";

            return generatedMessage;
        }
    }
}
