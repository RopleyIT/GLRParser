﻿// This source code is based on code written for Ropley Information
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
using System.Linq.Expressions;

namespace Parsing
{
    /// <summary>
    /// Contains the shared data structures that define a state machine.
    /// Note that one instance of this class can be shared across a
    /// number of parallel state machine instances.
    /// </summary>

    public class FSMTable
    {
        /// <summary>
        /// Constructor. Initialises the state table and guard evaluators.
        /// </summary>
        /// <param name="states">The behavioural description of
        /// the finite state machine.</param>
        /// <param name="guards">The list of guard evaluators
        /// used as conditions on events.</param>
        /// <param name="tokens">The lookup table that
        /// associates names with integer values</param>

        public FSMTable(FSMState[] states,
            TwoWayMap<string, int> tokens, bool ignoreErrors)
        {
            States = states;
            Tokens = tokens;
            Bound = false;
            ErrorRecoveryEnabled = ignoreErrors;
        }

        /// <summary>
        /// The state transition table
        /// </summary>

        public FSMState[] States
        {
            get;
            private set;
        }

        /// <summary>
        /// Indicates whether the state machine is expected to
        /// just drop events that are not recognised for the
        /// current state (true), or just terminate (false).
        /// </summary>

        public bool ErrorRecoveryEnabled
        {
            get;
            set;
        }

        /// <summary>
        /// Lookup table for names of tokens as used
        /// in the grammar, since we are using
        /// inbounded integers as the token types
        /// rather than an enum, so that the tokens
        /// can be set at runtime.
        /// </summary>

        public TwoWayMap<string, int> Tokens
        {
            get;
            set;
        }

        /// <summary>
        /// Indicates whether the run-time binding operation
        /// has taken place on the FSM tables. This run-time
        /// binding compiles the proxies that link the actions
        /// and guards in the FSM tables with the equivalent
        /// methods in the derived parser class.
        /// </summary>

        public bool Bound
        {
            get;
            set;
        }

        /// <summary>
        /// At run-time, convert all the guard and action method names
        /// into proxy functions that call the real guard and action
        /// functions with those names in their derived FSM class.
        /// </summary>
        /// <param name="fsmType">The type of the FSM derived class</param>

        public void Bind(Type fsmType)
        {
            // Bind all the guard and action functions using a
            // compiled expression proxy. This has the benefits
            // of late binding, with the performance (nearly)
            // of compile time binding.

            foreach (FSMState state in States)
            {
                // Bind the entry and exit actions

                if (state.EntryActions != null)
                    state.EntryActions.Bind(fsmType);
                if (state.ExitActions != null)
                    state.ExitActions.Bind(fsmType);

                // Now bind the guards and transition actions

                foreach (FSMTransition col in state.Transitions)
                {
                    if (col.Condition != null)
                        col.Condition.Bind(fsmType, false);
                    if (col.InlineActions != null)
                        col.InlineActions.Bind(fsmType);
                }
            }

            // Mark the state tables as bound

            Bound = true;
        }

        /// <summary>
        /// Delegate to a method containing the code:
        /// return new MyParserNamespace.AutoGenerated.MyParser_AutoGenerated();
        /// where the programmer-written parser class was called
        /// MyParserNamespace.MyParser.
        /// </summary>

        private Delegate fsmDefaultConstructor = null;

        /// <summary>
        /// Initialize the factory delegate for creating 
        /// instances of the state machine class
        /// </summary>
        /// <param name="autoGenFSMType">The autogenerated FSM class</param>

        public void InitializeFSMConstructor(Type autoGenFSMType)
        {
            // Argument validation

            if (autoGenFSMType == null)
                throw new ArgumentNullException(nameof(autoGenFSMType));

            // Create dynamic IL code to invoke the constructor
            // for the found parser class type, then cache it
            // into the delegate field reserved for that purpose.

            LambdaExpression newLambda = Expression.Lambda(Expression.New(autoGenFSMType));
            fsmDefaultConstructor = newLambda.Compile();
        }

        /// <summary>
        /// Create an instance of the state machine whose
        /// class is associated with this FSMTable
        /// </summary>
        /// <typeparam name="T">The type of the FSM class to create. Note
        /// that this should be either the programmer's FSM class type
        /// as derived from Parsing.FSM, or could be just Parsing.FSM
        /// if we don't need the actual FSM type.</typeparam>
        /// <returns>An instance of the FSM with the specified class</returns>

        public T CreateFSM<T>() where T : FSM, new()
        {
            // Before this factory method is called, we need to bind the
            // constructor for the auto-generated derived FSM class so
            // that subsequent calls to this method execute very quickly.
            // To do this, call InitializeFSMConstructor before calling
            // this CreateFSM<T> method.

            if (fsmDefaultConstructor == null)
                throw new InvalidOperationException
                    ("InitializeFSMConstructor should be called before CreateFSM<T>");

            // Invoke the constructor for the auto-generated FSM derived class,
            // but return it as a reference to the programmer-written parser class
            // the programmer wrote. There is no reason for a developer to want
            // direct access to the autogenerated methods of the derived FSM
            // class. Note that the FSM table is also injected here.

            T newFSM = ((Func<T>)fsmDefaultConstructor)();
            newFSM.FSMTable = this;
            return newFSM;
        }
    }
}
