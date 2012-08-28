using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Windows.UI.Core;
using Windows.Foundation;
using Windows.UI.Xaml;
using LinqToTwitter;
using Twittelytics.Common;
using Windows.UI.Xaml.Controls;

namespace Twittelytics.Commands
{
    public class TwitterAuthorizePinCommand : ICommand
    {
        readonly Action callback;
        PinAuthorizer auth = new PinAuthorizer();

        public TwitterAuthorizePinCommand(Action action)
        {
            callback = action;
        }

        public bool CanExecute(object parameter)
        {
                return true;
        }

        public event EventHandler CanExecuteChanged;

        public void Execute(object parameter)
        {
            if (callback != null)
            {
                //callback((Action)parameter);
            }
        }

        private void AuthenticatePin(string pin)
        {   
            CoreDispatcher dispatcher = Application.Current.Resources.Dispatcher;
            auth.CompleteAuthorize(
                pin,
                completeResp => dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    switch (completeResp.Status)
                    {
                        case TwitterErrorStatus.Success:
                            SaveAuthInfo(completeResp);
                            //Frame.Navigate(typeof(ItemsPage), "AllGroups");
                            break;
                        case TwitterErrorStatus.RequestProcessingException:
                        case TwitterErrorStatus.TwitterApiError:
                            //new MessageDialog(completeResp.Error.ToString(), completeResp.Message).ShowAsync().AsTask().Wait();
                            break;
                    }
                }).AsTask().Wait());
        }

        private void SaveAuthInfo(TwitterAsyncResponse<UserIdentifier> completeResp)
        {
            auth.UserId = completeResp.State.UserID;
            auth.ScreenName = completeResp.State.ScreenName;
            SuspensionManager.SessionState["Authorizer"] = auth;
            LocalDataCredentials cred = new LocalDataCredentials
            {
                AccessToken = auth.Credentials.AccessToken,
                ConsumerKey = auth.Credentials.ConsumerKey,
                ConsumerSecret = auth.Credentials.ConsumerSecret,
                OAuthToken = auth.Credentials.OAuthToken,
                ScreenName = completeResp.State.ScreenName,
                UserId = completeResp.State.UserID
            };
            cred.Save();
            SuspensionManager.SessionState["SavedAuthorizer"] = auth;
        }
    }
}
