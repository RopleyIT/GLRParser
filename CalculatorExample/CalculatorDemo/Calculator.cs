﻿using Parsing;
using System.IO;

namespace CalculatorDemo
{
    /// <summary>
    /// The user written part of the parser. The other
    /// is auto-generated by ParseLR.exe, and placed
    /// into Calculator.Designer.cs. The auto-generated
    /// part is actually a derived class from Calculator,
    /// in the namespace CalculatorDemo.AutoGenerated
    /// with class name CalculatorDemo_AutoGenerated.
    /// </summary>

    public class Calculator : Parser
    {
        /// <summary>
        /// Where the overall result of the
        /// calculated expression is stored.
        /// Alternatively filled in with the
        /// error information if badly formed.
        /// </summary>

        public string Result
        {
            get;
            protected set;
        }

        private static StringWriter errorResults;

        /// <summary>
        /// Get the error output from the parser
        /// </summary>

        public static string ErrorResults => errorResults.ToString();

        private static StringWriter debugResults;

        /// <summary>
        /// Verbose output from parser
        /// </summary>

        public static string DebugResults => debugResults.ToString();

        /// <summary>
        /// Given an arithmetic expression, parse it
        /// and compute the result.
        /// </summary>
        /// <param name="input">The expression string</param>
        /// <returns>A string representation of the result</returns>

        public static string Calculate(string input)
        {
            errorResults = new StringWriter();
            debugResults = new StringWriter();

            // Since the source code for the parser was
            // compiled offline and added to this project,
            // we just use CreateInstance() method of the
            // ParserFactory<T> class to make it create a
            // first instance of the calculator parser.

            Calculator calc = ParserFactory<Calculator>.CreateInstance();

            calc.DebugStream = debugResults;
            calc.ErrStream = errorResults;
            CalculatorTokeniser tokeniser
                = new(input, calc.Tokens);
            bool success = calc.Parse(tokeniser);

            if (success)
                return calc.Result;
            else
                return "Error";
        }
    }
}
