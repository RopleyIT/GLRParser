using System;

namespace PedXController
{
    /// <summary>
    /// A timed function
    /// </summary>

    public class TimedWorkItem
    {
        /// <summary>
        /// The time and date on which the event is to occur
        /// </summary>

        public DateTime When
        {
            get;
            private set;
        }

        /// <summary>
        /// Delegate to the action function to
        /// be invoked when the timer expires
        /// </summary>

        public Action What
        {
            get;
            private set;
        }

        /// <summary>
        /// A debugging or monitoring label for the
        /// action function when it happens
        /// </summary>

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="when">Time of new event</param>
        /// <param name="what">Action delegate for what to execute</param>
        /// <param name="label">Optional monitoring label for event</param>

        public TimedWorkItem(DateTime when, Action what)
        {
            When = when;
            What = what;
        }
    }
}
