﻿using System.IO;
using Parsing;

namespace CalculatorDemo
{
    /// <summary>
    /// The user half of the parser. The other half
    /// is auto-generated by ParseLR.exe, and placed
    /// into Calculator.Designer.cs.
    /// </summary>

    public class GLRCalculator : GeneralisedParser
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
        /// Get the debug output from the parser
        /// </summary>

        public static string DebugResults => debugResults.ToString();

        /// <summary>
        /// Does the expanded rule beneach the non-terminal token
        /// consist of an expression OP expression rule?
        /// </summary>
        /// <param name="ntt">The non terminal token
        /// to the left of the expansion to be tested</param>
        /// <returns>True if expression is of type sought</returns>

        public bool IsExprOpExpr(NonterminalToken ntt)
        {
            IToken[] tokens = ntt.Children;
            return
                tokens.Length == 3
                && tokens[0].Type == ParserTable.Tokens["expression"]
                && tokens[2].Type == ParserTable.Tokens["expression"]
                && PrecedenceOf(tokens[1].Type) >= 0;
        }

        /// <summary>
        /// Return an integer value to represent relative
        /// precedence of the arithmetic operators
        /// </summary>
        /// <param name="tokenType">The token to inspect</param>
        /// <returns>Zero or positive integer if an arithmetic operator.
        /// -1 if not an operator.</returns>

        public int PrecedenceOf(int tokenType)
        {
            if (tokenType < ParserTable.Tokens["PLUS"]
                || tokenType > ParserTable.Tokens["POWER"])
                return -1;
            else
                return tokenType - ParserTable.Tokens["PLUS"];
        }

        /// <summary>
        /// Compare two input tokens to see which has the
        /// higher arithmetic operator precedence.
        /// </summary>
        /// <param name="l">Token that must have higher
        /// precedence for the function to return true.</param>
        /// <param name="r">If this token has higher precedence,
        /// the function will return false.</param>
        /// <returns>True if l has higher precedence than r</returns>

        public bool HigherPrecedence(IToken l, IToken r) => PrecedenceOf(l.Type) > PrecedenceOf(r.Type);

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
            // we use the CreateInstance() method to load
            // an instance of the GLR calculator parser.

            GLRCalculator calc = ParserFactory<GLRCalculator>.CreateInstance();

            calc.DebugStream = debugResults;
            calc.ErrStream = errorResults;
            CalculatorTokeniser tokeniser
                = new (input, calc.Tokens);
            bool success = calc.Parse(tokeniser);

            if (success && calc.ParserResults.Length == 1)
                return calc.ParserResults[0].Value.ToString();
            else
                return "Error";
        }
    }
}
