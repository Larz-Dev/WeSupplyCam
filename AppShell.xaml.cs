using System.Windows.Input;

namespace WeSupplyCam
{
    public partial class AppShell : Shell
    {

        public ICommand TapCommand => new Command<string>(async (url) => await Launcher.OpenAsync(url));

        public AppShell()
        {
            InitializeComponent();
            BindingContext = this; // Asegúrate de que el binding context esté configurado
     
        }
      
    }
}
