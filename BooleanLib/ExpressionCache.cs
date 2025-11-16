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

using System.Collections.Generic;
using System.Linq;

namespace BooleanLib;

/// <summary>
/// In order to reuse boolean expression code, we keep a cache
/// of boolean expressions. Identical boolean expressions are
/// fused to form a single version within an instance of a
/// boolean expression cache. Boolean expressions are given
/// a unique ID number that can be used when translating the
/// boolean expression into a function name, or when placing
/// it into an indexed collection of expression implementations.
/// </summary>

public class ExpressionCache
{
    // The default leaf index provider used for
    // building truth tables and comparing expressions

    public LeafIndexProvider IndexProvider
    {
        get;
        private set;
    }

    // The physical storage of the cache of expressions

    private readonly List<BoolExpr> expressionList;

    /// <summary>
    /// Default constructor. Creates a new empty cache
    /// for expressions that have been interned, plus
    /// a default leaf index provider for recording
    /// which truth table column a leaf occupies.
    /// </summary>

    public ExpressionCache()
    {
        expressionList = [];
        IndexProvider = new LeafIndexProvider();
    }

    /// <summary>
    /// Place the expression into the expression cache. If
    /// the canonically equivalent expression is already in
    /// the cache, return a reference to the existing
    /// expression in the cache. If not, insert the
    /// expression, and return its reference.
    /// </summary>
    /// <param name="expr">The expression to intern</param>
    /// <returns>A reference to the shared version of
    /// the interned expression</returns>

    public BoolExpr InternExpression(BoolExpr expr)
    {
        // Don't insert null expressions in the cache

        if (expr is null)
            return null;

        // Ensure the expression has its unique string identifier
        // created

        expr.AsIdentifier(IndexProvider);

        // See if a reference to an equivalent expression
        // is already present in the cache

        BoolExpr foundExpression = expressionList.FirstOrDefault(be => be == expr);
        if (foundExpression == null)
        {
            expressionList.Add(expr);
            return expr;
        }
        else
            return foundExpression;
    }

    /// <summary>
    /// Read-only indexer for looking up the boolean
    /// expression with the specified index
    /// </summary>
    /// <param name="i">Index of expression to look up</param>
    /// <returns>The expression at this index. Note that an
    /// out of range index causes an exception to be thrown.</returns>

    public BoolExpr this[int i] => expressionList[i];

    /// <summary>
    /// Find the unique index number of the specified boolean expression.
    /// Note that the argument must be an expression that has previously
    /// been interned in the cache. If not, IndexOf may return -1 even
    /// though the equivalent expression is in the cache.
    /// </summary>
    /// <param name="be">The already interned expression to seek</param>
    /// <returns>The unique index for the expression, or -1 if
    /// the expression is not in the expression cache</returns>

    public int IndexOf(BoolExpr be)
    {
        for (int i = 0; i < expressionList.Count; i++)
            if (be == (object)(expressionList[i]))
                return i;
        return -1;
    }

    /// <summary>
    /// Number of expressions in the expression cache
    /// </summary>

    public int Count => expressionList.Count;
}
