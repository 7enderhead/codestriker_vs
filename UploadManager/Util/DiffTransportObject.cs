namespace UploadManager.Util
{
    // transport object to transmit the necessary data to create a
    // new topic for Codestriker
    public class DiffTransportObject
    {
        #region Properties
        public string Action { get; set; } = "submit_new_topic";
        public string Obsoletes { get; set; } = string.Empty;
        public string TopicTitle { get; set; } = string.Empty;
        public string TopicDescription { get; set; } = string.Empty;
        public string Filename { get; set; } = string.Empty;
        public string StartTag { get; set; } = string.Empty;
        public string EndTag { get; set; } = string.Empty;
        public string Module { get; set; } = string.Empty;
        public string Repository { get; set; } = string.Empty;
        public string ProjectId { get; set; } = string.Empty;
        public string BugIds { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Reviewers { get; set; } = string.Empty;
        public string EmailCc { get; set; } = string.Empty;
        public string TopicState { get; set; } = string.Empty;
        public string SubmitName { get; set; } = string.Empty;
        public string DiffString { get; set; } = string.Empty;
        #endregion

        public bool CheckFields()
        {
            if (string.IsNullOrEmpty(TopicTitle)) return false;
            if (string.IsNullOrEmpty(TopicDescription)) return false;
            if (string.IsNullOrEmpty(Email)) return false;
            if (string.IsNullOrEmpty(Reviewers)) return false;

            return true;
        }
    }
}
