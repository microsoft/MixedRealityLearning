using System;
using System.Collections.Generic;

namespace MRTK.Tutorials.AzureCloudServices.Scripts.BotDirectLine
{
    public enum EventTypes
    {
        None,
        ConversationStarted,
        MessageSent,
        MessageReceived, // Can be 1 or more messages
        Error
    }

    public class BotResponseEventArgs : EventArgs
    {
        public EventTypes EventType
        {
            get;
            set;
        }
        
        public string SentMessageId
        {
            get;
            set;
        }

        public string ConversationId
        {
            get;
            set;
        }

        /// <summary>
        /// Can contain e.g. an error code.
        /// </summary>
        public string Code
        {
            get;
            set;
        }

        /// <summary>
        /// Not an actual message but e.g. an error message.
        /// </summary>
        public string Message
        {
            get;
            set;
        }

        public string Watermark
        {
            get;
            set;
        }

        public IList<MessageActivity> Messages
        {
            get;
            private set;
        }

        public BotResponseEventArgs()
        {
            EventType = EventTypes.None;
            Messages = new List<MessageActivity>();
        }

        public override string ToString()
        {
            string retval = "[Event type: " + EventType;

            if (!string.IsNullOrEmpty(SentMessageId))
            {
                retval += "; ID of message sent: " + SentMessageId;
            }

            if (!string.IsNullOrEmpty(Code))
            {
                retval += "; Code: " + Code;
            }

            if (!string.IsNullOrEmpty(Message))
            {
                retval += "; Message: " + Message;
            }

            if (!string.IsNullOrEmpty(ConversationId))
            {
                retval += "; Conversation ID: " + ConversationId;
            }

            if (!string.IsNullOrEmpty(Watermark))
            {
                retval += "; Watermark: " + Watermark;
            }

            if (Messages.Count > 0)
            {
                retval += "; Messages: ";

                for (int i = 0; i < Messages.Count; ++i)
                {
                    retval += "\"" + Messages[i] + "\"";

                    if (i < Messages.Count - 1)
                    {
                        retval += ", ";
                    }
                }
            }

            retval += "]";
            return retval;
        }
    }
}