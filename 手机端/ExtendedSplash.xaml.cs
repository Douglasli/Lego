using System;
using System.IO.IsolatedStorage;
using System.Windows;
using System.Windows.Navigation;
using J2i.Net.Ntp;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Net.NetworkInformation;
using Microsoft.Phone.Reactive;

namespace sdkBasicCameraCS
{
    public partial class ExtendedSplash : PhoneApplicationPage
    {
        private IDisposable timeoutAction;

        public ExtendedSplash()
        {
            InitializeComponent();
        }


        void ntpClient_TimeReceived(object _, NtpClient.TimeReceivedEventArgs e)
        {
            timeoutAction.Dispose();

            Dispatcher.BeginInvoke(() =>
            {
                Action nextAction = () =>
                {
                    NavigationService.Navigate(new Uri("/MainPage.xaml", UriKind.Relative));
                    NavigationService.RemoveBackEntry();
                };

                if (e != null)
                {
                    TimeSpan ts = (DateTime.UtcNow - e.CurrentTime);
                    App.TimeDifference = (long)ts.TotalMilliseconds;
                    IsolatedStorageSettings.ApplicationSettings["TimeDifference"] = App.TimeDifference;
                    IsolatedStorageSettings.ApplicationSettings.Save();

                    differenceTextBlock.Text = App.TimeDifference.ToString("d");
                    txtCurrentTime.Text = e.CurrentTime.ToLongTimeString();
                    txtSystemTime.Text = DateTime.Now.ToUniversalTime().ToLongTimeString();
                }
                else if (IsolatedStorageSettings.ApplicationSettings.Contains("TimeDifference"))
                {
                    differenceTextBlock.Text = txtCurrentTime.Text = txtSystemTime.Text
                        = "Getting time failed. Use stored Value.";
                    App.TimeDifference = Convert.ToInt64(IsolatedStorageSettings.ApplicationSettings["TimeDifference"]);
                }
                else
                {
                    differenceTextBlock.Text = txtCurrentTime.Text = txtSystemTime.Text = "Getting time failed. Try later.";

                    nextAction = () => App.Current.Terminate();
                }
                Scheduler.Dispatcher.Schedule(nextAction, TimeSpan.FromSeconds(1));
            });
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            App.LocalAddress = App.GetMyIPAddress();
            if (NetworkInterface.GetIsNetworkAvailable() == false
                || App.LocalAddress == null)
            {
                MessageBox.Show("No available network. Turn on Wifi and try again.");
                Application.Current.Terminate();
                return;
            }


            var ntpClient = new NtpClient();
            ntpClient.TimeReceived += ntpClient_TimeReceived;
            ntpClient.RequestTime();

            //dispatcherTimer = new DispatcherTimer();
            //dispatcherTimer.Interval = TimeSpan.FromSeconds(3);
            //dispatcherTimer.Tick += (sender, e1) => ntpClient_TimeReceived(null, null);
            //dispatcherTimer.Start();

            timeoutAction = Scheduler.Dispatcher.Schedule(() => ntpClient_TimeReceived(null, null));

        }

    }
}