using System;

namespace UploadManager.Util
{
    public class LinkItem
    {
        public string Href { get; set; }
        public string Text { get; set; }

        public override string ToString()
        {
            if (string.IsNullOrEmpty(Href) || string.IsNullOrEmpty(Text))
                return string.Empty;
            return $"{Href}{Environment.NewLine}\t{Text}";
        }
    }
}
