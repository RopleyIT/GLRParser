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


namespace BooleanLib;

public class OrExpr(BoolExpr l, BoolExpr r) : BoolExpr
{
    public BoolExpr Left
    {
        get;
        private set;
    } = l;

    public BoolExpr Right
    {
        get;
        private set;
    } = r;

    /// <summary>
    /// Return the block of 64 bits from the truth table
    /// at block index specified in the argument.
    /// </summary>
    /// <param name="blockNum">Which block of the truth
    /// table to return the block of result bits from.</param>
    /// <returns>Bits 64*blockNum to 64*(blockNum+1)-1 of
    /// the resulting truth table.</returns>

    public override ulong ResultBits(long blockNum) => Left.ResultBits(blockNum) | Right.ResultBits(blockNum);

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

    public override ulong ResultBits(long blockNum, LeafIndexProvider leafIndexProvider) => Left.ResultBits(blockNum, leafIndexProvider)
            | Right.ResultBits(blockNum, leafIndexProvider);

    /// <summary>
    /// Bit mask with a bit set for each leaf variable
    /// that appears in the current expression
    /// </summary>

    public override long Leaves => Left.Leaves | Right.Leaves;

    public override string ToString() => $"{Left} OR {Right}";
}
