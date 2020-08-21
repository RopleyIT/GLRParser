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
using System.Collections.Generic;
using System.Text;

namespace ParserGenerator
{
    /// <summary>
    /// Captures one grammar rule within an input grammar
    /// </summary>

    public class GrammarProduction
    {
        /// <summary>
        /// The parent grammar that this production belongs to
        /// </summary>

        public Grammar Parent
        {
            get;
            private set;
        }

        /// <summary>
        /// Unique administrative number for this production
        /// </summary>

        public int ProductionNumber
        {
            get;
            set;
        }

        /// <summary>
        /// Used to give a unique low integer number within the set
        /// of productions that have the same LHS. The algorithm is
        /// that the first production in the grammar with this LHS
        /// has a value of zero, while later rules have contiguous
        /// values in order of appearance in the grammar.
        /// </summary>

        private int nonterminalRuleNumber;

        /// <summary>
        /// Unique index for this production, among the
        /// subset of productions that share the same
        /// LHS non-terminal token.
        /// </summary>

        public int NonterminalRuleNumber
        {
            get => nonterminalRuleNumber;
            set
            {
                nonterminalRuleNumber = value;
                if (Code != null && string.IsNullOrEmpty(Code.Name))
                    Code.Name = $"Action_{LHS.Text}_{value}";
            }
        }

        /// <summary>
        /// Since comparing productions is the hot path in the
        /// parser generator, the identity mechanism below is
        /// here to accelerate the comparison of productions
        /// with each other.
        /// </summary>

        private static Dictionary<string, int> productionIdentities;

        private int asId;

        /// <summary>
        /// Return a unique ID number for the production
        /// </summary>

        public int AsId
        {
            get
            {
                if (asId == 0)
                {
                    if (productionIdentities == null)
                        productionIdentities = new Dictionary<string, int>();
                    string idString = AsIdentifier();
                    if (!productionIdentities.TryGetValue(idString, out asId))
                    {
                        asId = productionIdentities.Count + 1;
                        productionIdentities.Add(idString, asId);
                    }
                }
                return asId;
            }
        }

        /// <summary>
        /// The non-terminal token this production is an expansion of
        /// </summary>

        public GrammarToken LHS
        {
            get;
            set;
        }

        /// <summary>
        /// The list of elements that appear
        /// to the right of the grammar rule
        /// </summary>

        public IList<GrammarElement> RHS
        {
            get;
            private set;
        }

        /// <summary>
        /// Guard condition to be carried back to a predecessor
        /// element in a parent reduction. Used only when a
        /// guard condition has been attached to a non-terminal.
        /// </summary>

        public BoolExpr CarryBack
        {
            get;
            set;
        }

        /// <summary>
        /// Optional inline code to be executed
        /// when this token has been parsed
        /// successfully in the grammar
        /// </summary>

        public GrammarGuardOrAction Code
        {
            get;
            private set;
        }

        /// <summary>
        /// Check that no $N argument has a value of N outside the number
        /// of tokens on the right side of the production being parsed
        /// </summary>
        /// <returns>An error message if at fault., The empty string if not.</returns>

        public string ValidateCodeArguments()
        {
            if (Code == null)
                return string.Empty;
            else
                return Code.ValidateCodeArguments(RHS.Count, $"rule {this}");
        }

        /// <summary>
        /// Constructor. Given a non-terminal
        /// for which we wish to create a production,
        /// create an empty grammar production for that
        /// non-terminal token.
        /// </summary>
        /// <param name="prodNum">Unique number for this production instance</param>
        /// <param name="rhs">The list of tokens
        /// for which we are defining a production</param>
        /// <param name="cd">The optional code fragment
        /// to be executed when the rule is reduced</param>
        /// <param name="parent">The grammar that owns this production</param>

        public GrammarProduction(Grammar parent, int prodNum, IList<GrammarElement> rhs, string cd)
            : this(parent, prodNum, rhs)
        {
            if (string.IsNullOrEmpty(cd))
                Code = null;
            else
                Code = new GrammarGuardOrAction(string.Empty, cd);
        }

        /// <summary>
        /// Constructor. Given a non-terminal
        /// for which we wish to create a production,
        /// create an empty grammar production for that
        /// non-terminal token.
        /// </summary>
        /// <param name="prodNum">Unique number for this production instance</param>
        /// <param name="rhs">The list of tokens
        /// for which we are defining a production</param>
        /// <param name="a">A pre-existing grammar action that
        /// this production will share with another production</param>
        /// <param name="parent">The grammar that owns this production</param>

        public GrammarProduction(Grammar parent, int prodNum, IList<GrammarElement> rhs, GrammarGuardOrAction a)
            : this(parent, prodNum, rhs) => Code = a;

        /// <summary>
        /// Constructor. Given a non-terminal
        /// for which we wish to create a production,
        /// create an empty grammar production for that
        /// non-terminal token.
        /// </summary>
        /// <param name="prodNum">Unique number for this production instance</param>
        /// <param name="rhs">The list of tokens
        /// for which we are defining a production</param>
        /// <param name="parent">The grammar that owns this production</param>

        public GrammarProduction(Grammar parent, int prodNum, IList<GrammarElement> rhs)
        {
            Parent = parent;
            ProductionNumber = prodNum;
            LHS = null;
            RHS = rhs;
            Code = null;
            CarryBack = null;
        }

        /// <summary>
        /// Generate a hash value from the list of tokens in the rule
        /// </summary>
        /// <returns>The hash value</returns>

        public override int GetHashCode()
        {

            string key = string.Empty;
            if (LHS != null)
                key = LHS.Text;
            foreach (GrammarElement ge in RHS)
                key += ge.Token.Text;
            return GrammarToken.HashFromString(key);
        }

        /// <summary>
        /// Implementation of by value comparison. Note this function
        /// is never used, as operator == has been overridden to
        /// execute a much faster implementation of equality. It is
        /// included here merely to make compiler warnings go away.
        /// </summary>
        /// <param name="obj">The other object to compare against</param>
        /// <returns>True if same value</returns>

        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;

            // Not the same if the argument is null but
            // the current instance is not.

            GrammarProduction gp = obj as GrammarProduction;

            // Different LHS tokens are a mismatch

            if (gp.LHS != LHS)
                return false;

            // Different length productions are a mismatch

            if (gp.RHS.Count != RHS.Count)
                return false;

            // Different elements in right hand sides
            // represent a mismatch

            for (int i = 0; i < RHS.Count; i++)
                if (RHS[i] != gp.RHS[i])
                    return false;

            // We don't bother to check the code fragment
            // as this is not really part of the production

            return true;
        }

        /// <summary>
        /// Reimplement operator == to compare by value
        /// </summary>
        /// <param name="l">Left operand</param>
        /// <param name="r">Right operand</param>
        /// <returns>True if both null, or both have same value</returns>

        public static bool operator ==(GrammarProduction l, GrammarProduction r)
        {
            // Obviously equal if same instance. The casts remove
            // any potential overloads of operator ==

            if (l == (object)r)
                return true;

            // Deal with one or other being null

            if (l is null || r is null)
                return false;

            // Compare value fields

            return l.AsId == r.AsId;
        }

        /// <summary>
        /// Operator != implemented as complement
        /// of operator ==.
        /// </summary>
        /// <param name="l">Left operand</param>
        /// <param name="r">Right operand</param>
        /// <returns>True if not equal or not both null</returns>

        public static bool operator !=(GrammarProduction l, GrammarProduction r) => !(l == r);

        /// <summary>
        /// Render the production with no
        /// position marker
        /// </summary>
        /// <returns>A string representation of the production</returns>

        public override string ToString() => ToString(-1);

        private string asIdentifier;

        /// <summary>
        /// Generate a unique unambiguous string that defines
        /// this grammar production. Can be used to make a
        /// quick comparision between productions to see if
        /// they are identical.
        /// </summary>
        /// <param name="g">The grammar object this
        /// production is being generated in.</param>
        /// <returns>A string description of this
        /// grammar production. Would be identical
        /// only for two grammar productions that have
        /// the same LHS token and the same list of
        /// RHS grammar elements.</returns>

        public string AsIdentifier()
        {
            if (asIdentifier == null)
            {
                StringBuilder sb = new StringBuilder();
                if (LHS != null)
                    sb.Append(LHS.Text);
                else
                    sb.Append("(null)");
                sb.Append(':');
                foreach (GrammarElement tok in RHS)
                {
                    sb.Append(' ');
                    sb.Append(tok.AsIdentifier(Parent.BooleanExpressionCache.IndexProvider));
                }

                // Ensure we only have to compare references hereafter

                asIdentifier = string.Intern(sb.ToString());
            }
            return asIdentifier;
        }

        /// <summary>
        /// Produce a string representation of the rule
        /// </summary>
        /// <param name="pos">A notional position
        /// withint the production that the parser
        /// may have reached.</param>
        /// <returns>The string representation of the rule</returns>

        public string ToString(int pos)
        {
            StringBuilder sb = new StringBuilder();
            if (LHS != null)
                sb.Append(LHS.Text);
            else
                sb.Append("(null)");
            sb.Append(':');
            int currPos = 0;
            foreach (GrammarElement tok in RHS)
            {
                if (currPos++ == pos)
                    sb.Append(" ^ ");
                else
                    sb.Append(' ');
                sb.Append(tok.ToString());
            }
            if (currPos == pos)
                sb.Append(" ^");

            return sb.ToString();
        }
    }
}
