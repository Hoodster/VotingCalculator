using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using System.Net;
using System.Text;
using System.Windows;
using Kalkulator_Wyborczy.Data;
using Firebase.Database;
using Firebase.Database.Query;

namespace Kalkulator_Wyborczy
{
    /// <summary>
    /// Logika interakcji dla klasy VotingCard.xaml
    /// </summary>
    public partial class VotingCard : Window
    {
        List<Candidate> candidatesList = new List<Candidate>();
        
        public VotingCard()
        {
            InitializeComponent();
            getCandidates();
            MessageBox.Show("If you are not ready yet you can leave this page without voting just by closing this window.", "Information");

        }

        private void getCandidates()
        {
            string sStream;
            var sPath = "http://webtask.future-processing.com:8069/candidates";
            using (var WebClient = new WebClient())
            {
                sStream = WebClient.DownloadString(sPath);
            }
            JObject candidates = JObject.Parse(sStream);
            IList<JToken> results = candidates["candidates"]["candidate"].Children().ToList();
          
            foreach (JToken p in results)
            {          
                Candidate candidate = new Candidate();
                //get candidate
                byte[] canbytes = Encoding.Default.GetBytes(p.ToObject<Candidate>().name);
                string candidatename = Encoding.UTF8.GetString(canbytes);
                candidate.name = candidatename;
                //get party
                byte[] parbytes = Encoding.Default.GetBytes(p.ToObject<Candidate>().party);
                string partyname = Encoding.UTF8.GetString(parbytes);             
                candidate.party = partyname;
              
                candidatesList.Add(candidate);
            }

            Dispatcher.Invoke(() =>
            {
                candidatesListView.DataContext = candidatesList;
            });
        }
       

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;
            var main = new MainWindow();
            main.Show();
            this.Hide();
        }

        private async void voteButton_Click(object sender, RoutedEventArgs e)
        {
            var firebase = new FirebaseClient("https://votingcalculator.firebaseio.com");
            List<Candidate> selectedItemsIndices = new List<Candidate>();
            Votes GetVotesObject = await firebase.Child("votes").OnceSingleAsync<Votes>();
            //Getting index of each selected item and find selected 
            foreach (object item in candidatesListView.SelectedItems)
            {               
                int index = candidatesListView.Items.IndexOf(item);
                selectedItemsIndices.Add(candidatesList[index]);
            }

           //message box
           var messageBoxResult = MessageBox.Show("Are you sure?", "Warning", MessageBoxButton.YesNo);
            if (messageBoxResult == MessageBoxResult.Yes)
            {
                string PESEL = Properties.Settings.Default.UserPESEL;
                Voter getClientData = await new FirebaseClient("https://votingcalculator.firebaseio.com")
                               .Child("voters")
                               .Child(PESEL)
                               .OnceSingleAsync<Voter>();

                if (selectedItemsIndices.Count == 1)
                {

                    //if candidate is not in database yet Firebase throws null so we put new candidate with one vote

                    Candidate getCandidate = await firebase.Child("candidates")
                               .Child(selectedItemsIndices[0].name)
                               .OnceSingleAsync<Candidate>();

                    await firebase.Child("candidates")
                               .Child(getCandidate.name)
                               .PutAsync(new Candidate(getCandidate.name, getCandidate.party, getCandidate.votes + 1));


                    //update valid vote client info        
                    Voter voter = new Voter
                    {
                        Name = getClientData.Name,
                        Surname = getClientData.Surname,
                        Password = getClientData.Password,
                        PESEL = Properties.Settings.Default.UserPESEL,
                        HasVoted = true,
                        ValidVote = true,
                        VotedCandidate = selectedItemsIndices[0].name
                    };

                    await firebase.Child("voters")
                                  .Child(Properties.Settings.Default.UserPESEL)
                                  .PutAsync(voter);

                    await firebase.Child("votes").PutAsync(new Votes(GetVotesObject.Valid + 1, GetVotesObject.Invalid, GetVotesObject.Blocked));
                }else{
                    //update invalid vote client info
                    Voter voter = new Voter
                    {
                        Name = getClientData.Name,
                        Surname = getClientData.Surname,
                        Password = getClientData.Password,
                        PESEL = Properties.Settings.Default.UserPESEL,
                        HasVoted = true,
                        ValidVote = false,
                        VotedCandidate = ""
                    };

                    await firebase.Child("voters")
                              .Child(Properties.Settings.Default.UserPESEL)
                              .PutAsync(voter);

                    await firebase.Child("votes").PutAsync(new Votes(GetVotesObject.Valid, GetVotesObject.Invalid + 1, GetVotesObject.Blocked));
                }    
                this.Close();
            }        
        }
    }
}
