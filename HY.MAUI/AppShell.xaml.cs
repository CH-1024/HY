using HY.MAUI.PageModels.Contact;

namespace HY.MAUI
{
    public partial class AppShell : Shell
    {
        public AppShell(AppShellModel appShell)
        {
            InitializeComponent();

            BindingContext = appShell;
        }
    }
}
