namespace CodestrikerPlugin.Util
{
    public static class OptionPageProxy
    {       
        public static string OptionUsername => CodestrikerToolWindowCommand.Instance.OptionUsername;
        public static string OptionPassword => CodestrikerToolWindowCommand.Instance.OptionPassword;
        public static string OptionEmail => CodestrikerToolWindowCommand.Instance.OptionEmail;
        public static string OptionCodestrikerUrl => CodestrikerToolWindowCommand.Instance.OptionCodestrikerURL;
      
    }
}
