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
        public static (string key, string value)[] msgTypes =
        {
            ("message","msg MSG"),
            ("trivia","msg TRV"),
            ("mind","msg MND"),
            ("selection","sel SEL"),
            ("system","msg SYS"),
            ("devil","msg DVL")
        };
        const int lipflapMinimum = 4;

        public string CreateMessageString()
        {
            string script = "";
            var messages = MessageParser.instance.parsedMessages;
            
            for (int i = 0; i < messages.Count; i++)
            {
                var message = messages[i];
                Message? prevMessage = null;

                bool isFirst = i == 0;

                if (i - 1 > -1)
                    prevMessage = messages[i - 1];

                bool generateHeader = true;
                if (prevMessage != null)
                {
                    if (message.id == prevMessage.id && message.subID == prevMessage.subID && !isFirst)
                        generateHeader = false;
                }

                Console.WriteLine(generateHeader);
                script += GenerateMessage(message,generateHeader,isFirst);
            }

            return script;
        }

        public string GenerateMessage(in Message message,bool generateHeader,bool isFirstMessage)
        {
            string generatedMessage = "";
            string messageType = msgTypes[(int)message.type].value;

            if (generateHeader && !isFirstMessage)
                generatedMessage = $"\n\n[{messageType}_{message.id:D3}_{message.subID}_0 [{message.nameplate}]]" + $"\n[s]";
            else if (isFirstMessage)
                generatedMessage = $"[{messageType}_{message.id:D3}_{message.subID}_0 [{message.nameplate}]]" + $"\n[s]";
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

            if (isMessage && message.msg.Length >= lipflapMinimum)
            {
                generatedMessage += $"[f 4 10 -1 0 0]";
            }

            generatedMessage += message.type == MessageType.SEL ? $"{message.msg}" : $"{message.msg}[n]";

            if (message.type == MessageType.MSG)
            {
                generatedMessage += "[f 1 3 -1]";
            }

            generatedMessage += message.type == MessageType.SEL ? "[e]" : "[w][e]";

            return generatedMessage;
        }
    }
}
