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
using System.Linq;
using System.Text;

namespace Parsing;

/// <summary>
/// An implementation of a non-terminal token as
/// recognised following a rule reduction in a
/// parser. This version of a token has a production
/// that caused it to be created, a type that is
/// the same type as the non-terminal token type
/// to the left of its reducing production, and a
/// value that is computed from the action functions
/// associated with the rule reduction. This
/// class is intended for use with the GLR parsing
/// algorithm rather than the faster LR(1) parser.
/// </summary>

public class NonterminalToken : IToken
{
    // The grammar rule that was applied to create
    // this reduced non-terminal token

    private readonly ParserProduction reducingProduction;

    // The parser this non-terminal token belongs to

    private readonly IParser parentParser;

    // The list of tokens that appear to the right
    // of this production rule. Some of these may
    // be regular terminal token objects. Some
    // may also be NonterminalToken objects as they
    // themselves are the result of earlier reductions.

    public IToken[] Children
    {
        get;
        private set;
    }

    /// <summary>
    /// Create a non-terminal token that is compatible
    /// with the specified reduction grammar rule
    /// </summary>
    /// <param name="pp">The production that caused
    /// the reduction to this non-terminal</param>
    /// <param name="p">The parser instance that
    /// is parsing this token sequence</param>
    /// <param name="rhsTokens">An enumerable
    /// collection of tokens that contain the
    /// values for the tokens being popped
    /// from the stack in a reduction.</param>

    public NonterminalToken
        (ParserProduction pp, IParser p, IEnumerable<IToken> rhsTokens)
    {
        parentParser = p;
        reducingProduction = pp;
        Children = [.. rhsTokens];
        if (pp == null || Children.Length != pp.TokenCount)
            throw new ArgumentException
                ("Trellis path length does not match reduction rule");
        nonterminalTokenValue = null;
        valueComputed = false;
    }

    /// <summary>
    /// The token type. This is one of the non-terminal
    /// token types defined by the grammar.
    /// </summary>

    public int Type => reducingProduction.NonterminalType;

    // Cache for storing the value of this token
    // once the action functions have been executed.
    // Used to prevent recomputation of actions when
    // the Value property is accessed more than once.

    private object nonterminalTokenValue;

    // Flag to indicate whether the actions have
    // been executed yet.

    private bool valueComputed;

    /// <summary>
    /// The value associated with this token. As this
    /// is a non-terminal token, this value is usually
    /// computed by the execution of the action function(s)
    /// associated with the reduction of this rule. Once
    /// computed, it may be assigned to, overwriting its
    /// computed value.
    /// </summary>

    public object Value
    {
        get
        {
            if (!valueComputed)
            {
                valueComputed = true;

                // Copy the list of daughter token
                // values into an array to be passed to the
                // action functions associated with this
                // rule's completion. Note that slot zero
                // of the array is reserved for the return value.

                object[] args = new object[reducingProduction.TokenCount + 1];

                // Copy the tokens for the rule being reduced across
                // to the array of arguments to the action function

                for (int i = reducingProduction.TokenCount; i > 0; i--)
                {
                    if (Children[i - 1] != null)
                        args[i] = Children[i - 1].Value;
                }

                // Default is that the 'return' object from the
                // reduction is null. It is the responsibility
                // of the developer to ensure a value is assigned
                // to '$$' to set the value of the token a rule
                // reduces to.

                args[0] = null;

                // Execute the action functions to
                // compute the resulting value. Note
                // that the final value of this token
                // is the computed result from the
                // last action function in the list.

                ParserInlineAction currAction = reducingProduction.InlineAction;
                if (currAction != null)
                {
                    // Debug output

                    if (parentParser.ErrorLevel >= MessageLevel.DEBUG)
                    {
                        parentParser.MessageWrite(MessageLevel.DEBUG, "Inline action: {0}(",
                                currAction.MethodName);
                        for (int i = 0; i < args.Length; i++)
                        {
                            if (i > 0)
                                parentParser.MessageWrite(MessageLevel.DEBUG, ", ");
                            parentParser.MessageWrite
                                (MessageLevel.DEBUG, "{0}", (args[i] ?? "(null)").ToString());
                        }
                        parentParser.MessageWriteLine(MessageLevel.DEBUG, ")");
                    }

                    parentParser.ActionError = null;

                    // Now execute the action method

                    currAction.Function(parentParser, args);

                    // Rudimentary error handling based on return
                    // data from user action functions. This should
                    // be beefed up to support error recovery by
                    // popping the parser stack back to a known
                    // error token in a rule, and discarding
                    // input tokens until a matched token.

                    if (parentParser.ActionError != null)
                    {
                        parentParser.MessageWriteLine
                        (
                            MessageLevel.WARN,
                            "{0} when reducing {1}",
                            parentParser.ActionError.Message,
                            reducingProduction.ToString(false)
                        );
                    }
                }

                // All action functions now completed, so copy
                // the result of the last action function out
                // to the value field of this non terminal token.

                nonterminalTokenValue = args[0];
            }
            return nonterminalTokenValue;
        }
        set
        {
            // Assigning a value to the non-terminal
            // token disables the mechanism that
            // computes its value from descendent
            // rules. Normal use would retrieve the
            // value of this token before attempting
            // to set it.

            nonterminalTokenValue = value;
            valueComputed = true;
        }
    }

    /// <summary>
    /// Return the position information for the
    /// beginning of this recognised non-terminal
    /// token. Note that at present if the rule is
    /// a reduction of an empty rule, the position
    /// is also set to the empty string.
    /// </summary>

    public string Position
    {
        get
        {
            if (Children != null && Children.Length > 0)
                return Children[0].Position;
            else
                return string.Empty;
        }
    }

    // Indent a string builder by a nominated number of indent tabs

    private static void Indent(StringBuilder b, int indent)
    {
        for (int i = 0; i < indent; i++)
            b.Append("  ");
    }

    /// <summary>
    /// Produce a string representation of the token for debugging
    /// </summary>
    /// <param name="tokenMap">The lookup table for tokens</param>
    /// <param name="showValue">True to include the token value
    /// in the string, false to only show the type</param>
    /// <param name="indent">Indent spacing for resulting string</param>
    /// <returns>String representation of the token</returns>

    public string AsString(TwoWayMap<string, int> tokenMap, bool showValue, int indent) => TreeString(tokenMap, showValue, indent, string.Empty);

    // Internal recursive implementation of tree view for NonterminalToken and its children

    private string TreeString(TwoWayMap<string, int> tokenMap, bool showValue, int indent, string preamble)
    {
        // First show the top level node for this NonterminalToken

        StringBuilder sb = new();
        Indent(sb, indent);
        if (preamble.Length >= 4)
            sb.Append(preamble[0..^2] + "+-");
        sb.Append(tokenMap[Type]);
        if (showValue && Value != null)
            sb.AppendFormat("[{0}]", Value.ToString());

        // Now output a representation for each of the child nodes.
        // This is implemented recursively in order to produce a tree.

        if (Children != null)
        {
            foreach (IToken daughter in Children)
            {
                sb.AppendLine();
                if (daughter is NonterminalToken child)
                {
                    string childPreamble;
                    if (daughter == Children.LastOrDefault())
                        childPreamble = preamble + "    ";
                    else
                        childPreamble = preamble + "  | ";
                    sb.Append(child.TreeString(tokenMap, showValue, indent, childPreamble));
                }
                else
                {
                    Indent(sb, indent);
                    sb.Append(preamble + "  +-" + daughter.AsString(tokenMap, true, 0));
                }
            }
        }
        return sb.ToString();
    }
}
