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
using ParserGenerator;
using System;
using System.Collections.Generic;

namespace Parsing;

/// <summary>
/// Factory class that manufactures IGuardEvaluator
/// objects from BoolExpr objects. IGuardEvaluator
/// objects are callable at runtime.
/// </summary>

public class GuardEvaluatorFactory
{
    private readonly int[] writtenGuards;
    private readonly Grammar grammar;

    /// <summary>
    /// The array of guard evaluators that
    /// are used at runtime
    /// </summary>

    public List<IGuardEvaluator> Guards
    {
        get;
        private set;
    }

    /// <summary>
    /// Constructor for factory
    /// </summary>
    /// <param name="g">The grammar object for which
    /// the factory will build evaluators</param>

    public GuardEvaluatorFactory(Grammar g)
    {
        Guards = [];
        grammar = g ?? throw new ArgumentNullException(nameof(g));

        // Create a new array index list for the guard functions,
        // and initialise them to -1 which represents the unset value

        writtenGuards = new int[g.BooleanExpressionCache.Count];
        for (int i = 0; i < writtenGuards.Length; i++)
            writtenGuards[i] = -1;

    }

    /// <summary>
    /// Obtain the boolean expression evaluator for the
    /// expression passed as an argument. This method
    /// ensures that each unique guard evaluator is
    /// shared among all parser state table rows that
    /// require the same boolean guard.
    /// </summary>
    /// <param name="be">The boolean expression to convert
    /// to an executable format</param>
    /// <returns>The evaluator tree corresponding to the
    /// executable guard expression</returns>

    public IGuardEvaluator GetBoolEvaluator(BoolExpr be)
    {
        // Handle the case where there is no expression

        if (be == null)
            return null;

        // Find the index into the expression cache of the expression be

        int guardProcNumber = grammar.BooleanExpressionCache.IndexOf(be);

        // If the expression is not already in the output evaluator list,
        // add it to the list, and record its position in the list

        if (guardProcNumber >= 0 && writtenGuards[guardProcNumber] < 0)
        {
            writtenGuards[guardProcNumber] = Guards.Count;
            Guards.Add(BuildBoolEvaluator(be));
        }

        // Find the reference to the (shared) instance
        // of the required expression evaluator

        return Guards[writtenGuards[guardProcNumber]];
    }

    /// <summary>
    /// Build the run-time guard expression parse tree
    /// </summary>
    /// <param name="be">The boolean expression to convert
    /// to an executable format</param>
    /// <returns>The evaluator tree corresponding to the
    /// executable guard expression</returns>

    private static IGuardEvaluator BuildBoolEvaluator(BoolExpr be)
    {
        // Try the node as a leaf element

        LeafExpr le = be as LeafExpr;
        if (le != null)
            return new LeafEvaluator(le.Name);

        // Try a not operator

        NotExpr ne = be as NotExpr;
        if (ne != null)
            return new NotEvaluator
            {
                Arg = BuildBoolEvaluator(ne.Argument)
            };

        // Try an AND operator

        AndExpr andExpr = be as AndExpr;
        if (andExpr != null)
            return new AndEvaluator
            {
                Left = BuildBoolEvaluator(andExpr.Left),
                Right = BuildBoolEvaluator(andExpr.Right)
            };

        // Try an OR operator

        OrExpr orExpr = be as OrExpr;
        if (orExpr != null)
            return new OrEvaluator
            {
                Left = BuildBoolEvaluator(orExpr.Left),
                Right = BuildBoolEvaluator(orExpr.Right)
            };

        // None of the above, so probably a null expression

        return null;
    }
}
