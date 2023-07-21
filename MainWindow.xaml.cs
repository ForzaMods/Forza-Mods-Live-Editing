using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Memory;

namespace ForzaMods_LiveEditing
{
    public partial class MainWindow : Window
    {
        private Mem m = new Mem();
        private IEnumerable<long> AoBScanResults;
        private string original_bytes;
        public MainWindow()
        {
            InitializeComponent();
            Thread fattach = new Thread(forza);
            fattach.Start();
        }

        void forza()
        {
            bool a = false;
            
            while (true)
            {
                Thread.Sleep(500);
                if (m.OpenProcess("ForzaHorizon5"))
                {
                    if (a) 
                        continue;

                    Update_Status("Opened forza process");

                    Dispatcher.BeginInvoke((Action)delegate
                    {
                        Replace_Box.IsEnabled = true;
                        Scan_For_Aob_Box.IsEnabled = true;
                        Scan_For_Aob_Button.IsEnabled = true;
                        Replace_Button.IsEnabled = true;
                        Revert_Button.IsEnabled = true;
                    });
                    a = true;
                }
                else
                {
                    if (!a) 
                        continue;
                    Update_Status("Doing nothing");

                    Dispatcher.BeginInvoke((Action)delegate
                    {
                        Replace_Box.IsEnabled = false;
                        Scan_For_Aob_Box.IsEnabled = false;
                        Scan_For_Aob_Button.IsEnabled = false;
                        Replace_Button.IsEnabled = false;
                        Revert_Button.IsEnabled = false;
                    });
                    a = false;
                }
            }
        }
        
        private void Scan_For_Aob_Button_OnClick(object sender, RoutedEventArgs e)
        {
            Update_Status("Started scan");
            string aob_to_scan = Scan_For_Aob_Box.Text;
            original_bytes = Scan_For_Aob_Box.Text;

            Task.Run(async() =>
            {
                try
                {
                    AoBScanResults = await m.AoBScan(aob_to_scan, true, true, false);
                }
                catch {await Dispatcher.BeginInvoke((Action)delegate
                {
                    Update_Status("Failed");
                });}

                await Dispatcher.BeginInvoke((Action)delegate
                {
                    Update_Status("Finished scan");
                });
            });
        }

        private void Replace_Button_OnClick(object sender, RoutedEventArgs e)
        {
            string scanned_bytes = Scan_For_Aob_Box.Text;
            string replacement_bytes = Replace_Box.Text;

            if (check_if_the_bytes_match(replacement_bytes, scanned_bytes))
            {
                Task.Run(() =>
                {
                    foreach (long result in AoBScanResults)
                    {
                        m.WriteMemory(result.ToString("X"), "bytes", replacement_bytes);
                        Update_Status("Wrote bytes to: " + result.ToString("X"));
                    }
                });
            }
        }

        void Update_Status(string replacement)
        {
            Dispatcher.BeginInvoke((Action)delegate () { Status.Content = "Status: " + replacement; });
        }

        bool check_if_the_bytes_match(string bytes1, string bytes2)
        {
            if (bytes1.Length != bytes2.Length)
            {
                MessageBox.Show("The bytes dont match");
                return false;
            }
            return true;   
        }

        private void Revert_Button_OnClick(object sender, RoutedEventArgs e)
        {
            Task.Run(() =>
            {
                foreach (long result in AoBScanResults)
                {
                    m.WriteMemory(result.ToString("X"), "bytes", original_bytes);
                    Update_Status("Wrote bytes to: " + result.ToString("X"));
                }
            });
        }

        private void Guide_Button_Click(object sender, RoutedEventArgs e)
        {
            var guide = new Guide();
            guide.Show();
        }

        private async void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            try
            {
                await Task.Run(() =>
                {
                    foreach (long result in AoBScanResults)
                    {
                        m.WriteMemory(result.ToString("X"), "bytes", original_bytes);
                        Update_Status("Wrote bytes to: " + result.ToString("X"));
                    }
                });
            }
            catch { }
            
            Environment.Exit(0);
        }
    }
}
