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


namespace Parsing
{
    /// <summary>
    /// For one item set in the parser table, we
    /// have a row. Each column in the row has
    /// a token type that was input, the guard
    /// condition associated with that token type,
    /// and information that tells the parser
    /// what to do next for that token and guard
    /// in that particular state.
    /// </summary>

    public class ParserStateColumn
    {
        /// <summary>
        /// Link back to parser used only for
        /// debugging reporting
        /// </summary>

        public static ParserTable Table
        {
            get;
            set;
        }

        /// <summary>
        /// The recognised input token type
        /// that would cause this transition
        /// to be taken. Could be strongly
        /// typed as ParserTokenType if the
        /// token type also defines the non-
        /// terminals in the token type enum
        /// </summary>

        public int InputToken
        {
            get;
            private set;
        }

        /// <summary>
        /// The guard condition to test for that
        /// is associated with this input token.
        /// Both the input token must match and
        /// the guard condition must be true
        /// when executed, for the transition to
        /// be taken according to this column.
        /// </summary>

        public IGuardEvaluator Condition
        {
            get;
            private set;
        }

        /// <summary>
        /// True if a rule reduction is to be
        /// performed before the shift operation
        /// </summary>

        public bool Reduce => index < 0;

        private readonly short index;

        /// <summary>
        /// Which next state to jump to on a shift,
        /// or which production to reduce by if reduce
        /// </summary>

        public short Index
        {
            get
            {
                if (index < 0)
                    return (short)~index;
                else
                    return index;
            }
        }

        /// <summary>
        /// Constructor for a state table column
        /// </summary>
        /// <param name="desc">A human readable description
        /// of this state column</param>
        /// <param name="tokenType">The observed input token
        /// type, or reduced non-terminal</param>
        /// <param name="guard">A guard condition associated
        /// with an input terminal token</param>
        /// <param name="reduce">True if this is a reduction,
        /// false if a shift</param>
        /// <param name="idx">The production index if a
        /// reduction, or the new state number if a shift</param>

        public ParserStateColumn(int tokenType,
            IGuardEvaluator guard, bool reduce, short idx)
        {
            InputToken = tokenType;
            Condition = guard;
            if (reduce)
                index = (short)(~idx);
            else
                index = idx;
        }

        /// <summary>
        /// Render the state column as a human
        /// readable string.
        /// </summary>
        /// <param name="p">The parser this state column
        /// belongs to, so that the token can be
        /// rendered meaningfully</param>
        /// <returns>A string representation of the
        /// state column</returns>

        public override string ToString()
        {
            string token = InputToken.ToString();
            string transition =
                Index.ToString();

            if (Table != null)
            {
                token = Table.Tokens[InputToken];
                transition = Reduce ?
                    Table.Productions[Index].ToString() :
                    Table.States[Index].ToString();
            }

            string result
                = "On " + token;
            if (Condition != null)
                result += " ["
                + Condition.AsString() + "]";
            if (Reduce)
                result += " reduce by production ";
            else
                result += " shift to state ";
            result += transition;
            return result;
        }
    }
}
