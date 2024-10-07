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

namespace Parsing
{
    /// <summary>
    /// The expected interface that each successive
    /// input token read from the token input stream
    /// exposes
    /// </summary>

    public interface IToken
    {
        /// <summary>
        /// The weakly-typed representation of the type of token,
        /// being a member of the list of token types retrieved
        /// from the parser's Tokens two way map by name lookup.
        /// </summary>

        int Type
        {
            get;
        }

        /// <summary>
        /// Data captured by the tokeniser that
        /// accompanies the input token type. For
        /// example, a token type might be
        /// INTEGER, and the Value property would
        /// be the parsed value of that integer.
        /// </summary>

        object Value
        {
            get;
            set;
        }

        /// <summary>
        /// Positional information for where the token came from.
        /// Could be the line number from which the token came,
        /// or a token number in a sequence.
        /// </summary>

        string Position
        {
            get;
        }

        /// <summary>
        /// Produce a string representation of the token for debugging
        /// </summary>
        /// <param name="tokenMap">The lookup table for tokens</param>
        /// <param name="showValue">True to include the token value
        /// in the string, false to only show the type</param>
        /// <param name="indent">Indent spacing for resulting string</param>
        /// <returns>String representation of the token</returns>

        string AsString(TwoWayMap<string, int> tokenMap, bool showValue, int indent);
    }

    /// <summary>
    /// A default implementation of the IToken interface,
    /// as used for tokens with a precomputed value.
    /// </summary>
    /// <remarks>
    /// Constructor
    /// </remarks>
    /// <param name="tokenType">Parser token type as an integer</param>
    /// <param name="val">Value associated with token</param>
    /// <param name="pos">The location in the input stream
    /// at which the token appeared</param>

    public class ParserToken(int tokenType, object val, string pos) : IToken
    {
        /// <summary>
        /// The weak type for the token. May well
        /// be represented by an enum, but used as
        /// an integer because we may want to
        /// create and run a parser in the same
        /// application, where we can't just
        /// define a new enum at runtime.
        /// </summary>

        public int Type
        {
            get;
            private set;
        } = tokenType;

        /// <summary>
        /// Data captured by the tokeniser that
        /// accompanies the input token type. For
        /// example, a token type might be
        /// INTEGER, and the Value property would
        /// be the parsed value of that integer.
        /// </summary>

        public object Value
        {
            get;
            set;
        } = val;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="tokenType">Parser token type as an integer</param>
        /// <param name="val">Value associated with token</param>
        /// <param name="line">The line of the input stream
        /// on which the token appeared</param>

        public ParserToken(int tokenType, object val, int line)
            : this(tokenType, val, line.ToString())
        {
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="tokenType">Parser token type as an integer</param>
        /// <param name="val">Value associated with token</param>

        public ParserToken(int tokenType, object val)
            : this(tokenType, val, string.Empty)
        {
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="tokenType">Parser token type as an integer</param>

        public ParserToken(int tokenType)
            : this(tokenType, null)
        {
        }

        /// <summary>
        /// The line of the input file where the
        /// token came from
        /// </summary>

        public string Position
        {
            get;
            private set;
        } = pos;

        /// <summary>
        /// Produce a string representation of the token for debugging
        /// </summary>
        /// <param name="tokenMap">The lookup table for tokens</param>
        /// <param name="showValue">True to include the token value
        /// in the string, false to only show the type</param>
        /// <param name="indent">Indent spacing for resulting string</param>
        /// <returns>String representation of the token</returns>

        public string AsString(TwoWayMap<string, int> tokenMap, bool showValue, int indent)
        {
            if (tokenMap == null)
                throw new ArgumentNullException(nameof(tokenMap));
            string result = string.Empty;
            for (int i = 0; i < indent; i++)
                result += "  ";
            result += tokenMap[Type];
            if (showValue && Value != null)
                result += "[" + Value.ToString() + "]";
            return result;
        }
    }
}
