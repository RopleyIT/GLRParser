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
using System.IO;

namespace Parsing
{
    /// <summary>
    /// Indicate the level at which a message should be displayed
    /// </summary>

    public enum MessageLevel
    {
        NONE,       // Silent, no messages at all
        ERROR,      // Errors reported, warnings skipped
        WARN,       // Warnings included in output
        VERBOSE,    // More verbose reporting of parser progress
        DEBUG       // Full (painful) parser reporting
    }

    public interface IParser
    {
        /// <summary>
        /// Used to return a string description of
        /// the type of parser. This is used to
        /// avoid complicated factories and
        /// reflection when validating the type of
        /// parser being created in inline parsing.
        /// Current supported values are "LR1" and "GLR"
        /// </summary>

        string ParserType
        {
            get;
        }

        /// <summary>
        /// Stream used for verbose parser reporting. Use
        /// this to track the parser's progress through
        /// the input grammar. Set/leave it as null to
        /// just not report this kind of information.
        /// </summary>

        TextWriter DebugStream
        {
            get;
            set;
        }

        /// <summary>
        /// Stream used for normal parser reporting.
        /// Use this to see error messages output by
        /// the parser. Set/leave it as null to just
        /// not report this kind of information.
        /// </summary>

        TextWriter ErrStream
        {
            get;
            set;
        }

        MessageLevel ErrorLevel
        {
            get;
            set;
        }

        /// <summary>
        /// The set of item sets, the productions,
        /// the token lookup table, and the action
        /// functions associated with the grammar.
        /// </summary>

        ParserTable ParserTable
        {
            get;
            set;
        }

        /// <summary>
        /// Two way lookup of token names and
        /// their integer values
        /// </summary>

        TwoWayMap<string, int> Tokens
        {
            get;
        }

        /// <summary>
        /// Prepare a readable representation of an input token
        /// </summary>
        /// <param name="tok">The token</param>
        /// <returns>A string representation of the token</returns>

        string TokenDescription(IToken tok);

        /// <summary>
        /// Feed one token into the parser, and cause the
        /// parser to advance based on this token. This
        /// can involve zero or more reductions, followed
        /// by a single shift operation.
        /// </summary>
        /// <param name="token">The token to be parsed</param>
        /// <returns>True if the parse advances the parser
        /// successfully</returns>

        bool ParseToken(IToken token);

        /// <summary>
        /// Return true if the parser stack is
        /// empty because the parse is complete
        /// </summary>

        bool ParseComplete
        {
            get;
        }

        /// <summary>
        /// Run the grammar parser over the input enumerable
        /// token stream, and execute the parser state machine
        /// based on the input sequence.
        /// </summary>
        /// <param name="source">The input source from
        /// which to read tokens</param>
        /// <returns>True if the input token sequence
        /// conformed to the state machine rules, 
        /// false if not.</returns>

        bool Parse(IEnumerable<IToken> source);

        /// <summary>
        /// Holds the most recent error object as created
        /// by Warn() or Error() in the most recent
        /// rule reduction action function.
        /// </summary>

        ParserError ActionError
        {
            get;
            set;
        }

        /// <summary>
        /// Flag a non-fatal error identified during a
        /// rule reduction action function in the grammar
        /// </summary>
        /// <param name="errorMessage">
        /// The description of the error</param>

        void Warn(string errorMessage);

        /// <summary>
        /// Flag a Fatal error identified during a
        /// rule reduction action function in the grammar
        /// </summary>
        /// <param name="errorMessage">
        /// The description of the error</param>

        void Error(string errorMessage);

        /// <summary>
        /// Helper method for outputting text to
        /// the debug and error output streams.
        /// Has formatting behaviour compatible
        /// with TextWriter.Write(format, args)
        /// </summary>
        /// <param name="errorLevel">Verbosity level used to write
        /// to the error stream as well as the debug stream</param>
        /// <param name="format">Format argument, obeying
        /// string.Format syntax</param>
        /// <param name="args">Arguments as used by the
        /// format argument</param>

        void MessageWrite(MessageLevel errorLevel, string format, params object[] args);

        /// <summary>
        /// Helper method for outputting text to
        /// the debug and error output streams.
        /// Has formatting behaviour compatible
        /// with TextWriter.WriteLine(format, args)
        /// </summary>
        /// <param name="errorLevel">Verbosity level used to write
        /// to the error stream as well as the debug stream</param>
        /// <param name="format">Format argument, obeying
        /// string.Format syntax</param>
        /// <param name="args">Arguments as used by the
        /// format argument</param>

        void MessageWriteLine(MessageLevel errorLevel, string format, params object[] args);
    }
}
