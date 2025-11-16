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

namespace Parsing;

/// <summary>
/// Encapsulates an event, a guard condition,
/// a transition action and a next state
/// </summary>

public class FSMTransition
{
    /// <summary>
    /// The recognised input event type
    /// that would cause this transition
    /// to be taken.
    /// </summary>

    public int InputToken
    {
        get;
        set;
    }

    /// <summary>
    /// The guard condition to test for that
    /// is associated with this input event.
    /// Both the input token must match and
    /// the guard condition must be true
    /// when executed, for the transition to
    /// be taken according to this column.
    /// </summary>

    public IGuardEvaluator Condition
    {
        get;
        set;
    }

    /// <summary>
    /// If used, a linked list of action functions to
    /// be called when transition is made.
    /// </summary>

    public FSMInlineAction InlineActions
    {
        get;
        set;
    }

    /// <summary>
    /// The link to the next state after this transition
    /// </summary>

    public FSMState NextState
    {
        get;
        set;
    }

    /// <summary>
    /// Render a meaningful representation of the transition
    /// </summary>
    /// <param name="tokenMap">Lookup table of event names</param>
    /// <returns>Meaningful string representation of transition</returns>

    public string ToString(TwoWayMap<string, int> tokenMap)
    {
        ArgumentNullException.ThrowIfNull(tokenMap);

        string result = tokenMap[InputToken];
        if (Condition != null)
            result += $"[{Condition.AsString()}]";
        if (NextState == null)
            result += " terminates state machine";
        else
            result += $" -> {NextState.Name}";
        return result;
    }

    /// <summary>
    /// Standard override of ToString method. No
    /// lookup of token type name used.
    /// </summary>
    /// <returns>String representation of transition</returns>

    public override string ToString() => $"{InputToken}[{Condition.AsString()}] -> {NextState.Name}";
}
