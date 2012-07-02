using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;
using com.zanthra.emw.Model;
using System.Net;
using System.IO;
using System.Xml.Linq;

using System.IO.IsolatedStorage;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.ComponentModel;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

namespace com.zanthra.emw.ViewModels
{
    class EveViewModel : INotifyPropertyChanged
    {
        
        public EveViewModel()
        {
            loadCharacters.DoWork += _loadCharactersWorker;
            loadCharacters.RunWorkerCompleted += _loadCharactersCompleted;

            loadTransactions.DoWork += _loadTransactionsWorker;
            loadTransactions.RunWorkerCompleted += _loadTransactionsCompleted;
        }

        public static string CHARACTER_LIST_URL = "https://api.eveonline.com/account/Characters.xml.aspx";
        public static string TRANSACTION_LIST_URL = "https://api.eveonline.com/char/WalletTransactions.xml.aspx";

        #region Properties

        /// <summary>
        /// Observable collection for the characters loaded for the current API Key.
        /// </summary>
        ObservableCollection<Character> _characters = new ObservableCollection<Character>();
        public ObservableCollection<Character> Characters
        {
            get { return _characters; }
        }

        /// <summary>
        /// The current collection of Transactions.
        /// </summary>
        ObservableCollection<Transaction> _transactions = new ObservableCollection<Transaction>();
        public ObservableCollection<Transaction> Transactions
        {
            get { return _transactions; }
        }

        /// <summary>
        /// The current collection of items.
        /// </summary>
        ObservableCollection<Item> _items = new ObservableCollection<Item>();
        public ObservableCollection<Item> Items
        {
            get { return _items; }
        }

        /// <summary>
        /// The API UserID in the text box.
        /// </summary>
        private string _userID;
        public string UserID
        {
            get { return _userID; }
            set
            {
                _userID = value;
                NotifyPropertyChanged("UserID");
            }
        }

        /// <summary>
        /// The API Key in the text box.
        /// </summary>
        private string _apiKey;
        public string ApiKey
        {
            get { return _apiKey; }
            set
            {
                _apiKey = value;
                NotifyPropertyChanged("ApiKey");
            }
        }

        /// <summary>
        /// The status bar status string.
        /// </summary>
        private string _statusString;
        public string StatusString
        {
            get
            {
                return _statusString;
            }
            set
            {
                _statusString = value;
                NotifyPropertyChanged("StatusString");
            }
        }

        /// <summary>
        /// The brokers fee, <i>not</i> in percent.  0.0075 = %0.75.
        /// </summary>
        private double _brokerFee;
        public double BrokerFee
        {
            get { return _brokerFee; }
            set { _brokerFee = value; NotifyPropertyChanged("BrokerFee"); }
        }

        /// <summary>
        /// The Transaction tax, <i>not</i> in percent. 0.005 = %0.5
        /// </summary>
        private double _transactionTax;
        public double TransactionTax
        {
            get { return _transactionTax; }
            set { _transactionTax = value; NotifyPropertyChanged("TransactionTax"); }
        }

        #endregion

        #region Load Characters
        /// <summary>
        /// Background worker for loading the character based on the current UserID and API Key.
        /// </summary>
        private BackgroundWorker loadCharacters = new BackgroundWorker();

        /// <summary>
        /// Starts loading characters.
        /// </summary>
        public void LoadCharacters()
        {
            UpdateStatus("Retrieving Characters");
            if (!loadCharacters.IsBusy)
                loadCharacters.RunWorkerAsync();
        }

        /// <summary>
        /// Worker method for loading characters.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void _loadCharactersWorker(object sender, DoWorkEventArgs e)
        {
            //Create a new temporary list.
            List<Character> newCharacters = new List<Character>();

            UpdateStatus("Loading Characters from API");
            //Get our XML root element from the EVE API.
            XElement root = XElement.Load(CHARACTER_LIST_URL + String.Format("?keyID={0}&vCode={1}", _userID, _apiKey));

            UpdateStatus("Processing Character List");
            //Throw an exception if the EVE API sends an error.
            if (root.Element("error") != null) throw new EveApiAccessException(root.Element("error"));

            //Otherwise, just create new characters from each result.
            foreach (XElement el in root.Element("result").Element("rowset").Elements())
            {
                Character toAdd = new Character
                {
                    characterID = Int64.Parse(el.Attribute("characterID").Value),
                    name = el.Attribute("name").Value,
                    corporationID = Int64.Parse(el.Attribute("corporationID").Value),
                    corporationName = el.Attribute("corporationName").Value
                };

                newCharacters.Add(toAdd);
            }

            e.Result = newCharacters;
        }

        /// <summary>
        /// Completion method for loading characters.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void _loadCharactersCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            //If there is an error, display in the status.
            if (e.Error is EveApiAccessException)
            {
                UpdateStatus(String.Format("EVE API Error while retrieving Characters: {0}", ((EveApiAccessException)e.Error.GetBaseException()).ErrorCode));
            }
            //Otherwise clear the character list and update it.
            else
            {
                _characters.Clear();
                foreach (Character c in (List<Character>)e.Result)
                {
                    _characters.Add(c);
                }
                UpdateStatus("Done");
            }
        }

        #endregion

        #region Load Transactions
        /// <summary>
        /// Background worker for loading transactions.
        /// </summary>
        private BackgroundWorker loadTransactions = new BackgroundWorker();

        /// <summary>
        /// Method to start loading transactions.
        /// </summary>
        /// <param name="character">The character to have transactions loaded for.</param>
        public void LoadTranscations(Character character)
        {
            if (character == null)
            {
                throw new ArgumentNullException("You must select a character.");
            }
            else
                loadTransactions.RunWorkerAsync(character);
        }

        /// <summary>
        /// Worker method for loading transactions.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="eargs"></param>
        private void _loadTransactionsWorker(object sender, DoWorkEventArgs eargs)
        {
            //Cast the character back.
            Character character = (Character)eargs.Argument;

            //Create a temporary transactions list.
            List<Transaction> newTransactions = new List<Transaction>();
            bool done = false;
            long beforeTransID = -1;
            while (!done)
            {
                XElement root;

                //If this is the first set of 1000, we don't but a beforeTransID in.
                if (beforeTransID == -1)
                {
                    UpdateStatus("Retrieving First Thousand Transactions");
                    root = XElement.Load(TRANSACTION_LIST_URL + String.Format("?keyID={0}&vCode={1}&characterID={2}",
                                                                              _userID, _apiKey, character.characterID));
                }
                //Otherwise retrieve the transactions beforeTransID.
                else
                {
                    UpdateStatus(String.Format("Retrieving Transactions Before transID {0}", beforeTransID));
                    root = XElement.Load(TRANSACTION_LIST_URL + String.Format("?keyID={0}&vCode={1}&characterID={2}&beforeTransID={3}", _userID, _apiKey, character.characterID, beforeTransID));
                }

                //TODO: If the number of transactions lands on 1000, it will likely trigger this error and not load the transactions.
                //I am unsure how the API handles beforeTransID if there are no transactions beforeTransID, and I don't have 1000 transactions to test it with.
                if (root.Element("error") != null)
                {
                    done = true;
                    throw new EveApiAccessException(root.Element("error"));
                }

                //If we have less than 1000 transactions we are done, otherwise linq query to find the minimum transID from this collection.
                if (root.Element("result").Element("rowset").Elements().Count() < 1000)
                    done = true;
                else
                    beforeTransID = (from e in root.Element("result").Element("rowset").Elements() select Int64.Parse(e.Attribute("transactionID").Value)).Min();

                //Add the transactions to the master transaction list.
                foreach (Transaction t in _parseFileForTransactions(root))
                {
                    newTransactions.Add(t);
                }
            }

            //Finish Up.
            eargs.Result = newTransactions;
            UpdateStatus("Done Retrieving Transactions");
        }

        /// <summary>
        /// Convert an XElement into a list of transactions.
        /// </summary>
        /// <param name="root"></param>
        /// <returns></returns>
        public static List<Transaction> _parseFileForTransactions(XElement root)
        {
            List<Transaction> newTransactions = new List<Transaction>();

            foreach (XElement el in root.Element("result").Element("rowset").Elements())
            {
                Transaction toAdd = new Transaction
                {
                    transIncluded = true,
                    time = DateTime.Parse(el.Attribute("transactionDateTime").Value),
                    transID = Int64.Parse(el.Attribute("transactionID").Value),
                    itemName = el.Attribute("typeName").Value,
                    quantity = Int64.Parse(el.Attribute("quantity").Value),
                    price = Double.Parse(el.Attribute("price").Value),
                    buy = el.Attribute("transactionType").Value.Equals("buy"),
                    typeID = Int64.Parse(el.Attribute("typeID").Value)
                };

                newTransactions.Add(toAdd);
            }

            return newTransactions;
        }

        /// <summary>
        /// Complete loading transactions.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _loadTransactionsCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            //If there is an error, pass it on.
            if (e.Error is EveApiAccessException)
            {
                UpdateStatus(String.Format("EVE API Error while retrieving Transactions: {0}", ((EveApiAccessException)e.Error.GetBaseException()).ErrorCode));
            }
            //Otherwise put any new transactions into the master list.
            else
            {
                foreach (Transaction t in (List<Transaction>)e.Result)
                {
                    if (!_transactions.Contains(t))
                    {
                        t.PropertyChanged +=new PropertyChangedEventHandler(Transaction_PropertyChanged);
                        _transactions.Add(t);
                    }
                }
                UpdateStatus("Calculating Items");
                //Group into items.
                calculateItems();
            }
        }

        #endregion

        #region Calculate Items

        /// <summary>
        /// Calculates the items collection from the transactions collection.
        /// </summary>
        private void calculateItems()
        {
            UpdateStatus("Calculating Items");

            //Clear the current collection.
            _items.Clear();

            //Use LINQ to group the transaction into typeIDs and create item entries in it.
            foreach (Item i in (from trans in Transactions where trans.transIncluded
                                group trans by trans.typeID into item
                                select
                                    new Item
                                    {
                                        ItemName = item.OrderByDescending(t=>t.time).First().itemName,
                                        TypeId = item.Key,
                                        NumberBought = item.Sum(trans => trans.buy ? trans.quantity : 0),
                                        NumberSold = item.Sum(trans => trans.buy ? 0 : trans.quantity),
                                        TotalIskSpent = item.Sum(trans => trans.buy ? trans.quantity * trans.price : 0),
                                        TotalIskRecieved = item.Sum(trans => trans.buy ? 0 : trans.quantity * trans.price),
                                        TransactionTax = this.TransactionTax,
                                        BrokerFee = this.BrokerFee
                                    }))
            {
                _items.Add(i);
            }

            //Update prices based on Brokers fee and Transaction Tax.
            //UpdatePrices();
            NotifyPropertyChanged("Items");
            UpdateStatus("Done");
        }
        #endregion

        /// <summary>
        /// Using HTTPs without acutally checking the certificate does not protect against man
        /// in the middle, but it does protect against thrid party snooping.
        /// </summary>
        private void allowHTTPS()
        {
            ServicePointManager.ServerCertificateValidationCallback =
                new RemoteCertificateValidationCallback(
                delegate(
                    object sender2,
                    X509Certificate certificate,
                    X509Chain chain,
                    SslPolicyErrors sslPolicyErrors)
                {
                    return true;
                });
        }

        #region Import Export

        /// <summary>
        /// Create comma separated list from items.
        /// </summary>
        /// <param name="fileName"></param>
        public void ExportItems(String fileName)
        {
            using (StreamWriter fileOut = new StreamWriter(new FileStream(fileName, FileMode.OpenOrCreate)))
            {
                fileOut.WriteLine("Item Name,Number Bought,Number Sold,Total ISK Spent,Total ISK Recieved,TypeID");
                foreach (Item i in _items)
                {
                    fileOut.WriteLine("{0},{1},{2},{3},{4},{5}", i.ItemName, i.NumberBought, i.NumberSold, i.TotalIskSpent, i.TotalIskRecieved, i.TypeId);
                }
            }
        }

        /// <summary>
        /// Create comma separated list from Transactions.
        /// </summary>
        /// <param name="fileName"></param>
        public void ExportTransactions(String fileName)
        {
            using (StreamWriter fileOut = new StreamWriter(new FileStream(fileName, FileMode.OpenOrCreate)))
            {
                fileOut.WriteLine("Item Name,Buy Order,Price,Quantity,Time,Transaction ID,TypeID");
                foreach (Transaction t in _transactions)
                {
                    fileOut.WriteLine("{0},{1},{2},{3},{4},{5},{6},{7}", t.itemName, t.buy, t.price, t.quantity, t.time, t.transID, t.typeID, t.transactionIncludedString);
                }
            }
        }

        /// <summary>
        /// Import comma separated list of Transactions.
        /// </summary>
        /// <param name="fileName"></param>
        public void Import(String fileName)
        {
            using (StreamReader fileIn = new StreamReader(new FileStream(fileName, FileMode.Open)))
            {
                String line = fileIn.ReadLine();
                while (!fileIn.EndOfStream)
                {
                    line = fileIn.ReadLine();
                    String[] split = line.Split(new Char[]{','});
                    try
                    {
                        Transaction toAdd = new Transaction
                        {
                            time = DateTime.Parse(split[4]),
                            transID = Int64.Parse(split[5]),
                            itemName = split[0],
                            typeID = Int64.Parse(split[6]),
                            quantity = Int64.Parse(split[3]),
                            buy = Boolean.Parse(split[1]),
                            price = Double.Parse(split[2]),
                            transIncluded = true
                        };

                        if (split.Length > 7)
                        {
                            toAdd.transactionIncludedString = split[7];
                        }

                        if (!_transactions.Contains(toAdd))
                        {
                            toAdd.PropertyChanged += new PropertyChangedEventHandler(Transaction_PropertyChanged);
                            _transactions.Add(toAdd);
                        }
                    }
                    catch (Exception)
                    {
                        //System.Windows.MessageBox.Show(String.Format("Error parsing string: {0}", line));
                    }
                }
            }
            calculateItems();
        }

        private void Transaction_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            calculateItems();
        }

        #endregion

        /// <summary>
        /// Update the prices of items based on Transaction Tax and Broker Fees.
        /// </summary>
        public void UpdatePrices()
        {
            /*foreach (Item i in _items)
            {
                i.BrokerFee = BrokerFee;
                i.TransactionTax = TransactionTax;
            }

            NotifyPropertyChanged("Items");*/
            calculateItems();
        }

        /// <summary>
        /// Remove all Transactions and items.
        /// </summary>
        public void ClearTransactions()
        {
            _transactions.Clear();
            _items.Clear();
        }

        /// <summary>
        /// Export all transactions and settings to isolated storage.
        /// </summary>
        public void Close()
        {
            UpdateStatus("Saving");
            IsolatedStorageFile f = IsolatedStorageFile.GetUserStoreForAssembly();
            using(IsolatedStorageFileStream stream = new IsolatedStorageFileStream("transactions", FileMode.Create))
            {
                IFormatter formatter = new BinaryFormatter();
                formatter.Serialize(stream, _transactions);
                formatter.Serialize(stream, _userID);
                formatter.Serialize(stream, _apiKey);
                formatter.Serialize(stream, _transactionTax);
                formatter.Serialize(stream, _brokerFee);
            }
        }

        /// <summary>
        /// Import transactions and settings from isolated storage.
        /// </summary>
        public void Open()
        {
            UpdateStatus("Loading");
            allowHTTPS();

            IsolatedStorageFile f = IsolatedStorageFile.GetUserStoreForAssembly();
            using (IsolatedStorageFileStream stream = new IsolatedStorageFileStream("transactions", FileMode.OpenOrCreate))
            {
                IFormatter formatter = new BinaryFormatter();
                foreach(Transaction t in (ObservableCollection<Transaction>)formatter.Deserialize(stream))
                {
                    t.PropertyChanged += new PropertyChangedEventHandler(Transaction_PropertyChanged);
                    _transactions.Add(t);
                }

                try
                {
                    UserID = (String)formatter.Deserialize(stream);
                    ApiKey = (String)formatter.Deserialize(stream);
                }
                catch (Exception)
                {
                    UserID = "ID";
                    ApiKey = "Verification Code";
                }

                try
                {
                    TransactionTax = (Double)formatter.Deserialize(stream);
                    BrokerFee = (Double)formatter.Deserialize(stream);
                }
                catch (Exception)
                {
                    BrokerFee = .01;
                    TransactionTax = .01;
                }

                calculateItems();
                UpdatePrices();
            }
        }

        /// <summary>
        /// Updates the status string.
        /// </summary>
        /// <param name="status"></param>
        private void UpdateStatus(string status)
        {
            StatusString = status;
        }

        public void transactionUpdated()
        {

        }

        private void PropogateTransactionChange()
        {

        }

        /// <summary>
        /// Property changed event.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Notify a property changed.
        /// </summary>
        /// <param name="property"></param>
        private void NotifyPropertyChanged(string property)
        {
            if(PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(property));
            }
        }
    }
}
