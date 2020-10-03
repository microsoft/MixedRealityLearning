namespace MRTK.Tutorials.AzureCloudServices.Scripts.BotDirectLine
{
    public class ConversationState
    {
        public string ConversationId
        {
            get;
            set;
        }

        /// <summary>
        /// ID of the previously sent message.
        /// </summary>
        public string PreviouslySentMessageId
        {
            get;
            set;
        }

        public string Watermark
        {
            get;
            set;
        }
    }
}