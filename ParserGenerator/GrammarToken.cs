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

namespace ParserGenerator
{
    /// <summary>
    /// Multiplicity on tokens
    /// in a grammar input file.
    /// </summary>

    public enum Multiplicity
    {
        ExactlyOne,
        ZeroOrOne,
        OneToMany,
        ZeroToMany
    }

    /// <summary>
    /// Type of token, determining
    /// its role within a grammar.
    /// </summary>

    public enum TokenType
    {
        Terminal,
        Nonterminal,
        Guard
    }

    /// <summary>
    /// Represents a terminal or non-terminal token
    /// captured from the input grammar
    /// </summary>

    public class GrammarToken
    {
        /// <summary>
        /// Describes the role this token plays
        /// in the grammar. A Terminal is an
        /// indivisible token as returned from
        /// the input tokeniser. A Nonterminal
        /// is a token that appears to the left
        /// of a production in the grammar. A
        /// Guard is the name of some element
        /// that returns a boolean when called.
        /// A Code token is an element containing
        /// verbatim code to be pasted into the
        /// parser for calling when a rule is
        /// recognised and reduced.
        /// </summary>

        public TokenType TokenType
        {
            get;
            private set;
        }

        /// <summary>
        /// The name of the token, or in the
        /// case of a code token, the actual
        /// code itself.
        /// </summary>

        public string Text
        {
            get;
            private set;
        }

        private string baseType;

        /// <summary>
        /// The string representation of the data
        /// type a token's Value property holds
        /// </summary>

        public string ValueType
        {
            get
            {
                string mType = "object";
                if (baseToken != null && !string.IsNullOrEmpty(baseToken.ValueType))
                    mType = baseToken.ValueType;

                return Multiplicity switch
                {
                    ParserGenerator.Multiplicity.ZeroOrOne => $"IOptional<{mType}>",
                    ParserGenerator.Multiplicity.ZeroToMany or ParserGenerator.Multiplicity.OneToMany => $"IList<{mType}>",
                    // Exactly one
                    _ => baseType,
                };
            }
            set
            {
                SetMultiplicity(Multiplicity.ExactlyOne, null);
                baseType = value;
            }
        }

        /// <summary>
        /// If this is a multiplicity, return the type of the
        /// object this is a multiplicity of. If it is not,
        /// return the type of the current token.
        /// </summary>

        public string BaseType
        {
            get
            {
                if (baseToken != null)
                    return baseToken.ValueType;
                else
                    return baseType;
            }
        }

        /// <summary>
        /// The integer code associated with the token
        /// </summary>

        public int TokenNumber
        {
            get;
            private set;
        }

        // When the multiplicity is not exactly one,
        // this is the other token we are a multiple of.
        // This is used so that the correct data types
        // can be obtained for the grammar output
        // of action functions.

        private GrammarToken baseToken;
        private Multiplicity multiplicity;

        /// <summary>
        /// Whether this token identifies an element
        /// that is followed by '?', '*' or '+'.
        /// </summary>

        public Multiplicity Multiplicity => multiplicity;

        /// <summary>
        /// Change the multiplicity of this token. If set to
        /// anything other than the default Multiplicity.ExactlyOne,
        /// another token this is a multiplicity of must be provided.
        /// </summary>
        /// <param name="m">The multiplicity</param>
        /// <param name="tok">The token this is a multiplicity of</param>

        public void SetMultiplicity(Multiplicity m, GrammarToken tok)
        {
            if (m == Multiplicity.ExactlyOne && tok != null
                || m != Multiplicity.ExactlyOne && tok == null)
                throw new ArgumentException("SetMultiplicity has inconsistent parameters");

            if (m == Multiplicity.ExactlyOne)
                baseToken = null;
            else
                baseToken = tok;
            multiplicity = m;
        }

        /// <summary>
        /// Grammar tokens are immutable. Hence
        /// once constructed their properties
        /// hold constant values.
        /// </summary>
        /// <param name="tokNum">The unique number for the token</param>
        /// <param name="tokType">Whether the token being
        /// created is terminal or non-terminal</param>
        /// <param name="text">The name for the token, or
        /// the code fragment if the token type is Code</param>

        public GrammarToken(int tokNum, TokenType tokType, string text)
            : this(tokNum, tokType, text, string.Empty)
        { }

        /// <summary>
        /// Grammar tokens are immutable. Hence
        /// once constructed their properties
        /// hold constant values.
        /// </summary>
        /// <param name="tokNum">The unique number for the token</param>
        /// <param name="tokType">Whether the token being
        /// created is terminal or non-terminal</param>
        /// <param name="text">The name for the token, or
        /// the code fragment if the token type is Code</param>
        /// <param name="valType">The string representation
        /// of the data type held in the Value property
        /// of an IToken</param>

        public GrammarToken(int tokNum, TokenType tokType, string text, string valType)
        {
            TokenType = tokType;
            Text = text;
            TokenNumber = tokNum;
            ValueType = valType;
        }

        /// <summary>
        /// Ensure the same hash code appears for tokens considered identical
        /// </summary>
        /// <returns>The hash code for the token</returns>

        public override int GetHashCode()
        {
            string hashStr = TokenType == TokenType.Terminal ? "T" : "N";
            hashStr += Text;
            return HashFromString(hashStr);
        }

        /// <summary>
        /// Create a reasonable hash value from a string
        /// </summary>
        /// <param name="s">The input string</param>
        /// <returns>The computed hash value</returns>

        public static int HashFromString(string s)
        {
            int hash = 0;
            if (s != null)
                foreach (char c in s)
                    hash = (c & 0xFF) + (hash << 6) + (hash << 16) - hash;
            return hash;
        }

        /// <summary>
        /// Implementation of by value comparison. Unused, since
        /// operator == optimised to not call this method.
        /// </summary>
        /// <param name="obj">The other object to compare against</param>
        /// <returns>True if same value</returns>

        public override bool Equals(object obj)
        {
            if (obj is not GrammarToken gt)
                return false;

            return gt.TokenType == TokenType
                && gt.Text == Text;
        }

        /// <summary>
        /// Reimplement operator == to compare by value
        /// </summary>
        /// <param name="l">Left operand</param>
        /// <param name="r">Right operand</param>
        /// <returns>True if both null, or both have same value</returns>

        public static bool operator ==(GrammarToken l, GrammarToken r)
        {
            // Obviously equal if same instance

            if (l == (object)r)
                return true;

            // Deal with one or other being null

            if (l is null || r is null)
                return false;

            // Compare value fields

            return l.TokenType == r.TokenType
                && l.Text == r.Text;
        }

        /// <summary>
        /// Operator != implemented as complement
        /// of operator ==.
        /// </summary>
        /// <param name="l">Left operand</param>
        /// <param name="r">Right operand</param>
        /// <returns>True if not equal or not both null</returns>

        public static bool operator !=(GrammarToken l, GrammarToken r) => !(l == r);

        /// <summary>
        /// Produce a string representation of the token
        /// </summary>
        /// <returns>String representation of the token</returns>

        public override string ToString()
        {
            string valueType = string.Empty;
            if (!string.IsNullOrEmpty(ValueType))
                valueType = $"<{ValueType}>";
            return $"{TokenType} ({Text}{valueType})";
        }
    }
}
