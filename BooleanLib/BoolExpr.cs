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
using System.Collections.Generic;
using System.Text;

namespace BooleanLib
{
    /// <summary>
    /// Result of comparing two boolean functions.
    /// Meaning of values:
    /// Intersect
    ///     Neither function is a subset of the
    ///     other, but they have some input
    ///     combinations for which they both
    ///     return true.
    /// LeftIsSubsetOfRIght
    ///     The left function never returns true
    ///     when the right function returns false.
    /// RightIsSubsetOfLeft
    ///     The right function never returns true
    ///     when the left function returns false.
    /// Disjoint
    ///     The left and right functions never
    ///     return true at the same time.
    /// Independent
    ///     The left and right functions share
    ///     no common variables.
    /// </summary>

    public enum BooleanComparison
    {
        Intersect = 0,
        LeftIsSubsetOfRight = 1,
        RightIsSubsetOfLeft = 2,
        Equal = 3,
        Disjoint = 4,
        Independent = 5
    }

    /// <summary>
    /// Common base class for all boolean expression nodes
    /// </summary>

    public abstract class BoolExpr
    {
        /// <summary>
        /// Reimplement operator == to compare by value
        /// </summary>
        /// <param name="l">Left operand</param>
        /// <param name="r">Right operand</param>
        /// <returns>True if both null, or both have same value</returns>

        public static bool operator ==(BoolExpr l, BoolExpr r)
        {
            // Obviously equal if same instance, or both null

            if (object.ReferenceEquals(l, r))
                return true;

            // Deal with one or other being null

            if (l is null || r is null)
                return false;

            // Short circuit the truth table comparison by seeing if
            // either expression has computed its unique identifier.
            // Expressions with identical truth tables also have
            // identical identifiers.

            if (l.asIdentifier != null && r.asIdentifier != null)
                return l.asIdentifier == r.asIdentifier;

            // Compare value fields

            return l.CompareExpressions(r) == BooleanComparison.Equal;
        }

        /// <summary>
        /// Unused eqaulity override
        /// </summary>
        /// <param name="obj">The other BoolExpr we 
        /// shall compare against</param>
        /// <returns>True if the two expressions are equal</returns>

        public override bool Equals(object obj)
        {
            // Perform a derived type check. If
            // not the same derived class, fail.

            BoolExpr arg = obj as BoolExpr;
            return this == arg;
        }

        public override int GetHashCode() =>
            // Too difficult to do as requires
            // computation of unique identifier

            0;

        /// <summary>
        /// Operator != implemented as complement
        /// of operator ==.
        /// </summary>
        /// <param name="l">Left operand</param>
        /// <param name="r">Right operand</param>
        /// <returns>True if not equal or not both null</returns>

        public static bool operator !=(BoolExpr l, BoolExpr r) => !(l == r);

        /// <summary>
        /// Return the block of 64 bits from the truth table
        /// at block index specified in the argument.
        /// </summary>
        /// <param name="blockNum">Which block of the truth
        /// table to return the block of result bits from.</param>
        /// <returns>Bits 64*blockNum to 64*(blockNum+1)-1 of
        /// the resulting truth table.</returns>

        public abstract ulong ResultBits(long blockNum);

        /// <summary>
        /// Return the block of 64 bits from the truth table
        /// at block index specified in the argument.
        /// </summary>
        /// <param name="blockNum">Which block of the truth
        /// table to return the block of result bits from.</param>
        /// <param name="leafIndexProvider">An alternative
        /// provider of leaf indices, used when producing
        /// unique expression identifiers</param>
        /// <returns>Bits 64*blockNum to 64*(blockNum+1)-1 of
        /// the resulting truth table.</returns>

        public abstract ulong ResultBits(long blockNum, LeafIndexProvider leafIndexProvider);

        /// <summary>
        /// Bit mask with a bit set for each leaf variable
        /// that appears in the current expression
        /// </summary>

        public abstract long Leaves
        {
            get;
        }

        /// <summary>
        /// Compare two boolean expressions to see their
        /// logical set theoretical relationship. See the
        /// description of the BooleanComparison enum for
        /// the different results that can be returned.
        /// </summary>
        /// <param name="otherExpr">Right expression to be compared</param>
        /// <returns>The results of thec comparison</returns>

        public BooleanComparison CompareExpressions(BoolExpr otherExpr)
        {
            ArgumentNullException.ThrowIfNull(otherExpr);

            // First check for independent boolean functions.
            // Such pairs of functions have non-overlapping
            // sets of boolean leaf variables. NOTE: Ideally
            // we should minimise the leaves here, as the
            // expressions might eliminate some of their
            // own variables. However, in the interests of
            // performance, we assume the writers of the
            // boolean expressions aren't that daft.

            if ((Leaves & otherExpr.Leaves) == 0)
                return BooleanComparison.Independent;

            // Use the leaf nodes included in the expressions
            // to minimise the number of comparison operations.
            // Set up the word mask to be used for counting
            // through the rows of the truth table. This
            // algorithm skips any parts of the truth table
            // that are repeated because of omitted variables
            // in the list of leaves held by the exresssion
            // factory. Note that the expressions below all
            // assume the maximum number of leaves in the
            // boolean leaf index provider is 63 or fewer.
            // If it was 64, the right shifts below would
            // have to have the shifted sign bit masked off.

            long leaves = Leaves | otherExpr.Leaves;
            leaves >>= 6;

            // Variables used for accumulating results

            ulong nonDisjointBits = 0;
            ulong leftNotSubOfRight = 0;
            ulong rightNotSubOfLeft = 0;

            // Now loop through the two truth tables.
            // The loop terminates early if all the
            // resulting conditions have been determined.

            long ttIndex = 0;
            do
            {
                ulong rrb = otherExpr.ResultBits(ttIndex);
                ulong lrb = ResultBits(ttIndex);
                nonDisjointBits |= lrb & rrb;
                leftNotSubOfRight |= lrb & ~rrb;
                rightNotSubOfLeft |= ~lrb & rrb;
                ttIndex = MaskedIncrement(leaves, ttIndex);
            } while
            (
                ttIndex != 0 &&
                (
                    nonDisjointBits == 0 ||
                    leftNotSubOfRight == 0 ||
                    rightNotSubOfLeft == 0
                )
            );

            BooleanComparison result = 0;
            if (nonDisjointBits == 0)
                result = BooleanComparison.Disjoint;
            if (leftNotSubOfRight == 0)
                result |= BooleanComparison.LeftIsSubsetOfRight;
            if (rightNotSubOfLeft == 0)
                result |= BooleanComparison.RightIsSubsetOfLeft;
            return result;
        }

        /// <summary>
        /// Check the truth table for a boolean expression to find
        /// exactly what the minimum set of leaf variables should
        /// be for the expression. Since boolean operators can
        /// sometimes cause a variable to not contribute to the
        /// expression output e.g. A AND B OR A AND NOT B, this
        /// function guarantees to find the minimum set of actual
        /// variables that contribute to a boolean function.
        /// </summary>
        /// <param name="expr">The expression to check</param>
        /// <returns>The minimum set of leaves that
        /// contribute to this expression</returns>

        public long MinimisedLeaves()
        {
            // Retrieve the unminimised leaf mask. Our goal
            // is to remove bits from this where a leaf
            // does not contribute to the output values
            // of an expression. First we split the unminimised
            // leaf mask into the least significant 6 bits and
            // the rest. Note that the expressions below all
            // assume the maximum number of leaves in the
            // boolean leaf index provider is 63 or fewer.
            // If it was 64, the right shifts below would
            // have to have the shifted sign bit masked off.

            long leaves = Leaves;
            int lsSixBits = (int)(leaves & 0x3FL);
            long leafWords = leaves >> 6;
            leaves = 0;
            long msLeafBits = 0;
            long ttIndex = 0;

            do
            {
                ulong resultBits = 0;
                bool resultBitsFetched = false;

                // First test the least significant 6 leaf indexes

                if (lsSixBits != 0)
                {
                    resultBits = ResultBits(ttIndex);
                    resultBitsFetched = true;

                    for (int leafIdx = 0; leafIdx < 6; leafIdx++)
                    {
                        int leafBit = 1 << leafIdx;

                        // Only look at the truth table for bits that have not already
                        // been proven to be part of the minimum set, but that are also
                        // identified as being a leaf variable in the non-minimised expression.

                        if ((lsSixBits & leafBit) != 0 && (leaves & leafBit) == 0)
                        {
                            ulong pattern = LeafExpr.Pattern(leafIdx, 0);
                            if ((resultBits & pattern) != ((resultBits & ~pattern) << leafBit))
                                leaves |= (uint)leafBit;
                        }
                    }
                }

                // Now test each higher order leaf bit between result words. As
                // leaf bits get found, we no longer need to test those as they
                // are guaranteed to be in the list of leaves. Hence the masking
                // of leafWords by msLeafBits.

                long maskedIndex = leafWords & ~msLeafBits;
                while (maskedIndex != 0)
                {
                    // Extract the bit pattern that has a single 1 in the
                    // bit position of the maskedIndex variable's least
                    // significant 1 bit.

                    long currBitMask = maskedIndex & (-maskedIndex);

                    // Remove this least significant 1 bit from the mask

                    maskedIndex -= currBitMask;

                    // Test the corresponding bit of the current truth table
                    // word we are inspecting. If it is zero, we need to
                    // compare it with the corresponding word at the offset
                    // into the truth table that has a 1 at this position.

                    if ((ttIndex & currBitMask) == 0 && (msLeafBits & currBitMask) == 0)
                    {
                        if (!resultBitsFetched)
                        {
                            resultBits = ResultBits(ttIndex);
                            resultBitsFetched = true;
                        }

                        // If we find a bit that is in the set of leaves, we
                        // can also remove it from leafWords, as we no longer need
                        // to look for it as a candidate leaf bit.

                        if (resultBits != ResultBits(ttIndex | currBitMask))
                            msLeafBits |= currBitMask;
                    }
                }
                ttIndex = MaskedIncrement(leafWords, ttIndex);
            } while (ttIndex != 0);

            // Reassemble the least significan six bits
            // with the topmost bits of the leaf pattern.

            return leaves | (msLeafBits << 6);
        }

        /// <summary>
        /// Compute the Hamming weight of the boolean function.
        /// </summary>
        /// <returns>A value between 0 and 0x4000000000000000
        /// representing the proportion of the output truth table
        /// that contains 1 values rather than 0 values.</returns>

        public long HammingWeight()
        {
            long weight = 0;
            long truthTableRows = 0;
            long leafWords = Leaves >> 6;
            long ttIndex = 0;
            do
            {
                truthTableRows += 64;
                weight += HammingWeight(ResultBits(ttIndex));
                ttIndex = MaskedIncrement(leafWords, ttIndex);
            } while (ttIndex != 0);

            // The value in truthTableRows is a power of two,
            // i.e. it has a Hamming weight of 1. We want to
            // normalise all weights so that a direct integer
            // comparison can be performed. All weights are
            // normalised so that the truthtableRows value
            // becomes 0x4000000000000000.

            for (int shiftCount = 32; shiftCount > 0; shiftCount >>= 1)
                if (truthTableRows < (1L << (LeafIndexProvider.MaxVarCount + 1 - shiftCount)))
                {
                    truthTableRows <<= shiftCount;
                    weight <<= shiftCount;
                }
            return weight;
        }

        /// <summary>
        /// Given a 64 bit unsigned integer, return a count of how many
        /// bits in that number are set to true. This count is known as
        /// the Hamming Weight for the number.
        /// </summary>
        /// <param name="blockBits">The number whose weight is needed</param>
        /// <returns>The weight of the number.</returns>

        public static int HammingWeight(ulong blockBits)
        {
            unchecked
            {
                blockBits -= (blockBits & 0xAAAAAAAAAAAAAAAAUL) >> 1;
                blockBits = (blockBits & 0x3333333333333333UL) + ((blockBits & 0xCCCCCCCCCCCCCCCCUL) >> 2);
                blockBits = (blockBits & 0x0F0F0F0F0F0F0F0FUL) + ((blockBits & 0xF0F0F0F0F0F0F0F0UL) >> 4);
                return (int)((blockBits * 0x0101010101010101UL) >> 56);
            }
        }

        private string asIdentifier = null;

        /// <summary>
        /// Return the expression as an identifier, that is
        /// unique and based on the expression's truth table.
        /// This ensures that the resulting identifier is the
        /// same for different expressions that yield the
        /// same boolean outputs for their inputs.
        /// </summary>
        /// <param name="expr">The expression to be
        /// rendered as an identifier</param>
        /// <returns>The string identifier form of the
        /// expression</returns>

        public virtual string AsIdentifier(LeafIndexProvider leafIndexProvider)
        {
            if (asIdentifier == null)
            {
                ArgumentNullException.ThrowIfNull(leafIndexProvider);

                StringBuilder sb = new();

                // Find the normalised minimum set of leaf names
                // that actually contribute to the expression

                long minLeaves = MinimisedLeaves();
                List<string> leafNames = leafIndexProvider.LeafNamesFromMask(minLeaves);

                // Create a new leaf provider, and add each of the
                // minimum set of names into it, in the original
                // provider order, from LSB to MSB. Also,
                // create the first part of the unique identifier,
                // which consists of the individual leaf names
                // catenated in order, separated by underscores.

                LeafIndexProvider exprProvider = new();
                foreach (string leafName in leafNames)
                {
                    exprProvider.FindLeafIndex(leafName);
                    sb.Append(leafName);
                    sb.Append('_');
                }

                // Now glue on the truth table output bits.
                // These are appended as a sequence of 
                // hexadecimal digits. Leading zeros are
                // not suppressed, but the number of
                // digits reflects the number of rows
                // in the truth table. This is only
                // true though for expressions with
                // two or more input leaves.

                int leafCount = leafNames.Count;
                if (leafCount <= 5)
                {
                    ulong resultBits = ResultBits(0, exprProvider);
                    resultBits &= (1UL << (1 << leafCount)) - 1;
                    string formatString = "{0:X1}";
                    if (leafCount >= 2)
                        formatString = "{0:X" + (1 << (leafCount - 2)) + "}";
                    sb.AppendFormat(formatString, resultBits);
                }
                else
                {
                    long truthTableBlockCount = 1L << (leafCount - 6);
                    for (long blkNum = 0; blkNum < truthTableBlockCount; blkNum++)
                        sb.AppendFormat("{0:X16}", ResultBits(blkNum, exprProvider));
                }
                asIdentifier = sb.ToString();
            }
            return asIdentifier;
        }

        /// <summary>
        /// Increment an integer, using a bit mask to indicate
        /// which bits of the integer are used for counting.
        /// Bits set to zero in the mask word behave as if they
        /// are always zero, while carry bits propagate to the
        /// next bit column that has a 1 in the mask word.
        /// </summary>
        /// <param name="mask">The pattern indicating
        /// which bits participate in the count</param>
        /// <param name="previousValue">The value to
        /// be incremented.</param>
        /// <returns>The incremented value</returns>

        private static long MaskedIncrement(long mask, long previousValue) => (previousValue - mask) & mask;
    }
}
