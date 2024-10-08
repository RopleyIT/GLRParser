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
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Parsing
{
    /// <summary>
    /// Functor class that creates finite state
    /// machines out of grammar descriptions
    /// </summary>
    /// <typeparam name="T">The type of the
    /// application-specific state machine class
    /// </typeparam>

    public static class FSMFactory<T> where T : FSM, new()
    {
        private static FSMTable fsmTable;

        /// <summary>
        /// The state transition table used by the FSM instances created
        /// by this state machine factory. This is initialised in the
        /// constructor for the FSM factory, either by parameters to the
        /// factory constructor causing the grammar to be parsed,
        /// compiled and dynamically loaded for an inline FSM, or
        /// by just calling the initialisation method in the
        /// application-specific parser's auto-generated derived class.
        /// </summary>

        public static FSMTable FSMTable
        {
            get
            {
                if (fsmTable == null)
                {
                    CompilerLoader<T> loader = new();
                    Type fsmType = loader.GetDerivedType(typeof(T).Assembly);
                    if (fsmType != null)
                    {
                        fsmTable = (FSMTable)fsmType
                            .GetMethod("InitFSMTable")
                            .Invoke(null, null);
                    }
                    else
                        throw new InvalidOperationException
                        (
                            "Could not load the assembly containing the autogenerated " +
                            $"FSM class derived from {typeof(T).FullName}. " +
                            "Use ParseLR to generate source code for the offline " +
                            "FSM, and add it to your project."
                        );
                }
                return fsmTable;
            }
        }

        /// <summary>
        /// Rapid access to the token name/value lookup table for the state machine
        /// </summary>

        public static TwoWayMap<string, int> Tokens => FSMTable.Tokens;

        /// <summary>
        /// Initialize an FSM factory for an inline state machine. Given
        /// the FSM class T, this method needs to deduce the
        /// type of the corresponding autogenerated parser class that
        /// is derived from it, compile the source code into a loaded
        /// dynamic assembly, then create an instance of its FSMTable.
        /// </summary>
        /// <param name="input">A string containing the
        /// input grammar description.</param>
        /// <param name="ignoreErrorEvents">True to make
        /// the state machine engine not halt on an
        /// unrecognised input event, but just drop
        /// it. False to make it terminate on the
        /// first erroneous input event.</param>
        /// <param name="sourceOutput">If non-null
        /// the FSM source code is written to here
        /// </param>

        public static void InitializeFromGrammar
        (
            string grammar,
            bool ignoreErrorEvents,
            TextWriter sourceOutput = null
        )
        {
            using StringReader srg = new(grammar);
            InitializeFromGrammar
            (
                srg,
                ignoreErrorEvents,
                sourceOutput
            );
        }

        /// <summary>
        /// Initialise a FSM factory for an inline state machine.
        /// Given the FSM class T, this method needs to deduce the
        /// type of the corresponding autogenerated FSM class that
        /// is derived from it, compile the source code into a loaded
        /// dynamic assembly, then create an instance of its FSMTable.
        /// </summary>
        /// <param name="input">A stream from which the
        /// input grammar description is read.</param>
        /// <param name="ignoreErrorEvents">True to make
        /// the state machine engine not halt on an
        /// unrecognised input event, but just drop
        /// it. False to make it terminate on the
        /// first erroneous input event.</param>
        /// <param name="sourceOutput">If non-null
        /// the FSM source code is written to here
        /// </param>

        public static void InitializeFromGrammar
        (
            TextReader input,
            bool ignoreErrorEvents,
            TextWriter sourceOutput = null
        )
        {
            List<string> assemblyRefs;
            StringBuilder fsmSource = new();
            using (StringWriter sourceWriter = new(fsmSource))
            {
                // Generate the source code for the autogenerated
                // derived parser class into a string builder so
                // that it can then be compiled inline.

                CompilerErrors =
                    FSMFactory.CreateOfflineStateMachine
                    (
                        input,
                        sourceWriter,
                        ignoreErrorEvents,
                        out assemblyRefs
                    );

                if (!string.IsNullOrEmpty(CompilerErrors))
                    throw new ArgumentException(CompilerErrors);
            }

            // Write out the source code if the sourceOutput
            // argument points at a valid TextWriter

            sourceOutput?.Write(fsmSource);

            // Compile the parser source into an in-memory
            // assembly so that an instance can be created.

            CompilerLoader<T> loader = new();
            Type fsmType = loader.CompileAutogenAssembly(fsmSource, assemblyRefs);
            CompilerErrors = loader.CompilerErrors;

            // Load the type, and run its static InitParserTable method
            // so that we have an initialised parser table to use when
            // creating instances.

            if (fsmType != null)
            {
                fsmTable = (FSMTable)fsmType
                    .GetMethod("InitFSMTable")
                    .Invoke(null, null);
            }
            else
                throw new ArgumentException
                    ($"Could not compile the inline FSM assembly.\r\n{CompilerErrors}");
        }

        /// <summary>
        /// Create a finite state machine instance based on the
        /// state transition tables initialised within this
        /// FSM factory.
        /// </summary>
        /// <typeparam name="T">The type of the derived
        /// FSM class we want to create. Note that this
        /// derived FSM class must have a default
        /// constructor or no constructor at all, and
        /// must obviously be inherited from FSM.</typeparam>
        /// <returns>A new state machine instance</returns>

        public static T CreateInstance() => FSMTable.CreateFSM<T>();

        /// <summary>
        /// When building an inline state machine, the compiler is
        /// run silently in the background. If the input grammar
        /// contains errors, they will pop up here.
        /// </summary>

        public static string CompilerErrors
        {
            get;
            private set;
        }
    }

    public static class FSMFactory
    {
        /// <summary>
        /// Given an input state machine grammar description,
        /// generate the output source code for the auto-generated
        /// half of the state machine class.
        /// </summary>
        /// <param name="inputStream">A stream from which the
        /// input grammar description is read.</param>
        /// <param name="outputStream">A stream to which the
        /// output source code will be written.</param>
        /// <param name="ignoreErrorEvents">True to make
        /// the state machine engine not halt on an
        /// unrecognised input event, but just drop
        /// it. False to make it terminate on the
        /// first erroneous input event.</param>
        /// <param name="assemblyRefs">
        /// The list of DLLs referenced from the FSM
        /// grammar source</param>
        /// <returns>An error message if the creation
        /// fails. An empty string if it does not.</returns>

        public static string CreateOfflineStateMachine
        (
            TextReader inputStream,
            TextWriter outputStream,
            bool ignoreErrorEvents,
            out List<string> assemblyRefs
        )
        {
            assemblyRefs = [];
            try
            {
                FSMOutput fsmGenerator = new(outputStream);
                return fsmGenerator.BuildSource(inputStream,
                    ignoreErrorEvents, out assemblyRefs);
            }
            catch (Exception x)
            {
                return x.Message;
            }
        }
    }
}
