using Kalkulator_Wyborczy.Data;
using Kalkulator_Wyborczy.Services;
using System.Net;
using System.Threading.Tasks;
using System.Windows;

namespace Kalkulator_Wyborczy
{

    public partial class Login : Window
    {
        Validation validation;
        Cryptography cryptography;
        Credentials credentials;
        public Login()
        {           
            InitializeComponent();
            if (Properties.Settings.Default.UserPESEL != string.Empty) {
                var mainWindow = new MainWindow();
                mainWindow.Show();
                this.Close();
            }

            validation = new Validation();
            cryptography = new Cryptography();
            credentials = new Credentials();

            Task task = new Task(() => CheckConnection());
            task.Start();

        }

        private void switchRegLog_Click(object sender, RoutedEventArgs e) {
            if (loginLayout.Visibility == Visibility.Visible) {
                loginLayout.Visibility = Visibility.Hidden;
                registerLayout.Visibility = Visibility.Visible;

                loginWindowHeader.Text = "sign up";
                switchRegLog.Content = "Already registered? Sign in.";
            } else {
                loginLayout.Visibility = Visibility.Visible;
                registerLayout.Visibility = Visibility.Hidden;

                loginWindowHeader.Text = "sign in";
                switchRegLog.Content = "First time here? Register.";
            }
        }

        /// <summary>
        /// bool method to check if client is connected to the Internet
        /// </summary>
        /// <returns></returns>
        private bool ConnectionBroadcast() {
            try
            {
                using (var client = new WebClient())
                using (client.OpenRead("https://votingcalculator.firebaseio.com"))
                {
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// switches warning box visibility
        /// </summary>
        private void CheckConnection() {
            while (true) {
                bool status = ConnectionBroadcast();

                this.Dispatcher.Invoke(() => {
                    if (status) {
                        connectionStatus.Visibility = Visibility.Collapsed;
                        regButton.IsEnabled = true;
                        LogButton.IsEnabled = true;
                    }
                    else {
                        connectionStatus.Visibility = Visibility.Visible;
                        regButton.IsEnabled = false;
                        LogButton.IsEnabled = false;
                    }
                });
            }
        }

        private async void regButton_Click(object sender, RoutedEventArgs e)
        {
            UserCredentials userCredentials = new UserCredentials()
            {
                Name = regName.Text,
                Surname = regSurname.Text,
                PESEL = regPesel.Text,
                Password = regPassword.Password,
            };

           await credentials.CreateNewUserAsync(userCredentials);
        }

        private async void LogButton_Click(object sender, RoutedEventArgs e) {
            Data.Login loginInstance = new Data.Login()
            {
                PESEL = loginPesel.Text,
                Password = loginPassword.Password
            };

            var result = await credentials.LoginUser(loginInstance);

            if (result)
            {
                var mainWindow = new MainWindow();
                mainWindow.Show();
                this.Hide();
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            //kill all app processes while closing window
            e.Cancel = true;
            Application.Current.Shutdown();
        }
    }
}
