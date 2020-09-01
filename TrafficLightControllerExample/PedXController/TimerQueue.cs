using System;
using System.Collections.Generic;
using System.Threading;

namespace PedXController
{
    /// <summary>
    /// A timer queue for managing timed events
    /// </summary>

    public class TimerQueue
    {
        private readonly List<TimedWorkItem> workItems;
        private readonly Timer queueTimer;
        private readonly object lockObject;

        public TimerQueue()
        {
            // New timers have an empty scheduler
            // list and a non-ticking timer

            lockObject = new object();
            workItems = new List<TimedWorkItem>();
            queueTimer = new Timer(TimerTicks);
        }

        /// <summary>
        /// The event handler that is invoked every time the timer expires
        /// </summary>
        /// <param name="unused">Part of the TimerCallback signature, unused here</param>

        private void TimerTicks(object unused)
        {
            // All items at the head of the scheduler queue are
            // popped from the head of the queue and scheduled,
            // if their scheduled run time has since passed by.

            Monitor.Enter(lockObject);
            while (workItems.Count > 0 && workItems[0].When < DateTime.Now)
            {
                TimedWorkItem twi = workItems[0];
                workItems.RemoveAt(0);
                Monitor.Exit(lockObject);
                twi.What();
                Monitor.Enter(lockObject);
            }

            // The queue is now either empty, or has items that have yet
            // to be scheduled, as their time is in the future. Empty queues
            // have the timer turned off.

            if (workItems.Count == 0)
                queueTimer.Change(Timeout.Infinite, Timeout.Infinite);
            else
            {
                // Items in the queue are all in the future. Schedule
                // the timer to expire at the time of the first item
                // in the queue.

                int milliseconds = (int)((workItems[0].When - DateTime.Now).TotalMilliseconds);
                if (milliseconds < 0)
                    milliseconds = 0;
                queueTimer.Change(milliseconds, Timeout.Infinite);
            }
            Monitor.Exit(lockObject);
        }

        /// <summary>
        /// Schedule a new action for some time in the future
        /// </summary>
        /// <param name="time">When we want the action to execute</param>
        /// <param name="action">What the action is to be executed</param>

        public void Schedule(DateTime time, Action action)
        {
            // Add the item into the list at the right offset into the list.
            // Tune the timer's next tick time, as the list has changed.

            TimedWorkItem twi = new TimedWorkItem(time, action);
            Monitor.Enter(lockObject);
            workItems.Add(twi);
            workItems.Sort((tw1, tw2) => Math.Sign(tw1.When.Ticks - tw2.When.Ticks));
            Monitor.Exit(lockObject);
            TimerTicks(null);
        }
    }
}
