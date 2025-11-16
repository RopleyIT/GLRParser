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

using System.Text.RegularExpressions;

namespace Parsing;

/// <summary>
/// Represents an item set or state row in the
/// parser action and goto table
/// </summary>

public class ParserState
{
    /// <summary>
    /// Verbose string representation
    /// of the parser state instance
    /// </summary>

    public string StateString
    {
        get;
        set;
    }

    private static readonly Regex briefStateRx = new
        (@"State (S\d+) CORE ITEMS:\r\n(.*)");

    /// <summary>
    /// Terse string representation
    /// of the parser state instance
    /// </summary>

    public string BriefStateString(MessageLevel level)
    {
        switch (level)
        {
            default:
            case MessageLevel.DEBUG:
                return StateString;

            case MessageLevel.VERBOSE:
                Match m = briefStateRx.Match(StateString);
                if (m != null && m.Groups.Count == 3)
                {
                    string body = m.Groups[2].Value;
                    int cutIdx = body.IndexOf("CLOSURE ITEMS");
                    if (cutIdx >= 0)
                        body = body[..cutIdx];
                    cutIdx = body.IndexOf("{\r\n");
                    if (cutIdx >= 0)
                        body = body[..cutIdx];
                    return m.Groups[1].Value + ":\r\n" + body;
                }
                else
                    return StateString;

            case MessageLevel.ERROR:
            case MessageLevel.WARN:
                m = briefStateRx.Match(StateString);
                if (m != null && m.Groups.Count == 3)
                    return m.Groups[1].Value;
                else
                    return StateString;
        }
    }

    // Generate the string output

    public override string ToString() => StateString;

    // List of columns, each with enough 
    // information to determine whether
    // to perform a SHIFT (stack push) or
    // a REDUCE (multi-item stack pop)
    // operation for a given input token
    // and its guard condition.

    public ParserStateColumn[] TerminalColumns
    {
        get;
        set;
    }

    // List of columns for each valid non-terminal
    // token that could be recognised from the
    // current item set. These are used for the
    // GOTO part of a reduction, meaning that once
    // states have been popped off the stack after
    // the reduce operation, the uncovered state
    // will have an element in this collection that
    // tells us which state to jump to next.

    public ParserStateColumn[] NonterminalColumns
    {
        get;
        set;
    }
}
