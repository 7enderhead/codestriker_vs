using System.Windows.Forms;

namespace CodestrikerPlugin.OptionPage
{
    public partial class CodestrikerOptionPage : UserControl
    {
        public CodestrikerOptionPage()
        {
            InitializeComponent();
        }

        internal CodestrikerOptionPageGrid OptionsPage;

        public void Initialize()
        {
            txt_username.Text = OptionsPage.OptionUsername;
            txt_password.Text = OptionsPage.OptionPassword;
            txt_password.PasswordChar = '•';
            txt_email.Text = OptionsPage.OptionEmail;
            txt_codestriker_url.Text = OptionsPage.OptionCodestrikerUrl;

            label2.Text = Resources.Password;
            label3.Text = Resources.Email;
            label4.Text = Resources.CodestrikerUrl;
        }

        private void txt_email_KeyUp(object sender, KeyEventArgs e)
        {
            OptionsPage.OptionEmail = txt_email.Text;
        }

        private void txt_password_KeyUp(object sender, KeyEventArgs e)
        {
            OptionsPage.OptionPassword = txt_password.Text;
        }

        private void txt_username_KeyUp(object sender, KeyEventArgs e)
        {
            OptionsPage.OptionUsername = txt_username.Text;
        }

        private void txt_Codestriker_URL_TextChanged(object sender, System.EventArgs e)
        {
            OptionsPage.OptionCodestrikerUrl = txt_codestriker_url.Text;
        }
    }
}
