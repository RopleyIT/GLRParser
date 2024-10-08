﻿using System;

namespace PedXController
{
    /// <summary>
    /// A timed function
    /// </summary>
    /// <remarks>
    /// Constructor
    /// </remarks>
    /// <param name="when">Time of new event</param>
    /// <param name="what">Action delegate for what to execute</param>
    /// <param name="label">Optional monitoring label for event</param>

    public class TimedWorkItem(DateTime when, Action what)
    {
        /// <summary>
        /// The time and date on which the event is to occur
        /// </summary>

        public DateTime When
        {
            get;
            private set;
        } = when;

        /// <summary>
        /// Delegate to the action function to
        /// be invoked when the timer expires
        /// </summary>

        public Action What
        {
            get;
            private set;
        } = what;
    }
}
