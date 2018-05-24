using System;
using System.Collections.Generic;
using System.Net;
using Newtonsoft.Json.Linq;
using System.Linq;
using System.Windows;
using Firebase.Database;
using Firebase.Database.Query;
using Kalkulator_Wyborczy.Data;

namespace Kalkulator_Wyborczy
{
    /// <summary>
    /// Logika interakcji dla klasy MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            SetupMainPage();
           
        }
        private async void SetupMainPage()
        {
            var firebase = new FirebaseClient("https://votingcalculator.firebaseio.com");  
            //user name on main page
            string PESEL = Properties.Settings.Default.UserPESEL;
            Voter v = await firebase.Child("voters").Child(PESEL).OnceSingleAsync<Voter>();   
                      
            voterBadge.Text = Cryptography.Decode(v.Name) + " " + Cryptography.Decode(v.Surname);

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
                {
                    //if user sent invalid vote app also says him cause of voting lock
                    votingInfo.Text = "You already cast an invalid vote.";
                }
                //if user has voted he can see voting statistics
                stats.Visibility = Visibility.Visible;
            }
            else
                voteButton.IsEnabled = true;
        }


        private void voteButton_Click(object sender, RoutedEventArgs e)
        {
            //if user is not on blacklist and is up to 18 he can enter voting card
            if(checkIfAllowed() && checkIfMature())
            {
                var vc = new VotingCard();
                vc.Show();
                this.Hide();
            }           
        }

        private bool checkIfAllowed()
        {

            //gets blacklist JSON and compares user's PESEL with PESEL numbers from blacklist
            string sStream;
            var sPath = "http://webtask.future-processing.com:8069/blocked";
            using (var WebClient = new WebClient())
            {
                sStream = WebClient.DownloadString(sPath);
            }
            JObject disallowed = JObject.Parse(sStream);
            IList<JToken> results = disallowed["disallowed"]["person"].Children().ToList();

            string pesel = Cryptography.Decode(input: Properties.Settings.Default.UserPESEL);
            foreach (JToken p in results)
            {
                string s = p.ToObject<Person>().PESEL;
                //if any PESEL matches user is not allowed, app sends notification and returns false
                if (pesel.Equals(s))
                {
                    addBlockedPESELtoStats();
                    MessageBox.Show("You don't have voting rights.");
                    return false;
                }
            }
            return true;
        }

        private async void addBlockedPESELtoStats()
        {
            var firebase = new FirebaseClient("https://votingcalculator.firebaseio.com");
            Votes getVotes = await firebase.Child("votes").OnceSingleAsync<Votes>();
            await firebase.Child("votes").PutAsync(new Votes(getVotes.Valid, getVotes.Invalid, getVotes.Blocked + 1));
        }


        private bool checkIfMature()
        {
            int year, day;
            string pesel = Cryptography.Decode(Properties.Settings.Default.UserPESEL);
            //month
            int rightmonth = int.Parse(pesel.Substring(2,2));
            //year
            string century = "", controlmonth = pesel.Substring(2,1);
           //count century according to PESEL ISO, valid to maximum PESEL year - 2299
            switch(controlmonth)
            {
                case "8":               
                case "9":
                    century = "18";
                    rightmonth -= 80;
                    break;
                case "0":
                case "1":
                    century = "19";
                    break;
                case "2":
                case "3":
                    century = "20";
                    rightmonth -= 20;
                    break;
                case "4":
                case "5":
                    century = "21";
                    rightmonth -= 40;
                    break;
                case "6":
                case "7":
                    century = "22";
                    rightmonth -= 60;
                    break;
            }
            year = int.Parse(century + pesel.Substring(0, 2));           
            //day
            day = int.Parse(pesel.Substring(4, 2));
            //check is user got up to 18
            DateTime now = DateTime.Now;
            DateTime userage = new DateTime(year, rightmonth, day);
            DateTime uptoeighteen = now.AddYears(-18);
            if (userage <= uptoeighteen)
                return true;
            else
            {
                MessageBox.Show("You are juvenile. You can't vote.");
                return false;
            }
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
    }
}
