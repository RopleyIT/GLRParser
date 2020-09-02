using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Parsing;

namespace CalculatorDemo
{
    /// <summary>
    /// Tokeniser for simple arithmetic expression strings
    /// </summary>

    public class CalculatorTokeniser : IEnumerable<IToken>
    {
        // Holds the list of tokens, which are pulled from the
        // input stream as soon as the tokeniser is constructed

        private readonly List<IToken> tokenStream;

        // Reference to the lookup table of token names against their
        // integer token values. This is usually constructed from the
        // input grammar and held by the parser.

        private readonly TwoWayMap<string, int> tokenValue;
        private readonly string input;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="exprString">The calculator expression
        /// to be parsed</param>

        public CalculatorTokeniser(string exprString, TwoWayMap<string, int> terminals)
        {
            // Capture the parser's view of what the values
            // are for each of the terminal token names

            if (terminals != null)
                tokenValue = terminals;
            else
                throw new ArgumentNullException
                    (nameof(terminals), "No terminal name/value lookup provided");

            // Make sure there is an expression string to parse

            if (string.IsNullOrEmpty(exprString))
                throw new ArgumentException("No expression string to parse");
            else
                input = exprString;

            // Convert the input string into a list of tokens

            tokenStream = new List<IToken>();
            IToken tok;
            while ((tok = NextToken()).Type != tokenValue["EOF"])
                tokenStream.Add(tok);

            // Put the end of stream token on the end

            tokenStream.Add(tok);

        }

        // Tracks how far through the input string we have got

        private int nextCharPos = 0;

        /// <summary>
        /// Return the next input token from the source
        /// </summary>
        /// <returns>The next token in sequence</returns>

        private IToken NextToken()
        {
            // Skip white space

            EatPattern(@"[\t ]*");

            // Check for end of input

            if (EndOfInput())
                return new ParserToken(tokenValue["EOF"]);

            // Look for number tokens. Note that
            // the number is returned as a string of digits,
            // so that leading zeros after a decimal point
            // can have their significance captured within
            // the parser itself.

            IToken found;
            if ((found = TryToken("[0-9]+", "NUMBER")) != null)
                return found;

            // Look for single character operators

            if (EatChar('('))
                return new ParserToken(tokenValue["LPAREN"]);

            if (EatChar(')'))
                return new ParserToken(tokenValue["RPAREN"]);

            if (EatChar('+'))
                return new ParserToken(tokenValue["PLUS"]);

            if (EatChar('-'))
                return new ParserToken(tokenValue["MINUS"]);

            if (EatChar('*'))
                return new ParserToken(tokenValue["TIMES"]);

            if (EatChar('/'))
                return new ParserToken(tokenValue["DIVIDE"]);

            if (EatChar('^'))
                return new ParserToken(tokenValue["POWER"]);

            if (EatChar('.'))
                return new ParserToken(tokenValue["PERIOD"]);

            if (EatChar('E') || EatChar('e'))
                return new ParserToken(tokenValue["EXPONENT"]);

            // Unrecognised token

            throw new InvalidOperationException("Unrecognised character in input");
        }

        /// <summary>
        /// Are we at the end of the input stream?
        /// </summary>
        /// <returns>Whether we are at the end of the input stream</returns>

        private bool EndOfInput() => nextCharPos >= input.Length;

        /// <summary>
        /// Wrap up the test for a text pattern and the creation of
        /// the specified token type if the pattern has been found
        /// </summary>
        /// <param name="pattern">Regular expression pattern to look for</param>
        /// <param name="tokenType">The type of token to generate</param>
        /// <returns>A parser token if matched, or null if not</returns>

        public IToken TryToken(string pattern, string tokenType)
        {
            string tokenString = EatPattern(pattern);
            if (!string.IsNullOrEmpty(tokenString))
                return new ParserToken(tokenValue[tokenType], tokenString);
            else
                return null;
        }

        /// <summary>
        /// Given a regular expression pattern, if
        /// the head of the input stream matches this
        /// pattern, return the match, adjusting the
        /// front of the stream. If no match is found,
        /// the input stream remains unadjusted.
        /// </summary>
        /// <param name="pat">The string regular expresssion to look for.</param>
        /// <returns>The matched sequence, or empty if no match at 
        /// the head of the string.</returns>

        private string EatPattern(string pat)
        {
            if (!pat.StartsWith("\\G"))
                pat = $"\\G({pat})";
            return EatPattern(new Regex(pat, RegexOptions.Multiline));
        }

        /// <summary>
        /// Given a regular expression pattern, if
        /// the head of the input stream matches this
        /// pattern, return the match, adjusting the
        /// front of the stream. If no match is found,
        /// the input stream remains unadjusted.
        /// </summary>
        /// <param name="re">The regular expression to look for.</param>
        /// <returns>The matched sequence, or empty if no match at 
        /// the head of the string.</returns>

        private string EatPattern(Regex re)
        {
            Match m = re.Match(input, nextCharPos);
            if (m.Success)
            {
                nextCharPos += m.Length;
                return m.Value;
            }
            else
                return string.Empty;
        }

        /// <summary>
        /// Input consumer for single characters, that
        /// consequently runs much faster than regular
        /// expression matching.
        /// </summary>
        /// <param name="c">The character to look for next</param>
        /// <returns>True if found, after advancing input
        /// pointer. False if not found, without altering
        /// input stream.</returns>

        private bool EatChar(char c)
        {
            if (input[nextCharPos] == c)
            {
                nextCharPos++;
                return true;
            }
            else
                return false;
        }

        /// <summary>
        /// Return a strongly-typed enumerator over the
        /// set of tokens in the input stream
        /// </summary>
        /// <returns>The token stream enumerator</returns>

        public IEnumerator<IToken> GetEnumerator() => tokenStream.GetEnumerator();

        /// <summary>
        /// Return an enumerator over the
        /// set of tokens in the input stream
        /// </summary>
        /// <returns>The token stream enumerator</returns>

        IEnumerator System.Collections.IEnumerable.GetEnumerator() => tokenStream.GetEnumerator();
    }
}
