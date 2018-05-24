using Firebase.Database;
using Firebase.Database.Query;
using System;
using System.Net;
using System.Threading.Tasks;
using System.Windows;

namespace Kalkulator_Wyborczy
{

    public partial class Login : Window
    {
        private readonly int[] checks = { 9, 7, 3, 1, 9, 7, 3, 1, 9, 7 };
        public Login()
        {           
            InitializeComponent();
            if (Properties.Settings.Default.UserPESEL != string.Empty) {
                var mainWindow = new MainWindow();
                mainWindow.Show();
                this.Close();
            }

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
       
        private bool checkPESEL(string pesel) {
            bool rightpesel = false;
            if (pesel.Length == 11) {
                rightpesel = checkPESELSUM(pesel).Equals(pesel[10].ToString());
                return rightpesel;
            } else {
                return rightpesel;
            }
        }

        private string checkPESELSUM(string pesel) {
            int sum = 0;
            for (int i = 0; i < checks.Length; i++) {
                sum += checks[i] * int.Parse(pesel[i].ToString());
            }
            return (sum % 10).ToString();
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
            var firebase = new FirebaseClient("https://votingcalculator.firebaseio.com").Child("voters");
            //password and PESEL ecryption
            string encryptedpassword = Cryptography.Encode(regPassword.Password);
        string encryptedPESEL = Cryptography.Encode(regPesel.Text);
            //check if everything is provided
            if (!string.IsNullOrWhiteSpace(regName.Text) && !string.IsNullOrWhiteSpace(regPassword.Password) && !string.IsNullOrWhiteSpace(regSurname.Text) && !string.IsNullOrWhiteSpace(regPesel.Text)) {
                //check PESEL
                if (checkPESEL(regPesel.Text)) {
                    Voter voter = await firebase.Child(Cryptography.Encode(regPesel.Text)).OnceSingleAsync<Voter>();
                    //check if account with PESEL exists
                    if (voter == null)
                    {
                        await firebase
                            .Child(encryptedPESEL)
                            .PutAsync(new Voter(Cryptography.Encode(regName.Text), Cryptography.Encode(regSurname.Text), encryptedPESEL, encryptedpassword));

                        MessageBox.Show("New user registered. You can now sign into Voting Calculator.");
                    }
                    else
                        MessageBox.Show("User with this PESEL number already exists. Please check your PESEL number or go to login page");        
                }
                else
                    MessageBox.Show("One of fields is missing, please provide all required informations.");
            } else        
                MessageBox.Show("Invalid PESEL number.");          
        }

        //LOGIN BUTTON CLICK
        private async void LogButton_Click(object sender, RoutedEventArgs e) {
            var firebase = new FirebaseClient("https://votingcalculator.firebaseio.com");
            if (loginPesel.Text != string.Empty && loginPassword.Password != string.Empty) {
                //connect to specific database key to check password
                string key = Cryptography.Encode(loginPesel.Text);
                Voter voter = await firebase
                    .Child("voters")
                    .Child(key)
                    .OnceSingleAsync<Voter>();
                string decrpassword = null;
                try {
                    decrpassword = Cryptography.Decode(voter.Password);
                } catch (NullReferenceException) {
                    MessageBox.Show("Invalid password or PESEL number.");
                    return;
                }
                //if PESEL is valid check password
                if (decrpassword.Equals(loginPassword.Password)) {
                    //save encrypted PESEL as key in app's properties
                    Properties.Settings.Default.UserPESEL = key;
                    Properties.Settings.Default.Save();

                    //go to main window
                    var mainWindow = new MainWindow();
                    mainWindow.Show();
                    this.Hide();
                } else
                    MessageBox.Show("Invalid PESEL number or password. Please try again.");                               
            } else
            {
                MessageBox.Show("One of the fields are empty");
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
