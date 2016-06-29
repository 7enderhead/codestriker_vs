using Microsoft.VisualStudio.Shell;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace CodestrikerPlugin.OptionPage
{
   
    [Guid("00000000-0000-0000-0000-000000000000")]
    public class CodestrikerOptionPageGrid : DialogPage
    {
        public string OptionUsername { get; set; }
        public string OptionPassword { get; set; }
        public string OptionEmail { get; set; }
        public string OptionCodestrikerUrl { get; set; }

        protected override IWin32Window Window
        {
            get
            {
                CodestrikerOptionPage page = new CodestrikerOptionPage { OptionsPage = this };
                page.Initialize();
               
                return page;
            }
        }
    }
}
