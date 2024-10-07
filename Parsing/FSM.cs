// This source code is based on code written for Ropley Information
// Technology Ltd. (RIT), and is offered for public use without warranty.
// You are entitled to edit or extend this code for your own purposes,
// but use of any unmodified parts of this code does not grant
// the user exclusive rights or ownership of that unmodified code. 
// While every effort has been made to deliver quality software, 
// there is no guarantee that this product offered for public use
// is without defects. The software is provided "as is," and you 
// use the software at your own risk. No warranties are made as to 
// performance, merchantability, fitness for a particular purpose, 
// nor are any other warranties expressed or implied. No oral or 
// written communication from or information provided by RIT 
// shall create a warranty. Under no circumstances shall RIT
// be liable for direct, indirect, special, incidental, or 
// consequential damages resulting from the use, misuse, or 
// inability to use this software, even if RIT has been
// advised of the possibility of such damages. Downloading
// opening or using this file in any way will constitute your 
// agreement to these terms and conditions. Do not use this 
// software if you do not agree to these terms.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Parsing
{
    /// <summary>
    /// An instance of a finite state machine
    /// </summary>

    public class FSM
    {
        /// <summary>
        /// Stream used for verbose FSM reporting. Use
        /// this to track the FSM's progress through
        /// the input events. Set/leave it as null to
        /// just not report this kind of information.
        /// </summary>

        public TextWriter DebugStream
        {
            get;
            set;
        }

        /// <summary>
        /// Use this to see error messages output by
        /// the FSM. Set/leave it as null to just
        /// not report this kind of information.
        /// </summary>

        public TextWriter ErrStream
        {
            get;
            set;
        }

        /// <summary>
        /// Helper method for outputting text to
        /// the debug and error output streams.
        /// Has formatting behaviour compatible
        /// with TextWriter.Write(format, args)
        /// </summary>
        /// <param name="isError">True to write to the error
        /// stream as well as the debug stream</param>
        /// <param name="format">Format argument, obeying
        /// string.Format syntax</param>
        /// <param name="args">Arguments as used by the
        /// format argument</param>

        public void MessageWrite(bool isError, string format, params object[] args)
        {
            DebugStream?.Write(format, args);
            if (isError && ErrStream != null)
                ErrStream.Write(format, args);
        }

        /// <summary>
        /// Helper method for outputting text to
        /// the debug and error output streams.
        /// Has formatting behaviour compatible
        /// with TextWriter.WriteLine(format, args)
        /// </summary>
        /// <param name="isError">True to write to the error
        /// stream as well as the debug stream</param>
        /// <param name="format">Format argument, obeying
        /// string.Format syntax</param>
        /// <param name="args">Arguments as used by the
        /// format argument</param>

        public void MessageWriteLine(bool isError, string format, params object[] args) => MessageWrite(isError, format + "\r\n", args);

        private FSMState currentState;

        /// <summary>
        /// Provide read-only access to the current state
        /// </summary>

        public FSMState State => currentState;

        private FSMTable fsmTable;

        /// <summary>
        /// The state machine description
        /// </summary>

        public FSMTable FSMTable
        {
            get => fsmTable;

            set
            {
                if (value == null)
                    return;

                if (value.Tokens == null)
                    throw new ArgumentException
                        ("Badly formed FSM tables passed to FSM");

                // If the state table is changing,
                // we shall launch a new parser

                fsmTable = value;
                currentState = fsmTable.States[0];

                // If this is a newly attached parser table,
                // none of the late-bound guard and action
                // delegates will have been initialised

                if (!fsmTable.Bound)
                    fsmTable.Bind(GetType());
            }
        }

        private IEnumerator<IToken> tokenSource;

        /// <summary>
        /// Return true if the state machine has run to completion. This
        /// is recognised by the current state having transited to null.
        /// </summary>

        public bool Terminated => currentState == null;

        /// <summary>
        /// Execute the state machine, consuming events/tokens
        /// from the enumerable event source
        /// </summary>
        /// <param name="eventSource">The source of input events</param>
        /// <returns>True if the state machine completes successfully,
        /// false if an illegal event/guard combination occurs in
        /// some state.</returns>

        public bool Run(IEnumerable<IToken> eventSource)
        {
            // Validate the input token stream handle

            if (eventSource == null)
                throw new ArgumentNullException
                    (nameof(eventSource), "Need an input token source to run FSM");

            // Validate the presence of parsing rule tables

            if (FSMTable == null)
                throw new InvalidOperationException
                    ("Attempting to run state machine without setting state tables");

            // Get an iterator over the input token stream

            tokenSource = eventSource.GetEnumerator();

            // A null value in currentState means a transition
            // took place that terminated the state machine.

            while (!Terminated)
            {
                IToken itok;

                // Acquire the next input event or token.
                // If the input stream dries up, prevent
                // the run proceeding after this token.

                if (tokenSource.MoveNext() == false)
                    return false;
                else
                    itok = tokenSource.Current;

                // Handle the event

                if (ProcessEvent(itok) == false)
                    return false;
            }
            return true;
        }

        /// <summary>
        /// The most recent event or token that is
        /// being processed in the current transition
        /// </summary>

        public IToken CurrentEvent
        {
            get;
            private set;
        }

        /// <summary>
        /// Inject one event into the state machine
        /// and perform the consequent transition.
        /// </summary>
        /// <param name="tok">The input event to process</param>
        /// <returns>True if event processing succeeded,
        /// false if the machine was already terminated,
        /// or there is no matching transition for the
        /// the event and guard conditions.</returns>

        public bool ProcessEvent(IToken tok)
        {
            CurrentEvent = tok;

            // Cannot proceed with processing this event
            // if the machine has terminated

            if (Terminated)
            {
                MessageWriteLine(true, "Machine previously terminated");
                return false;
            }

            // Output debugging messages. The test for
            // DebugStream is to preserve execution speed,
            // even though it is tested in MessageWriteLine.

            if (DebugStream != null)
                MessageWriteLine(false,
                    $"Input event {FSMTable.Tokens[tok.Type]} in state {currentState.Name}");

            // Find the matching transition for this event

            FSMTransition transition = currentState
                .Transitions.FirstOrDefault
                (
                    t => t.InputToken == tok.Type &&
                    (
                        t.Condition == null ||
                        t.Condition.Value(this)
                    )
                );

            // If the event is unrecognised, this is an
            // error in the state machine, so abort. If
            // ErrorRecoveryEnabled is set in the state
            // transition tables, this indicates we should
            // just ignore the input event and carry on.

            if (transition == null)
            {
                MessageWriteLine(!FSMTable.ErrorRecoveryEnabled, "No matching transition found");
                return FSMTable.ErrorRecoveryEnabled;
            }
            else if (DebugStream != null)
                MessageWriteLine(false,
                    $"Transition {transition.ToString(FSMTable.Tokens)} taken\r\nExit actions:");

            // Now perform the transition. Begin by executing
            // any exit actions for the current state.

            ExecuteActionList(currentState.ExitActions);

            // Execute any actions on the transition itself

            if (DebugStream != null)
                MessageWriteLine(false, "Transition actions:");
            ExecuteActionList(transition.InlineActions);

            // Transit to the new state

            currentState = transition.NextState;

            // Finally execute the entry actions associated
            // with the state into which we are transiting.
            // Note that on termination of the state machine,
            // we perform a transition to the 'null' state,
            // in which case there would be no entry action.

            if (!Terminated)
            {
                if (DebugStream != null)
                    MessageWriteLine(false, "Entry actions:");

                ExecuteActionList(currentState.EntryActions);
            }
            else if (DebugStream != null)
                MessageWriteLine(false, "Machine terminates");

            // Processing of event completed successfully

            return true;
        }

        /// <summary>
        /// Given a linked list of action function proxies,
        /// execute the actions in the order specified.
        /// </summary>
        /// <param name="action">The head of the list of actions</param>

        private void ExecuteActionList(FSMInlineAction action)
        {
            while (action != null)
            {
                if (DebugStream != null)
                    MessageWriteLine(false, $"\t{action.MethodName}();");
                action.Function(this);
                action = action.Next;
            }
        }
    }
}
