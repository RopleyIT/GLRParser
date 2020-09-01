using Parsing;
using System;
using System.Collections.Generic;
using System.IO;

namespace PedXController
{
    /// <summary>
    /// The implementation of the application-specific
    /// parts of the state machine controller.
    /// </summary>

    public class CrossingController : FSM, IEnumerable<IToken>
    {
        /// <summary>
        /// Factory method
        /// </summary>
        /// <returns>An instance of a crossing controller</returns>

        public static CrossingController Create(TextWriter debugStream, TextWriter errStream)
        {
            // Find the path to the grammar file so that it can be parsed into
            // a dynamic assembly at runtime. The grammar file is copied to the
            // executable folder on build as if it were a resource.

            string grammarFileLocation = typeof(CrossingController).Assembly.Location;
            grammarFileLocation = Path.GetDirectoryName(grammarFileLocation);
            grammarFileLocation = Path.Combine(grammarFileLocation, "CrossingController.g");

            // Parse the grammar file to create the state machine engine for
            // the traffic light controller.

            using StreamReader grammarStream = new StreamReader(grammarFileLocation);
            FSMFactory<CrossingController>.InitializeFromGrammar(grammarStream, false);
            CrossingController cc = FSMFactory<CrossingController>.CreateInstance();
            return cc;
        }

        /// <summary>
        /// The traffic light being controlled
        /// </summary>

        public TrafficLight Light
        {
            get;
            private set;
        }

        /// <summary>
        /// The pedestrian walk indicator being controlled
        /// </summary>

        public StopWalkIndicator WalkIndicator
        {
            get;
            private set;
        }

        private readonly TimerQueue crossingTimer;

        /// <summary>
        /// Create a crossing controller that starts
        /// life permitting vehicles to pass, and
        /// telling pedestrians to wait.
        /// </summary>

        public CrossingController()
        {
            Light = new TrafficLight();
            WalkIndicator = new StopWalkIndicator
            {
                ButtonPressed = false
            };
            crossingTimer = new TimerQueue();
            inputEvents = new Queue<IToken>();
        }

        // Actions that set the colour of the
        // traffic lights, or the crossing indicator.

        public void SetLightRedAndYellow() => Light.Colour = LightColour.REDANDYELLOW;

        public void SetLightGreen() => Light.Colour = LightColour.GREEN;

        public void SetLightRed() => Light.Colour = LightColour.RED;

        public void SetLightYellow() => Light.Colour = LightColour.YELLOW;

        public void SetLightWalk() => WalkIndicator.CanWalk = true;

        public void SetLightDontWalk() => WalkIndicator.CanWalk = false;

        // Action to capture the fact that the
        // request to cross button has been pressed.

        public void RecordButtonPressed() => WalkIndicator.ButtonPressed = true;

        // Actions to queue a time interval for the
        // various phases of the crossing controller.

        public void SetGreenTimer() => crossingTimer.Schedule(DateTime.Now.AddSeconds(3), InjectTickEvent);

        public void SetYellowTimer() => crossingTimer.Schedule(DateTime.Now.AddSeconds(3), InjectTickEvent);

        public void SetWalkTimer() => crossingTimer.Schedule(DateTime.Now.AddSeconds(3), InjectTickEvent);

        public void SetAllRedTimer() => crossingTimer.Schedule(DateTime.Now.AddSeconds(3), InjectTickEvent);

        public void SetRedAndYellowTimer() => crossingTimer.Schedule(DateTime.Now.AddSeconds(3), InjectTickEvent);

        // Clear down the record of the request to
        // cross button, as the crossing has been
        // permitted.

        public void ClearButtonPress() => WalkIndicator.ButtonPressed = false;

        // Event input management

        private readonly Queue<IToken> inputEvents;

        /// <summary>
        /// Cause a timer tick event to be placed on the input
        /// </summary>

        public void InjectTickEvent() => ProcessEvent(new ParserToken(FSMTable.Tokens["TIMERTICK"]));

        /// <summary>
        /// Cause a button press event to be placed on the input
        /// </summary>

        public void InjectButtonPress() => ProcessEvent(new ParserToken(FSMTable.Tokens["BUTTONPRESS"]));

        /// <summary>
        /// Capture the next input event from the input queue. Note that
        /// this uses a polled loop to capture the event. A better approach
        /// would be to use event objects on the queue.
        /// </summary>
        /// <returns>Each successive input token</returns>

        public IEnumerator<IToken> GetEnumerator()
        {
            while (inputEvents.Count <= 0)
                System.Threading.Thread.Sleep(500);
            yield return inputEvents.Dequeue();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            while (inputEvents.Count <= 0)
                System.Threading.Thread.Sleep(500);
            yield return inputEvents.Dequeue();
        }
    }
}
