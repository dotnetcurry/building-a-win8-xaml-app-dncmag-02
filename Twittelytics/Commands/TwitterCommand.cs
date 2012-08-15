using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Twittelytics.Commands
{
    public class TwitterCommand<T> : ICommand
    {
        readonly Action<T> callback;

        public TwitterCommand(Action<T> handler)
        {
            callback = handler;
           
        }

        public bool CanExecute(object parameter)
        {
            return true;
        }

        public event EventHandler CanExecuteChanged;

        public void Execute(object parameter)
        {
            if (callback!=null)
            {
                callback((T)parameter);
            }
        }
    }
}
