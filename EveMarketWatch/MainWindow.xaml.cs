using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using com.zanthra.emw.ViewModels;
using com.zanthra.emw.Model;
using System.ComponentModel;
using Microsoft.Win32;

namespace com.zanthra.emw
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        EveViewModel _viewModel;
        public MainWindow()
        {
            InitializeComponent();

            _viewModel = (EveViewModel)this.DataContext;
        }

        private void btnLoadCharacters_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _viewModel.LoadCharacters();
            }
            catch (EveApiAccessException ex)
            {
                MessageBox.Show(String.Format("Error Code: {0} from API", ex.ErrorCode));
            }
        }

        private void btnLoadTransactions_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _viewModel.LoadTranscations((Character)listBox1.SelectedItem);
            }
            catch (EveApiAccessException ex)
            {
                MessageBox.Show(String.Format("Error Code: {0} from API", ex.ErrorCode));
            }
            catch (ArgumentNullException ex)
            {
                MessageBox.Show(String.Format(ex.Message));
            }
        }

        GridViewColumnHeader _lastHeaderClicked = null;
        ListSortDirection _lastDirection = ListSortDirection.Ascending;

        /// <summary>
        /// This method handles sorting of the grid view based on header column clicks.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void GridViewColumnHeaderClickedHandler(object sender,
                                                RoutedEventArgs e)
        {
            GridViewColumnHeader headerClicked =
                  e.OriginalSource as GridViewColumnHeader;
            ListSortDirection direction;

            if (headerClicked != null)
            {
                if (headerClicked.Role != GridViewColumnHeaderRole.Padding)
                {
                    if (headerClicked != _lastHeaderClicked)
                    {
                        direction = ListSortDirection.Ascending;
                    }
                    else
                    {
                        if (_lastDirection == ListSortDirection.Ascending)
                        {
                            direction = ListSortDirection.Descending;
                        }
                        else
                        {
                            direction = ListSortDirection.Ascending;
                        }
                    }

                    if (headerClicked.Column.DisplayMemberBinding is Binding)
                    {
                        string sortOnProperty = (headerClicked.Column.DisplayMemberBinding as Binding).Path.Path;
                        Sort(sortOnProperty, direction, e);

                        if (direction == ListSortDirection.Ascending)
                        {
                            headerClicked.Column.HeaderTemplate =
                            Resources["HeaderTemplateArrowUp"] as DataTemplate;
                        }
                        else
                        {
                            headerClicked.Column.HeaderTemplate =
                            Resources["HeaderTemplateArrowDown"] as DataTemplate;
                        }

                        // Remove arrow from previously sorted header
                        if (_lastHeaderClicked != null && _lastHeaderClicked != headerClicked)
                        {
                            _lastHeaderClicked.Column.HeaderTemplate = null;
                        }


                        _lastHeaderClicked = headerClicked;
                        _lastDirection = direction;
                    }
                }
            }
        }

        private void Sort(string sortBy, ListSortDirection direction, RoutedEventArgs e)
        {
            
            ICollectionView dataView =
            CollectionViewSource.GetDefaultView((e.Source as ListView).ItemsSource);

            dataView.SortDescriptions.Clear();
            SortDescription sd = new SortDescription(sortBy, direction);
            dataView.SortDescriptions.Add(sd);
            dataView.Refresh();
        }

        private void listBox1_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            try
            {
                _viewModel.Close();
            }
            catch(System.Runtime.Serialization.SerializationException ex)
            {
                MessageBox.Show("something went wrong saving data.\n" + ex.Message);
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                _viewModel.Open();
            }
            catch(System.Runtime.Serialization.SerializationException ex)
            {
                MessageBox.Show("Something went wrong reading stored data.\n" + ex.Message);
            }
        }

        private void btnExport_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog sfd = new SaveFileDialog();
            sfd.DefaultExt = "csv";
            sfd.AddExtension = true;
            sfd.Filter = "EVE MarketWatch CSV Files (*.csv)|*.csv";

            if (sfd.ShowDialog() == true)
            {
                try
                {
                    _viewModel.ExportTransactions(sfd.FileName);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(String.Format("Error Writing to File: {0}", ex.Message));
                }
            }
        }

        private void btnImport_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "EVE MarketWatch Transaction Files (*.csv;*.emw)|*.csv;*.emw";

            if (ofd.ShowDialog() == true)
            {
                try
                {
                    if (ofd.FileName.EndsWith(".emw"))
                    {
                        _viewModel.LoadTransactions(new System.IO.FileStream(ofd.FileName, System.IO.FileMode.Open));
                    }
                    else
                    {
                        _viewModel.Import(ofd.FileName);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(String.Format("Error Reading from File: {0}", ex.ToString()));
                }
            }
        }

        private void btnExportItems_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog sfd = new SaveFileDialog();
            sfd.DefaultExt = "csv";
            sfd.AddExtension = true;
            sfd.Filter = "EVE MarketWatch CSV Files (*.csv)|*.csv";

            if (sfd.ShowDialog() == true)
            {
                try
                {
                    _viewModel.ExportItems(sfd.FileName);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(String.Format("Error Writing to File: {0}", ex.Message));
                }
            }
        }

        private void btnClear_Click(object sender, RoutedEventArgs e)
        {
            switch (MessageBox.Show("This will delete all transactions from the list.\nDo you want to export your transactions first?",
                            "Clear Transactions",
                            MessageBoxButton.YesNoCancel,
                            MessageBoxImage.Question)) {
                case MessageBoxResult.Yes:
                    // "Yes" processing
                    SaveFileDialog sfd = new SaveFileDialog();
                    sfd.DefaultExt = "csv";
                    sfd.AddExtension = true;
                    sfd.Filter = "EVE MarketWatch CSV Files (*.csv)|*.csv";
    
                    if (sfd.ShowDialog() == true)
                    {
                        try
                        {
                            _viewModel.ExportTransactions(sfd.FileName);
                            _viewModel.ClearTransactions();
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show(String.Format("Error Writing to File: {0}", ex.Message));
                        }
                    }
                    break;
    
                case MessageBoxResult.No:
                    // "No" processing
                    _viewModel.ClearTransactions();
                    break;
    
                case MessageBoxResult.Cancel:
                    // "Cancel" processing
                    break;
                
            }
        }

        private void btnUpdatePrices_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.UpdatePrices();
        }

        private void miExit_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void miAutoSave_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog sfd = new SaveFileDialog();
            sfd.DefaultExt = "emw";
            sfd.AddExtension = true;
            sfd.Filter = "EVE Marketwatch Binary Transaction File (*.emw)|*.emw";

            if(sfd.ShowDialog() == true && sfd.FileName != _viewModel.TransactionFilePath)
            {
                if(System.IO.File.Exists(sfd.FileName))
                {
                    if(MessageBox.Show("This file already exists, do you want to import the transactions first?", "Import Transactions", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                    {
                        _viewModel.LoadTransactions(new System.IO.FileStream(sfd.FileName, System.IO.FileMode.Open));
                    }
                }
                _viewModel.TransactionFilePath = sfd.FileName;
            }
        }

        private void miSave_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.Save();
        }

        private void miSaveAs_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog sfd = new SaveFileDialog();
            sfd.DefaultExt = "emw";
            sfd.AddExtension = true;
            sfd.Filter = "EVE Marketwatch Binary Transaction File (*.emw)|*.emw|EVE MarketWatch CVS File (*.cvs)|*.cvs";

            if (sfd.ShowDialog() == true)
            {
                try
                {
                    if (sfd.FilterIndex == 1)
                    {
                        _viewModel.ExportItems(sfd.FileName);
                    }
                    else
                    {
                        _viewModel.SaveTransactions(new System.IO.FileStream(sfd.FileName, System.IO.FileMode.Create));
                    }
                    
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error saving transactions: {0}", ex.ToString());
                }
            }
        }
    }
}
