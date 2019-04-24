using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FModel.Custom
{
    public class TypeAssistant
    {
        public event EventHandler Idled = delegate { };
        public int WaitingMilliSeconds { get; set; }
        System.Threading.Timer waitingTimer;

        public TypeAssistant(int waitingMilliSeconds = 600)
        {
            WaitingMilliSeconds = waitingMilliSeconds;
            waitingTimer = new Timer(p =>
            {
                Idled(this, EventArgs.Empty);
            });
        }
        public void TextChanged()
        {
            waitingTimer.Change(WaitingMilliSeconds, System.Threading.Timeout.Infinite);
        }
    }
}
