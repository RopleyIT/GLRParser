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

using System.Collections.Generic;

namespace ParserGenerator
{
    /// <summary>
    /// One state within the simple state machine recogniser
    /// </summary>

    public class State
    {
        /// <summary>
        /// The name of the current state. Should be a member
        /// of the Nonterminals list in the Grammar object.
        /// </summary>

        public GrammarToken StateName
        {
            get;
            set;
        }

        /// <summary>
        /// The list of possible transitions that may cause
        /// a transition from this state.
        /// </summary>

        public List<StateTransition> Transitions
        {
            get;
            private set;
        }

        /// <summary>
        /// Optional inline code to be executed
        /// when this state has been entered
        /// in the state machine.
        /// </summary>

        public GrammarGuardOrAction EntryCode
        {
            get;
            set;
        }

        /// <summary>
        /// Optional inline code to be executed
        /// when this state is being left
        /// in the state machine.
        /// </summary>

        public GrammarGuardOrAction ExitCode
        {
            get;
            set;
        }

        /// <summary>
        /// Create an instance of a state in a state machine
        /// </summary>
        /// <param name="stateName">The name of the new state</param>
        /// <param name="entryCode">An entry action function</param>
        /// <param name="exitCode">An exit action function</param>

        public State(GrammarToken stateName, GrammarGuardOrAction entryCode, GrammarGuardOrAction exitCode)
        {
            StateName = stateName;
            Transitions = new List<StateTransition>();
            EntryCode = entryCode;
            ExitCode = exitCode;
        }
    }
}
