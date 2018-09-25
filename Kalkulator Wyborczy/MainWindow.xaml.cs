using System;
using System.Collections.Generic;
using System.Net;
using Newtonsoft.Json.Linq;
using System.Linq;
using System.Windows;
using Firebase.Database;
using Firebase.Database.Query;
using Kalkulator_Wyborczy.Data;
using Kalkulator_Wyborczy.Services;
using System.Threading.Tasks;

namespace Kalkulator_Wyborczy
{
    /// <summary>
    /// Logika interakcji dla klasy MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        Cryptography cryptography;
        Validation validation;
        Connection connection;
        FirebaseClient firebase;
        public MainWindow()
        {
            cryptography = new Cryptography();
            validation = new Validation();
            connection = new Connection();

            InitializeComponent();
            SetupMainPage();
        }

        private async void SetupMainPage()
        {
            await GetFirebaseInstanceAsync();
            //user name on main page
            string PESEL = Properties.Settings.Default.UserPESEL;
            Voter v = await firebase.Child("voters").Child(PESEL).OnceSingleAsync<Voter>();   
                      
            voterBadge.Text = await cryptography.Decode(v.Name) + " " + await cryptography.Decode(v.Surname);

            if (v.HasVoted)
            {
                //make voting button inactive
                voteButton.Visibility = Visibility.Collapsed;
                inactiveVoteButton.Visibility = Visibility.Visible;
                if (v.ValidVote)
                {
                    //set information for who user has voted
                    string elector = v.VotedCandidate;
                    Candidate candidate = await firebase
                           .Child("candidates")
                           .Child(elector)
                           .OnceSingleAsync<Candidate>();
                    votingInfo.Text = "You already voted for " + candidate.name + " from " + candidate.party + " party.";
                }
                else
                    votingInfo.Text = "You already cast an invalid vote.";

                stats.Visibility = Visibility.Visible;
            }
            else
                voteButton.IsEnabled = true;
        }

        private async void voteButton_Click(object sender, RoutedEventArgs e)
        {
            string pesel = await cryptography.Decode(Properties.Settings.Default.UserPESEL);

            if (await validation.CheckIfAllowedAsync(pesel))
            {
                if (await validation.CheckIfMatureAsync(pesel))
                {
                    var vc = new VotingCard();
                    vc.Show();
                    this.Hide();
                }
            } else
                AddBlockedPESELtoStats();
        }

        /// <summary>
        /// Save in database vote attempt by citizen who lost voting rights.
        /// </summary>
        private async void AddBlockedPESELtoStats()
        {
            Votes getVotes = await firebase.Child("votes").OnceSingleAsync<Votes>();
            await firebase.Child("votes").PutAsync(new Votes(getVotes.Valid, getVotes.Invalid, getVotes.Blocked + 1));
        }
        
        private void signOut_Click(object sender, RoutedEventArgs e)
        {
            //clear user properties value
            Properties.Settings.Default.UserPESEL = "";
            var login = new Login();
            this.Hide();
            login.Show();
           
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void stats_Click(object sender, RoutedEventArgs e)
        {
            var statistics = new Statistics();
            statistics.Show();
            this.Hide();
        }

        private async Task GetFirebaseInstanceAsync()
        {
            firebase = await connection.GetFirebaseClient();
        }
    }
}
