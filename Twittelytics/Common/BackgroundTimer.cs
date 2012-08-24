using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Twittelytics.Common
{
    public class BackgroundTimer
    {
        private AutoResetEvent _stopRequestEvent;
        private AutoResetEvent _stoppedEvent;

        #region Interval
        private TimeSpan _interval;
        public TimeSpan Interval
        {
            get
            {
                return _interval;
            }
            set
            {
                if (IsEnabled)
                {
                    Stop();
                    _interval = value;
                    Start();
                }
                else
                {
                    _interval = value;
                }
            }
        }
        #endregion

        public event EventHandler<object> Tick;

        #region IsEnabled
        private bool _isEnabled;
        public bool IsEnabled
        {
            get
            {
                return _isEnabled;
            }
            set
            {
                if (_isEnabled == value)
                    return;

                if (value)
                    Start();
                else
                    Stop();
            }
        }
        #endregion

        public BackgroundTimer()
        {
            _stopRequestEvent = new AutoResetEvent(false);
            _stoppedEvent = new AutoResetEvent(false);
        }

        public void Start()
        {
            if (_isEnabled)
            {
                return;
            }

            _isEnabled = true;
            _stopRequestEvent.Reset();
            Task.Run((Action)Run);
        }

        public void Stop()
        {
            if (!_isEnabled)
            {
                return;
            }

            _isEnabled = false;
            _stopRequestEvent.Set();
            _stoppedEvent.WaitOne();
        }

        private void Run()
        {
            while (_isEnabled)
            {
                _stopRequestEvent.WaitOne(_interval);

                if (_isEnabled &&
                    Tick != null)
                {
                    Tick(this, null);
                }
            }

            _stoppedEvent.Set();
        }
    }

}
