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


namespace ParserGenerator
{

    /// <summary>
    /// Representation of one shift, reduction
    /// or goto associated with one state
    /// in a parser. Each state/itemset has
    /// a list of these, representing an
    /// enforced priority order for the set
    /// of transitions, so that ambiguous or
    /// overlapping grammar elements (overlapped
    /// guard conditions on the same token)
    /// are checked for in the correct order.
    /// </summary>

    public class GrammarTransition
    {
        /// <summary>
        /// Compute the Hamming weight of the Element in this
        /// Grammar transition.
        /// </summary>
        /// <returns>If the element's guard condition is
        /// non-null, a value between 0 and 0x4000000000000000
        /// representing the proportion of the output truth table
        /// that contains 1 values rather than 0 values. If the
        /// guard condition on the element is null, this means
        /// that the implied guard condition is always true
        /// for all inputs. Hence the return value is the
        /// maximum possible, at 0x4000000000000000.</returns>

        public long HammingWeight() => Element.HammingWeight();

        /// <summary>
        /// Simplified constructor for LR(1)
        /// single transition per element
        /// parsers
        /// </summary>
        /// <param name="elem">The token and guard condition</param>
        /// <param name="shift">The next item set to shift to</param>

        public GrammarTransition
        (
            GrammarElement elem,
            GrammarItemSet shift
        )
        {
            Element = elem;
            ShiftOrGoto = shift;
            Reduction = null;
        }

        /// <summary>
        /// Simplified constructor for LR(1)
        /// single transition per element
        /// parsers
        /// </summary>
        /// <param name="elem">The token and guard condition</param>
        /// <param name="reduction">The production by which we reduce</param>

        public GrammarTransition
        (
            GrammarElement elem,
            GrammarProduction reduction
        )
        {
            Element = elem;
            ShiftOrGoto = null;
            Reduction = reduction;
        }

        /// <summary>
        /// The token and guard condition
        /// this grammar transition looks for
        /// </summary>

        public GrammarElement Element
        {
            get;
            private set;
        }

        /// <summary>
        /// The destination state that a shift
        /// of this element would go to. This
        /// is set to null if the transition
        /// is a reduction.
        /// </summary>

        public GrammarItemSet ShiftOrGoto
        {
            get;
            private set;
        }

        /// <summary>
        /// The production by which the stack would
        /// be reduced if this input element were
        /// recognised at this point in the input
        /// event/token stream.
        /// </summary>

        public GrammarProduction Reduction
        {
            get;
            private set;
        }

        /// <summary>
        /// Test the type of the transition. A shift is used when an
        /// input token and guard match the next token/guard along
        /// the current grammar rule we are parsing. If there is a
        /// match, the parser just shifts over it, and jumps to the
        /// state that represents that token having been seen in the
        /// current rule.
        /// </summary>

        public bool IsShift => ShiftOrGoto != null
                    && Element.Token.TokenType == TokenType.Terminal;

        /// <summary>
        /// Test the type of the transition. A goto is used after a
        /// reduction has removed several tokens from the parser
        /// stack. The uncovered state will have a list of non-terminal
        /// tokens that are allowed to appear to the right of the
        /// current rule position. The LHS of the production that has
        /// been reduced is a non-terminal, and it is this that is
        /// looked up in the list of goto tokens to decide which
        /// state to jump to next.
        /// </summary>

        public bool IsGoto => ShiftOrGoto != null
                    && Element.Token.TokenType == TokenType.Nonterminal;

        /// <summary>
        /// Test the type of the transition. A reduction transition
        /// is used when all the tokens in a rule have been seen, so the
        /// list of RHS tokens in the rule can be popped, and a single
        /// non-terminal LHS token from the rule pushed.
        /// </summary>

        public bool IsReduction => Reduction != null;

        /// <summary>
        /// Render the object as a string. Defaults to the
        /// non-verbose rendering
        /// </summary>
        /// <returns>The object rendered as a string</returns>

        public override string ToString() => ToString(false);

        /// <summary>
        /// Render the transition as a human readable string
        /// </summary>
        /// <param name="verbose">True to render with as
        /// much detail as possible, false for an
        /// abbreviated version</param>
        /// <returns>The rendered string</returns>

        public string ToString(bool verbose)
        {
            string result = $"On: {Element}\r\n";
            if (IsReduction)
                result += $"REDUCE by: {Reduction.ToString(-1)}";
            else if (IsShift)
                result += $"SHIFT to: {ShiftOrGoto.ToString(verbose)}";
            else
                result += $"GOTO: {ShiftOrGoto.ToString(verbose)}";
            return result;
        }
    }
}
