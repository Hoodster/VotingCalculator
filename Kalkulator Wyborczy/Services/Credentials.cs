using Firebase.Database;
using Firebase.Database.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Kalkulator_Wyborczy.Services
{
    class Credentials
    {
        Cryptography cryptography;
        Validation validation;
        Connection connection;
        FirebaseClient firebase;
        public Credentials()
        {
            connection = new Connection();
            cryptography = new Cryptography();
            validation = new Validation();

            GetFirebaseInstance();
        }
        public async Task CreateNewUserAsync(Data.UserCredentials credentials)
        {
            //password and PESEL ecryption
            string encryptedpassword = await cryptography.Encode(credentials.Password);
            string encryptedPESEL = await cryptography.Encode(credentials.PESEL);
            //check if everything is provided
            if (!string.IsNullOrWhiteSpace(credentials.Name) && !string.IsNullOrWhiteSpace(credentials.Password) 
                && !string.IsNullOrWhiteSpace(credentials.Surname) && !string.IsNullOrWhiteSpace(credentials.PESEL))
            {
                //check PESEL
                if (await validation.checkPESEL(credentials.PESEL))
                {
                    Voter voter = await firebase.Child("voters").Child(await cryptography.Encode(credentials.PESEL)).OnceSingleAsync<Voter>();
                    //check if account with PESEL exists
                    if (voter == null)
                    {
                        await firebase
                            .Child(encryptedPESEL)
                            .PutAsync(new Voter(await cryptography.Encode(credentials.Name), await cryptography.Encode(credentials.Surname), encryptedPESEL, encryptedpassword));

                        MessageBox.Show("New user registered. You can now sign into Voting Calculator.");
                    }
                    else
                        MessageBox.Show("User with this PESEL number already exists. Please check your PESEL number or go to login page");
                }
                else
                    MessageBox.Show("One of fields is missing, please provide all required informations.");
            }
            else
                MessageBox.Show("Invalid PESEL number.");
        }
        public async Task<bool> LoginUser(Data.Login login)
        {
            if (login.PESEL != string.Empty && login.Password != string.Empty)
            {
                //connect to specific database key to check password
                string key = await cryptography.Encode(login.PESEL);
                Voter voter = await firebase
                    .Child("voters")
                    .Child(key)
                    .OnceSingleAsync<Voter>();
                string decrpassword = null;
                try
                {
                    decrpassword = await cryptography.Decode(voter.Password);
                }
                catch (NullReferenceException)
                {
                    MessageBox.Show("Invalid password or PESEL number.");
                    return await Task.FromResult(false);
                }
                //if PESEL is valid check password
                if (decrpassword.Equals(login.Password))
                {
                    //save encrypted PESEL as key in app's properties
                    Properties.Settings.Default.UserPESEL = key;
                    Properties.Settings.Default.Save();

                    return await Task.FromResult(true);
                    
                }
                else
                {
                    MessageBox.Show("Invalid PESEL number or password. Please try again.");
                    return await Task.FromResult(false);
                }
            }
            else
            {
                MessageBox.Show("One of the fields are empty");
                return await Task.FromResult(false);
            }
        }

        private async void GetFirebaseInstance()
        {
            firebase = await connection.GetFirebaseClient();
        }
    }
}
