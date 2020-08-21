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

namespace BooleanLib
{
    public class LeafExpr : BoolExpr
    {
        /// <summary>
        /// The index of the leaf variable
        /// within the lookup table for
        /// variables in the expression factory.
        /// </summary>

        private readonly int index;

        /// <summary>
        /// The name of the boolean variable
        /// </summary>

        public string Name
        {
            get;
            private set;
        }

        /// <summary>
        /// Constructor. This is only used
        /// internally by the expression factory.
        /// To create a leaf object, use
        /// BoolExprFactory.FindOrCreateLeaf
        /// </summary>
        /// <param name="name">Name of the variable</param>
        /// <param name="indexProvider">The expression
        /// factory under which this variable will
        /// be registered</param>

        public LeafExpr(string name, LeafIndexProvider indexProvider)
        {
            if (indexProvider == null)
                throw new ArgumentNullException(nameof(indexProvider));

            Name = name;
            index = indexProvider.FindLeafIndex(name);
        }

        /// <summary>
        /// Return the block of 64 bits from the truth table
        /// at block index specified in the argument.
        /// </summary>
        /// <param name="blockNum">Which block of the truth
        /// table to return the block of result bits from.</param>
        /// <returns>Bits 64*blockNum to 64*(blockNum+1)-1 of
        /// the resulting truth table.</returns>

        public override ulong ResultBits(long blockNum) => ResultBits(blockNum, null);

        /// <summary>
        /// Return the block of 64 bits from the truth table
        /// at block index specified in the argument.
        /// </summary>
        /// <param name="blockNum">Which block of the truth
        /// table to return the block of result bits from.</param>
        /// <param name="leafIndexProvider">An alternative
        /// lookup table between leaf names and truth
        /// table indexes.</param>
        /// <returns>Bits 64*blockNum to 64*(blockNum+1)-1 of
        /// the resulting truth table.</returns>

        public override ulong ResultBits(long blockNum, LeafIndexProvider leafIndexProvider)
        {
            if (leafIndexProvider == null)
                return Pattern(index, blockNum);
            else
                return Pattern(leafIndexProvider.FindLeafIndex(Name), blockNum);
        }

        /// <summary>
        /// The bit masks used to define truth table
        /// patterns for the low order 6 bits of the
        /// truth table variables.
        /// </summary>

        private static readonly ulong[] lowOrderPatterns =
        {
            0xAAAAAAAAAAAAAAAAL,
            0xCCCCCCCCCCCCCCCCL,
            0xF0F0F0F0F0F0F0F0L,
            0xFF00FF00FF00FF00L,
            0xFFFF0000FFFF0000L,
            0xFFFFFFFF00000000L
        };

        /// <summary>
        /// Given the index of a leaf, return the leaf's
        /// truth table bit pattern for the specified
        /// block of 64 result bits in the leaf's
        /// truth table.
        /// </summary>
        /// <param name="leafIndex">The leaf index in the
        /// leaf index provider's lookup table</param>
        /// <param name="blockNum">Which block of
        /// 64 bits from the truth table results
        /// to look up the leaf pattern for</param>
        /// <returns>The output truth table block</returns>

        public static ulong Pattern(int leafIndex, long blockNum)
        {
            // The first few leaf variables have truth tables
            // that are taken from the array of patterns called
            // lowOrderPatterns declared above. Ony when the
            // variable appears much later in the list of
            // variables do we have to have multiple unsigned
            // longs to represent the full truth table.

            if (leafIndex <= 5)
                return lowOrderPatterns[leafIndex];
            else if ((blockNum & (1L << (leafIndex - 6))) != 0)
                return 0xFFFFFFFFFFFFFFFFL;
            else
                return 0L;
        }

        /// <summary>
        /// Return the maximum valued index into the leaf
        /// list of the boolean expression factory of any
        /// leaf in this expression.
        /// </summary>

        public int MaxLeafIndex => index;

        /// <summary>
        /// Bit mask with a bit set for each leaf variable
        /// that appears in the current expression
        /// </summary>

        public override long Leaves => 1L << index;

        public override string ToString() => Name;

        /// <summary>
        /// Return the leaf expression as an identifier, that
        /// is unique and based on the expression's truth table.
        /// This ensures that the resulting identifier is the
        /// same for different expressions that yield the
        /// same boolean outputs for their inputs.
        /// </summary>
        /// <param name="expr">The expression to be
        /// rendered as an identifier</param>
        /// <returns>The string identifier form of the
        /// expression</returns>

        public override string AsIdentifier(LeafIndexProvider leafIndexProvider) => Name;
    }
}
