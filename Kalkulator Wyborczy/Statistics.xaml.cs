using System;
using System.Collections.Generic;
using Firebase.Database;
using System.Linq;
using System.Data;
using Kalkulator_Wyborczy.Data;
using System.IO;
using iTextSharp.text;
using iTextSharp.text.pdf;
using System.Windows;
using System.Web.Script.Serialization;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Controls.DataVisualization.Charting;
using System.Threading;
using System.Threading.Tasks;

namespace Kalkulator_Wyborczy
{
    public partial class Statistics : Window
    {
        //collections
        Votes GetVotesObject = null;
        List<Candidate> candidatesList = new List<Candidate>();
        List<Candidate> partyList = new List<Candidate>();
        
        
        //path to desktop location
        string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);

        //options strings
        private string[] LoadDataOptions =
        {
            "Parties",
            "Candidates",
            "Votes"
        };
        
        private string[] LoadVisualisationOptions =
        {
            "Data grid",
            "Chart"
        };
        public Statistics() {
            InitializeComponent();
            //combobox text sources
            dataType.ItemsSource = LoadDataOptions;
            visualisationType.ItemsSource = LoadVisualisationOptions;            
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e) {
            var MainWindow = new MainWindow();
            MainWindow.Show();
            this.Hide();
        }

        
        private async void getVotes(bool datagridcontext, bool chart) {
            var firebase = new FirebaseClient("https://votingcalculator.firebaseio.com");
            //chart is child of datagridcontext, chart needs datagridcontext as true to be useful
            if (chart)
                datagridcontext = true;
            //clear datagrid table
            try
            {
                countData.Columns.Clear();
                countData.Items.Refresh();
            }
            catch { }
            //get votes values
            GetVotesObject = await firebase.Child("votes").OnceSingleAsync<Votes>();
            
            //change visible info when method is called for chang
            if (datagridcontext)
            {
                this.Dispatcher.Invoke(() =>
                {
                    //helper to show data in datagrid correctly
                    List<Votes> votesList = new List<Votes>();
                    votesList.Add(GetVotesObject);

                    if (chart)
                    {
                        //create key value list and set as items source of chart
                        var ListKeyValuePair = new List<KeyValuePair<string, int>>();                    
                            ListKeyValuePair.Add(new KeyValuePair<string, int>("Valid", GetVotesObject.Valid));
                            ListKeyValuePair.Add(new KeyValuePair<string, int>("Invalid", GetVotesObject.Invalid));
                            ListKeyValuePair.Add(new KeyValuePair<string, int>("Blocked", GetVotesObject.Blocked));

                            ((ColumnSeries)Chart.Series[0]).ItemsSource = ListKeyValuePair;
                    }
                    else 
                        countData.ItemsSource = votesList;
                });
            }
            
        }
 

        private async void getCandidates(bool groupbyparty, bool datagridcontext, bool chart)
        {
            var firebase = new FirebaseClient("https://votingcalculator.firebaseio.com");
            //clear collections not to duplicate
            candidatesList.Clear();
            partyList.Clear();

            //clear datagrids, throws exception, app won't do anything then and continue processing method
            try
            {
                countData.ItemsSource = null;
                partyVotes.Columns.Clear();
                partyVotes.Items.Refresh();
            } catch { }
            
            var candidates = await firebase.Child("candidates").OnceAsync<Candidate>();
            //get candidates data from app's database
            foreach (var candidate in candidates)
            {
                Candidate newcandidate = new Candidate
                {
                    name = candidate.Object.name,
                    party = candidate.Object.party,
                    votes = candidate.Object.votes
                };
                candidatesList.Add(newcandidate);
            }
                //group parties and sum votes
              partyList = candidatesList.GroupBy(c => c.party).Select(cl => new Candidate
                {
                    party = cl.First().party,
                    votes = cl.Sum(c => c.votes),
                }).ToList();
           
            //if we want to show data in datagrid
            if (datagridcontext)
            {

                this.Dispatcher.Invoke(() =>
                {
                    if (!groupbyparty) {
                        if (chart) {
                            var ListKeyValuePair = new List<KeyValuePair<string, int>>();
                            foreach (Candidate votes in candidatesList) {
                                ListKeyValuePair.Add(new KeyValuePair<string, int>(votes.name, votes.votes));

                            }
                            ((ColumnSeries)Chart.Series[0]).ItemsSource = ListKeyValuePair;
                        } else
                            countData.ItemsSource = candidatesList;
                    }
                    else
                    {
                        //group candidates by party
                        ListCollectionView collection = new ListCollectionView(candidatesList);
                        collection.GroupDescriptions.Add(new PropertyGroupDescription("party"));

                        if (chart)
                        {
                            var ListKeyValuePair = new List<KeyValuePair<string, int>>();
                            foreach (Candidate votes in partyList)
                            {
                                ListKeyValuePair.Add(new KeyValuePair<string, int>(votes.party, votes.votes));

                            }
                            ((ColumnSeries)Chart.Series[0]).ItemsSource = ListKeyValuePair;
                        }
                        else
                        {
                            partyVotes.DataContext = partyList;
                            partyVotes.Columns.RemoveAt(0);
                            countData.ItemsSource = collection;
                        }

                    }
                });
            }

        }

        
        
        private async void csvExport_Click(object sender, RoutedEventArgs e)
        {

            /*
            Task gVotes = Task.Run(() => getVotes(false, false));
            gVotes.Wait();

            Task gCandidates= Task.Run(() => getCandidates(false, false,false));          
            gCandidates.Wait();
            */
            getVotes(false, false);
            candidatesList.Clear();
            var firebase = new FirebaseClient("https://votingcalculator.firebaseio.com");
            var candidates = await firebase.Child("candidates").OnceAsync<Candidate>();
            foreach (var candidate in candidates)
            {
                Candidate newcandidate = new Candidate
                {
                    name = candidate.Object.name,
                    party = candidate.Object.party,
                    votes = candidate.Object.votes
                };
                candidatesList.Add(newcandidate);
            }

            partyList = candidatesList.GroupBy(c => c.party).Select(cl => new Candidate
            {
                party = cl.First().party,
                votes = cl.Sum(c => c.votes),
            }).ToList(); 
            
            //candidates database
            using (var streamWriter = File.CreateText(desktopPath + "//vcCsvCandidates.csv"))
            {
                var csv = new CsvHelper.CsvWriter(streamWriter, true);
                csv.WriteRecords(candidatesList);
                csv.Flush();
            }

            //votes database
            using (var streamWriter = File.CreateText(desktopPath + "//vcCsvVotes.csv"))
            {
                List<Votes> votesList = new List<Votes>();
                votesList.Add(GetVotesObject);
                var csv = new CsvHelper.CsvWriter(streamWriter, true);            
                csv.WriteRecords(votesList);
                csv.Flush();
            }

            //parties database
            using (var streamWriter = File.CreateText(desktopPath + "//vcCsvParties.csv"))
            {
                var csv = new CsvHelper.CsvWriter(streamWriter, true);
                csv.WriteRecords(partyList);
                csv.Flush();
            }

            System.Windows.MessageBox.Show("You can find .csv files on your desktop as 'vcCsvCandidates.csv', 'vcCsvParties.csv' and 'vcCsvVotes.csv'.");
        }

        private async void importDatabase_Click(object sender, RoutedEventArgs e)
        {
            var firebase = new FirebaseClient("https://votingcalculator.firebaseio.com");
            getCandidates(false, false, false);
            List<Voter> userList = new List<Voter>();
            var helperVoters = await firebase.Child("voters").OnceAsync<Voter>();
            Votes helperVotes = await firebase.Child("votes").OnceSingleAsync<Votes>();

            foreach (var v in helperVoters)
            {
                Voter voter = new Voter
                {
                    Name = v.Object.Name,
                    Surname = v.Object.Surname,
                    HasVoted = v.Object.HasVoted,
                    ValidVote = v.Object.ValidVote,
                    Password = v.Object.Password,
                    VotedCandidate = v.Object.VotedCandidate,
                    PESEL = v.Object.PESEL
                };
                userList.Add(voter);
            }

            DatabaseHelper dbHelper = new DatabaseHelper
            {
                Candidates = candidatesList,
                Voters = userList,
                Votes = helperVotes
            };

           string database = new JavaScriptSerializer().Serialize(dbHelper);
           File.WriteAllText(desktopPath + "\\vcDatabase.json", database.ToString());

 
            System.Windows.MessageBox.Show("You can find JSON database file on your desktop as 'vcDatabase.json'.");
        }

        private void dataCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string dataresult = dataType.SelectedValue.ToString();
            string visresult = visualisationType.SelectedValue.ToString(); 
            
            //visualisation and type of data combinations
            if (dataresult.Equals("Parties"))
            {
                if (visresult.Equals("Data grid"))
                {
                    Chart.Visibility = Visibility.Collapsed;
                    partyVotes.Visibility = Visibility.Visible;
                    countData.Visibility = Visibility.Visible;
                    getCandidates(true, true,false);
                }
                else
                {
                    Chart.Visibility = Visibility.Visible;
                    partyVotes.Visibility = Visibility.Collapsed;
                    countData.Visibility = Visibility.Collapsed;
                    getCandidates(true, true,true);
                }
            }
            else if (dataresult.Equals("Candidates"))
            {
                if (visresult.Equals("Data grid"))
                {
                    Chart.Visibility = Visibility.Collapsed;
                    partyVotes.Visibility = Visibility.Collapsed;
                    countData.Visibility = Visibility.Visible;
                    getCandidates(false, true,false);
                }
                else
                {
                    Chart.Visibility = Visibility.Visible;
                    partyVotes.Visibility = Visibility.Collapsed;
                    countData.Visibility = Visibility.Collapsed;
                    getCandidates(false, true,true);
                }
            }
            else if (dataresult.Equals("Votes"))
            {
                if (visresult.Equals("Data grid"))
                {
                    Chart.Visibility = Visibility.Collapsed;
                    partyVotes.Visibility = Visibility.Collapsed;
                    countData.Visibility = Visibility.Visible;
                    getVotes(true,false);
                }
                else
                {
                    Chart.Visibility = Visibility.Visible;
                    partyVotes.Visibility = Visibility.Collapsed;
                    countData.Visibility = Visibility.Collapsed;
                    getVotes(true,true);
                }

            }
        }

        private async void pdfExport_Click(object sender, RoutedEventArgs e)
        {
            candidatesList.Clear();
            var firebase = new FirebaseClient("https://votingcalculator.firebaseio.com");
            var candidates = await firebase.Child("candidates").OnceAsync<Candidate>();         
            foreach (var candidate in candidates)
            {
                Candidate newcandidate = new Candidate
                {
                    name = candidate.Object.name,
                    party = candidate.Object.party,
                    votes = candidate.Object.votes
                };
                candidatesList.Add(newcandidate);
            }

            partyList = candidatesList.GroupBy(c => c.party).Select(cl => new Candidate
                {
                    party = cl.First().party,
                    votes = cl.Sum(c => c.votes),
                }).ToList();

            Votes getVotes = await new FirebaseClient("https://votingcalculator.firebaseio.com").Child("votes").OnceSingleAsync<Votes>();    

                Document doc = new Document(PageSize.LETTER);
            try
            {
                PdfWriter pdfWriter = PdfWriter.GetInstance(doc, new FileStream(desktopPath + "\\vcData.pdf", FileMode.Create));
            } catch(IOException)
            {
                System.Windows.MessageBox.Show("You have already opened document. Please terminate all reader programs and try again.");
            }
            doc.Open();
           
            //create tables and put them into PDF document
            PdfPTable table1 = new PdfPTable(3);
            table1.AddCell("candidate");
            table1.AddCell("party");
            table1.AddCell("votes");
            foreach (Candidate candidate in candidatesList)
            {
                table1.AddCell(candidate.name);
                table1.AddCell(candidate.party);
                table1.AddCell(candidate.votes.ToString());
            }

            PdfPTable table2 = new PdfPTable(2);
            table2.AddCell("party");
            table2.AddCell("votes");
            foreach (Candidate candidate in partyList)
            {
                table2.AddCell(candidate.party);
                table2.AddCell(candidate.votes.ToString());
            }

            PdfPTable table3 = new PdfPTable(2);
            table3.AddCell("vote type");
            table3.AddCell("count");
                table3.AddCell("Valid");
                table3.AddCell(GetVotesObject.Valid.ToString());

                table3.AddCell("Invalid");
                table3.AddCell(GetVotesObject.Invalid.ToString());

                table3.AddCell("Blocked");
                table3.AddCell(GetVotesObject.Blocked.ToString());
            
            doc.Add(table1);
            doc.NewPage();          
            doc.Add(table2);
            doc.NewPage();
            doc.Add(table3);
            doc.Close();
            System.Windows.MessageBox.Show("You can find PDF data file on your desktop.");
        }
    }
}
