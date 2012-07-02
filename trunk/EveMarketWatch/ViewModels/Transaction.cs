using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

using System.Runtime.Serialization;

namespace com.zanthra.emw.Model
{
    [Serializable]
    public class Transaction : INotifyPropertyChanged
    {
        public DateTime time { get; set; }
        public bool buy { get; set; }
        public string itemName { get; set; }
        public double price { get; set; }
        public long quantity { get; set; }
        public long transID { get; set; }
        public long typeID { get; set; }

        [OptionalField]
        public bool transIncluded = true;

        [OnDeserializing]
        private void SetTransIncludedDefault(StreamingContext sc)
        {
            transIncluded = true;
        }

        public String transactionIncludedString
        {
            get
            {
                if (transIncluded) return "true";
                return "false";
            }
            set
            {
                transIncluded = "true".Equals(value, StringComparison.InvariantCultureIgnoreCase) ;
                NotifyPropertyChanged("transIncluded");
            }
        }

        public override bool Equals(object obj)
        {
            if (obj == null) return false;

            if (this.GetType() != obj.GetType()) return false;

            // safe because of the GetType check
            Transaction transaction = (Transaction)obj;

            return Object.Equals(transID, transaction.transID);
        }

        public override int GetHashCode()
        {
            return transID.GetHashCode();
        }

        public override String ToString()
        {
            return String.Format("{0}, {1}, {2}, {3}", transID, itemName, quantity, price);
        }

        [field:NonSerialized]
        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(string property)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(property));
            }
        }
    }
}
