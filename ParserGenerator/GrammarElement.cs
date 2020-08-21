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

using BooleanLib;

namespace ParserGenerator
{
    /// <summary>
    /// Represents one element in the RHS of a production
    /// </summary>

    public class GrammarElement
    {
        /// <summary>
        /// The terminal or non-terminal token at this point
        /// in the sequence of the production
        /// </summary>

        public GrammarToken Token
        {
            get;
            set;
        }

        /// <summary>
        /// A guard condition set on this element, if
        /// in grammar. If no guard, should be null.
        /// </summary>

        public BoolExpr Guard
        {
            get;
            set;
        }

        /// <summary>
        /// Constructor for a new element
        /// </summary>
        /// <param name="tok">The token in this element</param>
        /// <param name="guard">Any guard condition associated with the element. Set
        /// to null if no guard applies.</param>

        public GrammarElement(GrammarToken tok, BoolExpr guard)
        {
            Token = tok;
            Guard = guard;
        }

        /// <summary>
        /// Ensure that identical elements get identical hash codes.
        /// </summary>
        /// <returns>The hash value for the whole element</returns>

        public override int GetHashCode() => Token.GetHashCode();

        /// <summary>
        /// Implementation of by value comparison. Unused, as the
        /// operator == overload has been optimised not to use it.
        /// </summary>
        /// <param name="obj">The other object to compare against</param>
        /// <returns>True if same value</returns>

        public override bool Equals(object obj)
        {
            if (!(obj is GrammarElement ge))
                return false;

            return ge.Token == Token
                && ge.Guard == Guard;
        }

        /// <summary>
        /// Reimplement operator == to compare by value
        /// </summary>
        /// <param name="l">Left operand</param>
        /// <param name="r">Right operand</param>
        /// <returns>True if both null, or both have same value</returns>

        public static bool operator ==(GrammarElement l, GrammarElement r)
        {
            // Obviously equal if same instance

            if (l == (object)r)
                return true;

            // Deal with one or other being null

            if (l is null || r is null)
                return false;

            // Compare value fields

            return l.Token == r.Token
                && l.Guard == r.Guard;
        }

        /// <summary>
        /// Operator != implemented as complement
        /// of operator ==.
        /// </summary>
        /// <param name="l">Left operand</param>
        /// <param name="r">Right operand</param>
        /// <returns>True if not equal or not both null</returns>

        public static bool operator !=(GrammarElement l, GrammarElement r) => !(l == r);

        /// <summary>
        /// Compute the Hamming weight of the element.
        /// </summary>
        /// <returns>A value between 0 and 0x4000000000000000
        /// representing the proportion of the output truth table
        /// that contains 1 values rather than 0 values. If the
        /// guard condition on the element is null, this means
        /// that the implied guard condition is always true
        /// for all inputs. Hence the return value is the
        /// maximum possible, at 0x4000000000000000.</returns>

        public long HammingWeight() => Guard?.HammingWeight() ?? 0x4000000000000000L;

        /// <summary>
        /// Return a meaningful identifier name for the grammar element
        /// </summary>
        /// <param name="indexProvider">The leaf index proider
        /// used to track all the leaves of expressions used
        /// in the boolean expressions of this grammar.</param>
        /// <returns>The element name in a form that can be used
        /// as an identifier in the target language</returns>

        public string AsIdentifier(LeafIndexProvider indexProvider)
        {
            // Grab a meaningful string for the token part of the element

            string result = Token.Text;
            if (string.IsNullOrEmpty(result))
                result = "\u03B5";

            // If there is a guard associated with it, attach its
            // identifier extension to the end of the token name

            if (Guard != null)
                result += '_' + Guard.AsIdentifier(indexProvider);
            return result;
        }

        /// <summary>
        ///  Produce a string representation of the element. The
        ///  Greek epsilon character represents the empty token.
        /// </summary>
        /// <returns>String representation of the element</returns>

        public override string ToString()
        {
            string tok = Token.Text;
            if (string.IsNullOrEmpty(tok))
                tok = "\u03B5";

            if (Guard != null)
                return $"{tok}[{Guard}]";
            else
                return tok;
        }
    }
}
