﻿using DynamicCSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Parsing
{
    /// <summary>
    /// For a given parser/FSM class T this class
    /// provides the C# compiler services and the
    /// lookup facility for the autogenerated derived
    /// class from the parser/FSM class T.
    /// </summary>
    /// <typeparam name="T">The type of the
    /// application-specific parser or FSM
    /// class from which the autogenerated
    /// class will be derived.</typeparam>

    public class CompilerLoader<T>
    {
        // The set of referenced assemblies used by the default parser

        private static readonly string[] defaultAssemblies =
        [
            "System",
            "System.Private.CoreLib",
            "System.Runtime",
            "System.Collections",
            "System.Linq",
            "System.IO"
        ];

        /// <summary>
        /// Compile a parser or state machine source code
        /// file and create a loaded dynamic assembly from it.
        /// </summary>
        /// <param name="source">The source code to be compiled.</param>
        /// <param name="referencedAssemblies">A list of DLL paths that
        /// must be referenced by the assembly.</param>
        /// <returns>The reflection type of the compiled and loaded
        /// assembly, compiled from the input source code.</returns>

        public Type CompileAutogenAssembly
            (StringBuilder source, IEnumerable<string> referencedAssemblies)
        {
            if (source == null)
                throw new ArgumentException("Trying to compile a null parser source");

            // Use the DynamicCSharp library to build and load the new assembly

            ICompiler compiler = Compiler.Create();
            compiler.Source = source.ToString();
            compiler.SourceStream = null;

            // Reference the .NET classes used by the default
            // autogenerated parser source code

            compiler.AddAssemblyReferences(defaultAssemblies);

            // Reference the assembly that contains the base class of the parser

            Assembly baseAssembly = typeof(T).Assembly;
            compiler.AddAssemblyReference(baseAssembly.GetName().Name);

            // Reference the parsing libraries

            compiler.AddAssemblyReference("Parsing");
            compiler.AddAssemblyReference("ParserGenerator");
            compiler.AddAssemblyReference("BooleanLib");

            // Reference any assemblies explicitly added in the grammar file

            if (referencedAssemblies != null && referencedAssemblies.Any())
                compiler.AddAssemblyReferences(referencedAssemblies);

            compiler.AssemblyName = AutoGenAssemblyName;
            compiler.Compile(true); // false for non-debug assembly
            if (compiler.HasErrors)
            {
                CompilerErrors = $"Errors building assembly {compiler.AssemblyName}\r\n";
                foreach (var diag in compiler.Diagnostics)
                    CompilerErrors += $"{diag}\r\n";
                return null;
            }
            else
                return GetDerivedType(compiler.Assembly);
        }

        /// <summary>
        /// When building an inline state machine, the compiler is
        /// run silently in the background. If the input grammar
        /// contains errors, they will pop up here.
        /// </summary>

        public string CompilerErrors
        {
            get;
            private set;
        } = string.Empty;

        /// <summary>
        /// Given the type for the FSM class that the programmer sees,
        /// and the assembly the actual FSM type to be created is
        /// located in, get the type for the FSM to be created.
        /// </summary>
        /// <param name="autogenAssembly">The assembly in which the
        /// derived FSM class to be created is located.</param>
        /// <returns>The type for the derived FSM class to
        /// be constructed.</returns>

        public Type GetDerivedType(Assembly autogenAssembly)
        {
            if (autogenAssembly == null)
                throw new ArgumentNullException(nameof(autogenAssembly));

            // Assume that the autogenerated derived FSM class is in
            // the same assembly as the programmer-written one. This would
            // be the case in an offline parser where the parser-generated
            // source code would be added to the same project as the
            // programmer-written parser class.

            Type derivedType = autogenAssembly.GetType(AutoGenClassName, false);
            if (derivedType == null)
                CompilerErrors += $"Could not load derived class {AutoGenClassName}\r\n";
            return derivedType;
        }

        /// <summary>
        /// The full name of the class autogenerated to contain the state machine
        /// guards and actions for this parser.
        /// </summary>

        public string AutoGenClassName =>
             $"{typeof(T).Namespace}.AutoGenerated.{typeof(T).Name}_AutoGenerated";

        /// <summary>
        /// The default name to allocate to the autogenerated assembly
        /// </summary>

        public string AutoGenAssemblyName =>
            $"DynAssem_{UniqueId()}";

        private static readonly string IdCharacters =
            "abcdefghijklmnop" +
            "qrstuvwxyzABCDEF" +
            "GHIJKLMNOPQRSTUV" +
            "WXYZ0123456789_µ";

        /// <summary>
        /// Generate a unique six letter/digit identifier
        /// </summary>
        /// <returns>The six character identifier</returns>

        private static string UniqueId()
        {
            long stamp = DateTime.Now.Ticks;

            string result = string.Empty;
            for (int i = 0; i < 10; i++)
                result += IdCharacters[(int)(stamp >> (6 * i)) & 0x3F];
            return result;
        }
    }
}
