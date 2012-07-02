using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace com.zanthra.emw.Model

{
    [Serializable]
    class Item : INotifyPropertyChanged
    {
        public long NumberBought { get; set; }
        public long NumberSold { get; set; }
        public double TotalIskSpent { get; set; }
        public double TotalIskRecieved { get; set; }
        public string ItemName { get; set; }
        public long TypeId { get; set; }

        private double _transactionTax;
        public double TransactionTax { 
            get
            {
                return _transactionTax;
            }
            set
            {
                //Transaction Tax affects many propeties.
                _transactionTax = value;
                NotifyPropertyChanged("AverageSale");
                NotifyPropertyChanged("MatchedProfit");
                NotifyPropertyChanged("AverageProfit");
                NotifyPropertyChanged("TradeValue");
            }
        }
        private double _brokerFee;
        public double BrokerFee
        {
            get
            {
                return _brokerFee;
            }
            set
            {
                //Broker fee affects many properties.
                _brokerFee = value;
                NotifyPropertyChanged("AverageSale");
                NotifyPropertyChanged("AverageBuy");
                NotifyPropertyChanged("MatchedProfit");
                NotifyPropertyChanged("AverageProfit");
                NotifyPropertyChanged("TradeValue");
            }
        }

        public string imageUrl
        {
            get { return String.Format("http://image.eveonline.com/Type/{0}_32.png", TypeId); }
        }

        public double ProfitMargin
        {
            get { return (AverageSale / AverageBuy - 1) * 100; }
        }

        public double AverageSale
        {
            get{return TotalIskRecieved / NumberSold * (1 - TransactionTax - BrokerFee);}
        }

        public double AverageBuy
        {
            get{return TotalIskSpent / NumberBought * (1 + BrokerFee);}
        }

        public long MatchedQuantity
        {
            get { return Math.Min(NumberBought, NumberSold); }
        }

        public double MatchedProfit
        {
            get { return MatchedQuantity * AverageSale - MatchedQuantity * AverageBuy; }
        }

        public double AverageProfit
        {
            get { return MatchedProfit / MatchedQuantity; }
        }

        public double TradeValue
        {
            get { return ProfitMargin * MatchedProfit; }
        }

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
