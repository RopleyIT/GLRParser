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
using System.Linq;
using System.Text;

namespace ParserGenerator
{
    /// <summary>
    /// Represents the set of all grammar
    /// items that could each be where we have
    /// got to while parsing an input token stream.
    /// For example, the items X: A B ^ C; and
    /// Y: A B ^ k L; both represent the fact that
    /// we have so far parsed an A then a B, but
    /// have yet to decide which way to go based
    /// on the next input token that arrives. (The
    /// underscore represents where we have got
    /// to so far in the parse of the grammar.)
    /// </summary>

    public class GrammarItemSet
    {
        /// <summary>
        /// Unique administrative number for this item set
        /// </summary>

        public int SetNumber
        {
            get;
            set;
        }

        /// <summary>
        /// The collection of items in this set
        /// </summary>

        public List<GrammarItem> Items
        {
            get;
            private set;
        }

        /// <summary>
        /// The GOTO table for the parser. When in a particular
        /// state, if a particular terminal or non terminal
        /// token has been recognised, this determines which
        /// next state to jump to. A state is synonymous with 
        /// an item set in an LR(1) parser.
        /// </summary>

        public Dictionary<GrammarElement, List<GrammarItemSet>> Shifts
        {
            get;
            private set;
        }

        /// <summary>
        /// When a complete rule has been recognised, this
        /// dictionary specifies for a given input token
        /// which production expansion is being replaced
        /// by its LHS token. This is used to know which
        /// block of user action code to execute, and then
        /// how many tokens to pop off the parser stack.
        /// </summary>

        public Dictionary<GrammarElement, List<GrammarProduction>> Reductions
        {
            get;
            private set;
        }

        /// <summary>
        /// Constructor. Initialises the new item set
        /// with a predetermined core of items
        /// </summary>
        /// <param name="setNum">A unique number for this set</param>
        /// <param name="coreItems">The list of grammar items
        /// passed in as the new core for this item set</param>

        public GrammarItemSet(List<GrammarItem> coreItems)
        {
            SetNumber = 0;
            Items = coreItems;
            Shifts = new Dictionary<GrammarElement, List<GrammarItemSet>>();
            Reductions = new Dictionary<GrammarElement, List<GrammarProduction>>();
        }

        /// <summary>
        /// Find out if the list of grammar items in this
        /// item set contains the specified grammar item
        /// </summary>
        /// <param name="gi">The item we are looking for</param>
        /// <returns>True if found, false if not</returns>

        public bool Contains(GrammarItem gi) => Items.Exists(item => item == gi);

        /// <summary>
        /// Render the item set as a string
        /// </summary>
        /// <returns>A list of rendered items as a string</returns>

        public override string ToString() => ToString(false);

        /// <summary>
        /// Render the item set as a string, with
        /// two levels of verbosity
        /// </summary>
        /// <param name="verbose">True for full details</param>
        /// <returns>Th list of rendered items as a string</returns>

        public string ToString(bool verbose)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("State S{0}{1}\r\n", SetNumber, (verbose ? " CORE ITEMS:" : ""));
            bool hasClosureItems = Items.Any(item => !item.Core);

            AppendLookAheadTokens(sb, Items, true, verbose);
            if (verbose)
            {
                if (hasClosureItems)
                {
                    sb.Append("CLOSURE ITEMS:\r\n");
                    AppendLookAheadTokens(sb, Items, false, verbose);
                }

                sb.Append("SHIFTS, REDUCTIONS AND GOTOS\r\n");
                foreach (GrammarTransition gt in TransitionsInOrder)
                {
                    if (gt.IsShift)
                        sb.AppendFormat("  {0}: SHIFT to state S{1}\r\n", gt.Element.ToString(), gt.ShiftOrGoto.SetNumber);
                    else if (gt.IsReduction)
                        sb.AppendFormat("  {0}: REDUCE by rule R{1}: {2};\r\n", gt.Element.ToString(),
                            gt.Reduction.ProductionNumber, gt.Reduction.ToString());
                    else
                        sb.AppendFormat("  {0}: GOTO state S{1}\r\n", gt.Element.ToString(), gt.ShiftOrGoto.SetNumber);
                }
            }
            return sb.ToString();
        }

        // Helper method called twice to generate the
        // core then the closure items in an item set.

        private static void AppendLookAheadTokens(StringBuilder sb, List<GrammarItem> items, bool core, bool verbose)
        {
            IEnumerable<IGrouping<int, GrammarItem>> closureQuery =
                from item in items
                where item.Core == core
                group item by item.Production.ProductionNumber into g
                select g;

            foreach (IGrouping<int, GrammarItem> g in closureQuery)
            {
                GrammarItem firstItem = g.First();
                sb.Append($"R{firstItem.Production.ProductionNumber}: " +
                    $"{firstItem.Production.ToString(firstItem.Position)}\r\n");
                if (verbose && firstItem.Production.RHS.Count <= firstItem.Position)
                {
                    foreach (GrammarItem gi in g)
                    {
                        if (gi == (object)firstItem)
                            sb.Append('{');
                        else
                            sb.Append(',');
                        sb.Append($"\r\n    {gi.LookAheadToken}");
                    }
                    sb.AppendLine("\r\n}");
                }
            }
        }

        /// <summary>
        /// The list of grammar elements for this item set, rearranged
        /// into the order needed when scanning the Shifts and Reductions
        /// properties to build the list of state machine transitions. Note
        /// that any scanner must process the shifts then the reductions, for
        /// each element in the list below, in order. This is only needed for
        /// LR(1) grammars, where we are just using the single look-ahead
        /// token to determine which transition to take. When building a GLR
        /// parser, all paths will be tried anyway, so rearranging the order
        /// would be irrelevant. Since this function is quite expensive to
        /// execute, the GLR parser generation skips this function call.
        /// For this reason, ElementsInOrder will always be null for GLR.
        /// </summary>

        public List<GrammarTransition> TransitionsInOrder
        {
            get;
            private set;
        }

        /// <summary>
        /// Number of shift transitions in this item set
        /// </summary>

        public int ShiftCount => TransitionsInOrder.Count(t => t.IsShift);

        /// <summary>
        /// Number of reduction transitions in this item set
        /// </summary>

        public int ReductionCount => TransitionsInOrder.Count(t => t.IsReduction);

        /// <summary>
        /// The number of goto transitions in this item set
        /// </summary>

        public int GotoCount => TransitionsInOrder.Count(t => t.IsGoto);

        /// <summary>
        /// Somewhere to put the offending list of pairs of grammar
        /// transitions that cannot be ordered because their guard
        /// conditions have intersecting truth tables.
        /// </summary>

        public List<KeyValuePair<GrammarTransition, GrammarTransition>> IntersectingPairs
        {
            get;
            private set;
        }

        /// <summary>
        /// Sort shifts and reductions within the item set. Business rules
        /// are as follows:
        /// <list type="bullet">
        ///  <item>
        ///   If the input event/tokens are different, lower ordinal
        ///   tokens appear earlier in the output list. This makes no
        ///   difference to parser execution, but groups the tokens
        ///   together in a standard order in the parser tables;
        ///  </item>
        ///  <item>
        ///   If tokens are the same, their guard conditions are
        ///   compared. If one guard condition is a subset of the
        ///   other, it will become the earlier of the two to be parsed;
        ///  </item>
        ///  <item>
        ///   If the guard conditions are intersecting, meaning that
        ///   there are overlapping sets of input values for which
        ///   they return true, but are otherwise disjoint, a warning
        ///   is added to error stream, and the transitions are ordered
        ///   according to their rule precedence (earlier rules in the
        ///   grammar have higher precedence);
        ///  </item>
        ///  <item>
        ///   Disjoint or equal guards are ordered by their rule
        ///   precedence. If the precedences are the same, meaning the
        ///   same rule is being applied, then shifts take precedence
        ///   over reductions.</item>
        /// </list>
        /// </summary>
        /// <returns>An error message identifying any places
        /// where ambiguous conditions were encountered</returns>

        public string ComputeShiftReduceOrder()
        {
            // Grab the list of transitions that pertain to
            // shifts and reductions, but excluding gotos.

            List<GrammarTransition> unorderedTransitions
                = CompileUnorderedTransitionList();

            // The final location for the transitions in
            // their correct state machine priority order

            TransitionsInOrder = new List<GrammarTransition>();

            // The list of any pairs of transitions that have
            // ambiguous guard conditions that the parser cannot
            // prioritise correctly with confidence. These result
            // in a warning message on the output error stream
            // from the parser generator.

            IntersectingPairs = new List<KeyValuePair<GrammarTransition, GrammarTransition>>();

            IEnumerable<IGrouping<int, GrammarTransition>> tokenGroups =
                unorderedTransitions.GroupBy(gt => gt.Element.Token.TokenNumber);

            foreach (IGrouping<int, GrammarTransition> tokenGroup in tokenGroups)
            {
                int oldCount = TransitionsInOrder.Count;
                TransitionsInOrder.AddRange
                    (tokenGroup.OrderBy(gt => gt.HammingWeight()));

                // Look through the most recently added set of transitions,
                // and find any pairs that have an ambiguous section of
                // their guard conditions' truth tables.

                for (int i = oldCount; i < TransitionsInOrder.Count; i++)
                {
                    for (int j = i + 1; j < TransitionsInOrder.Count; j++)
                    {
                        GrammarTransition l = TransitionsInOrder[i];
                        GrammarTransition r = TransitionsInOrder[j];
                        BoolExpr exprLeft = l.Element.Guard;
                        BoolExpr exprRight = r.Element.Guard;
                        if (exprLeft != null && exprRight != null)
                        {
                            BooleanComparison cmp = exprLeft.CompareExpressions(exprRight);
                            if (cmp == BooleanComparison.Intersect)
                                IntersectingPairs.Add
                                    (new KeyValuePair<GrammarTransition, GrammarTransition>(l, r));
                        }
                    }
                }
            }

            // Finally add the post-reduction goto transitions to the list

            foreach (GrammarElement ge in Shifts.Keys
                .Where(e => e.Token.TokenType == TokenType.Nonterminal))
            {
                if (Shifts.TryGetValue(ge, out List<GrammarItemSet> gotoTargets))
                    foreach (GrammarItemSet gis in gotoTargets)
                        TransitionsInOrder.Add(new GrammarTransition(ge, gis));
            }

            // Prepare the returned error message

            StringBuilder errMessage = new StringBuilder();
            foreach (KeyValuePair<GrammarTransition, GrammarTransition> gePair in IntersectingPairs)
            {
                errMessage.Append(
                    "Warning: ambiguous transitions for intersecting"
                    + " token guard conditions:\r\n"
                    + $"{gePair.Key.Element.Guard}\r\n"
                    + $"and: {gePair.Value.Element.Guard}\r\n"
                    + $"on token: {gePair.Key.Element.Token.Text}\r\n"
                    + $"in: {ToString(false)}\r\n");
            }
            return errMessage.ToString();
        }

        /// <summary>
        /// Assemble the list of GrammarTransition objects that represent
        /// terminal token shifts and reductions, but excluding the
        /// non-terminal goto transitions.
        /// </summary>
        /// <returns>The list of shift and reduction transitions</returns>

        private List<GrammarTransition> CompileUnorderedTransitionList()
        {
            List<GrammarTransition> unorderedTransitions
                = new List<GrammarTransition>();

            // Add shifts into the list of transitions

            foreach (GrammarElement ge in Shifts.Keys
                .Where(e => e.Token.TokenType == TokenType.Terminal))
            {
                if (Shifts.TryGetValue(ge, out List<GrammarItemSet> shiftTargets))
                    foreach (GrammarItemSet gis in shiftTargets)
                        unorderedTransitions.Add(new GrammarTransition(ge, gis));
            }

            // Add reductions to the list of transitions

            foreach (GrammarElement ge in Reductions.Keys)
            {
                if (Reductions.TryGetValue(ge, out List<GrammarProduction> reductionTargets))
                    foreach (GrammarProduction gp in reductionTargets)
                        unorderedTransitions.Add(new GrammarTransition(ge, gp));
            }

            return unorderedTransitions;
        }
    }
}
