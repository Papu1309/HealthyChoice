using System.Windows;

namespace HealthyChoice
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            DispatcherUnhandledException += App_DispatcherUnhandledException;
        }

        private void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            MessageBox.Show($"Произошла ошибка: {e.Exception.Message}",
                          "Ошибка",
                          MessageBoxButton.OK,
                          MessageBoxImage.Error);
            e.Handled = true;
        }
    }


}