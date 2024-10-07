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

using Parsing;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace ParseLR
{
    /// <summary>
    /// Command line based LR parser
    /// </summary>

    internal class Program
    {
        // Internal copies of command line flags

        private static bool dumpTables = false;
        private static bool debugOutput = false;
        private static bool compressTables = false;
        private static bool errorRecovery = false;
        private static bool fsm = false;
        private static bool glrParser = false;

        // Paths to input and output files

        private static string inputFile = null;
        private static string outputFile = null;

        /// <summary>
        /// Entry point to application
        /// Usage: parselr [-tdprfg] [-h|?] input.g [output.cs]
        ///     [-t]    Generate parser table data file
        ///             in a '.tables.txt' file. Also
        ///             causes state description strings
        ///             to be more detailed in state table.
        ///     [-d]    Generate debug output to a file
        ///             showing how the parser worked, the
        ///             file having a '.debug.txt' name.
        ///     [-p]    Compress the parser table by removing
        ///             unnecessary duplicate states.
        ///     [-r]    Use yacc/bison style error recovery
        ///             when parsing the grammar. Without
        ///             this flag, parsing stops at the first
        ///             error encountered.
        ///     [-h or -?]
        ///             Generate this help information.
        ///     [-f]    Expects a grammar for a simple state
        ///             machine, rather than an LR parser. In
        ///             this case -p and -r flags are ignored.
        ///     [-g]    Expects an ambiguous grammar for which
        ///             a generalised LR parser will be produced.
        ///             In this case -p and -r flags are ignored,
        ///             and shift/reduce or reduce/reduce conflicts
        ///             are not reported as errors.
        ///     input.g
        ///             The input grammar file to be parsed.
        ///     [output.cs]
        ///             The optional file to which the C#
        ///             state tables are written. If
        ///             omitted, for an input grammar
        ///             file named 'input.g', the name
        ///             'input.designer.cs' will be
        ///             used as the output file name.
        /// </summary>
        /// <param name="args">Command line arguments</param>

        private static void Main(string[] args)
        {
            // Parse the command line arguments

            if (!ParseArguments(args))
                return;

            // The four input/output channels used by the parser

            StreamReader? inputStream = null;
            StreamWriter? outputStream = null;
            StreamWriter? debugStream = null;
            StreamWriter? tableStream = null;

            try
            {
                // Validate that an input file was specified

                if (string.IsNullOrEmpty(inputFile))
                {
                    RenderHelpInfo("*** No input grammar file specified.\r\n");
                    return;
                }

                // Get the non-extension part of the input file name

                string fileStem = "output";
                if (!string.IsNullOrEmpty(inputFile))
                    fileStem = Path.GetFileNameWithoutExtension(inputFile);
                if (!string.IsNullOrEmpty(outputFile))
                    fileStem = Path.GetFileNameWithoutExtension(outputFile);
                if (fileStem.EndsWith(".designer", StringComparison.CurrentCultureIgnoreCase))
                    fileStem = Path.GetFileNameWithoutExtension(fileStem);

                // Connect the input and output files

                if (!string.IsNullOrEmpty(inputFile))
                    inputStream = new StreamReader(inputFile);
                if (!string.IsNullOrEmpty(outputFile))
                    outputStream = new StreamWriter(outputFile, false);
                else
                    outputStream = new StreamWriter($"{fileStem}.designer.cs", false);

                // Look to see if a debug output file is required

                if (debugOutput)
                    debugStream = new StreamWriter($"{fileStem}.debug.txt", false);

                // Deal with output of parser
                // tables in human readable form

                if (dumpTables)
                    tableStream = new StreamWriter($"{fileStem}.tables.txt", false);

                // Build the parser and associated objects

                List<string> extRefs;
                string errResult;
                if (fsm)
                    errResult = FSMFactory.CreateOfflineStateMachine
                        (inputStream, outputStream, errorRecovery, out extRefs);
                else
                {
                    errResult = ParserFactory.CreateOfflineParser
                    (
                        inputStream, outputStream, tableStream,
                        debugStream, compressTables, errorRecovery,
                        glrParser, out extRefs
                    );

                    // ParseLR is only used to create offline parsers for
                    // later compilation as part of another project. As a
                    // result, its grammars should not contain assembly
                    // references in the options section.

                    if (extRefs != null && extRefs.Count > 0)
                        errResult += "Offline grammar contains assembly references in the options section.\r\n";
                }
                Console.WriteLine(errResult);
            }
            finally
            {
                // Ensure all input and output files are closed,
                // whether there was an exception thrown or not

                outputStream?.Close();
                inputStream?.Close();
                debugStream?.Close();
                tableStream?.Close();
            }
        }

        /// <summary>
        /// Parse the command line arguments
        /// </summary>
        /// <param name="args">The array of command line argument strings</param>
        /// <returns>True fo parsed successfully, false if badly formed</returns>

        private static bool ParseArguments(string[] args)
        {
            foreach (string arg in args)
            {
                if (arg[0] == '-' || arg[0] == '/')
                {
                    // Handle command line arguments

                    for (int i = 1; i < arg.Length; i++)
                    {
                        switch (arg[i])
                        {
                            // Create a Generalised Canonical LR(1) parser. This is
                            // the most flexible, complex parser, but is quite a bit
                            // slower than the usual LR(1) parser.

                            case 'g':
                                glrParser = true;
                                break;

                            // Create a simple FSM rather than a full-blown parser

                            case 'f':
                                fsm = true;
                                break;

                            // Write a file containing a description of the
                            // LR(1) parser table structures

                            case 't':
                                dumpTables = true;
                                break;

                            // Enable debug logging. Only useful when debugging
                            // the parser itself, or when learning how parsers work

                            case 'd':
                                debugOutput = true;
                                break;

                            // Enable parser table compression. This compression does
                            // not reduce to LALR(1) from the default canonical LR(1)
                            // parser tables. Instead it uses a partial compression
                            // algorithm as suggested by David Pager, that does not
                            // introduce any spurious reduce/reduce conflicts in the
                            // way that LALR(1) parsers can sometimes do.

                            case 'p':
                                compressTables = true;
                                break;

                            // Use error recovery similar to yacc or bison, where
                            // the stack is popped until an error token can be
                            // parsed, then the input stream is flushed until
                            // the next token matches the token after the error
                            // keyword in the grammar.

                            case 'r':
                                errorRecovery = true;
                                break;

                            // Display command line help information

                            case 'h':
                            case '?':
                                RenderHelpInfo(string.Empty);
                                return false;

                            default:
                                RenderHelpInfo($"*** Unrecognised command line option: -{arg[i]}.\r\n");
                                return false;
                        }
                    }
                }
                else if (arg.EndsWith(".g", StringComparison.CurrentCultureIgnoreCase))
                {
                    // Capture the input grammar file. files ending with the
                    // letter g are assumed to be grammar input files

                    if (File.Exists(arg))
                        inputFile = arg;
                    else
                    {
                        RenderHelpInfo($"*** Cannot find input grammar file {arg}.\r\n");
                        return false;
                    }
                }
                else if (arg.EndsWith(".cs", StringComparison.CurrentCultureIgnoreCase))
                {
                    // The output from the parser generator is a C# source file

                    outputFile = arg;
                }
            }
            return true;
        }

        /// <summary>
        /// Display help information messages on the console window
        /// </summary>

        private static void RenderHelpInfo(string errMessage)
        {
            AssemblyName aName = Assembly.GetEntryAssembly().GetName();
            Version ver = aName.Version;
            Console.WriteLine
            (
                $"PARSELR: A C# Canonical LR Parser Generator, ver. {ver}\r\n" +
                errMessage +
                "Usage: parselr [-tdprgfh?] input.g [output.cs]\r\n" +
                "    [-t]    Generate parser table data file\r\n" +
                "            in a '.tables.txt' file. Also\r\n" +
                "            causes state description strings\r\n" +
                "            to be more detailed in state table.\r\n" +
                "    [-d]    Generate debug output to a file\r\n" +
                "            showing how the parser worked, the\r\n" +
                "            file having a '.debug.txt' name.\r\n" +
                "    [-p]    Compress the parser table by removing\r\n" +
                "            unnecessary duplicate states.\r\n" +
                "    [-r]    Use yacc/bison style error recovery\r\n" +
                "            when parsing the grammar. Without\r\n" +
                "            this flag, parsing stops at the first\r\n" +
                "            error encountered.\r\n" +
                "    [-g]    Expects an ambiguous grammar for which\r\n" +
                "            a generalised LR parser will be produced.\r\n" +
                "            In this case -p and -r flags are ignored,\r\n" +
                "            and shift/reduce and reduce/reduce\r\n" +
                "            conflicts are not reported as errors.\r\n" +
                "    [-f]    Expects a grammar for a simple state\r\n" +
                "            machine, rather than an LR parser. In\r\n" +
                "            this case -p and -r flags are ignored.\r\n" +
                "    [-h or -?]\r\n" +
                "            Generate this help information.\r\n" +
                "    input.g\r\n" +
                "            The input grammar file to be parsed.\r\n" +
                "    [output.cs]\r\n" +
                "            The optional file to which the C#\r\n" +
                "            state tables are written. Omission\r\n" +
                "            will result in a default name based\r\n" +
                "            on the input grammar file name. If\r\n" +
                "            the input file name was 'input.g',\r\n" +
                "            the  output file name will be\r\n" +
                "            'input.designer.cs'."
            );
        }
    }
}
