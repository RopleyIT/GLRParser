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


namespace Parsing;

/// <summary>
/// Represents one of the grammar rules that can
/// be reduced while parsing the input
/// </summary>
/// <remarks>
/// Construct a parser production
/// </remarks>
/// <param name="tokenCount">Number of element on 
/// the parser stack to be popped, that will have 
/// their values passed to the reduction action 
/// function if it exists</param>
/// <param name="ptt">The non-terminal token type
/// that the rule reduces to</param>
/// <param name="action">List of inline delegates
/// to be executed when this reduction occurs</param>
/// <param name="productionRule">Debug string
/// that explains which production is reducing</param>
/// <param name="productionNumber">The unique integer
/// code associated with this production. Used for
/// reporting.</param>
/// <param name="ruleNumber">The zero-based index
/// into the grammar of the set of rules that share
/// the same LHS token.</param>

public class ParserProduction(
    short productionNumber,
    short ruleNumber,
    string productionRule,
    int tokenCount, int ptt,
    ParserInlineAction action
    )
{
    /// <summary>
    /// The unique numeric ID for the production.
    /// Used only for reporting purposes, as it
    /// matches the index into the array of
    /// ParserProduction objects in the ParserTable.
    /// </summary>

    public short Number
    {
        get;
        set;
    } = productionNumber;

    /// <summary>
    /// The index of this production into
    /// the set of productions that have the
    /// same LHS non terminal token. When
    /// reading the grammar, this is the
    /// zero-based index reading down the
    /// grammar, of the rule within the
    /// set of rules that have the same
    /// left hand side.
    /// </summary>

    public short RuleNumber
    {
        get;
        set;
    } = ruleNumber;

    /// <summary>
    /// The number of tokens on the RHS of the
    /// production, so that we know how many
    /// items to pop from the parser stack
    /// </summary>

    public byte TokenCount
    {
        get;
        private set;
    } = (byte)tokenCount;


    /// <summary>
    /// Non-terminal on LHS, that is used to select
    /// column from ParserState table on a GOTO
    /// following a reduction.
    /// </summary>

    public int NonterminalType
    {
        get;
        private set;
    } = ptt;

    /// <summary>
    /// If used, an action function to be called
    /// when the rule is reduced. This approach
    /// is used when parsing a grammar and then
    /// running the newly created grammar all in 
    /// the same process, thus avoiding code 
    /// generation and compilation.
    /// </summary>

    public ParserInlineAction InlineAction
    {
        get;
        private set;
    } = action;

    /// <summary>
    /// In verbose reporting, contains a string
    /// representation of the rule being reduced
    /// </summary>

    private readonly string productionString = productionRule;

    /// <summary>
    /// Render the object as a string
    /// </summary>
    /// <returns>A meaningful string representation 
    /// of the parser production</returns>

    public override string ToString() => ToString(false);

    /// <summary>
    /// Return a string representation of the
    /// production, with two levels of detail.
    /// </summary>
    /// <param name="verbose">True to include
    /// the production index number and the list
    /// of action functions. False to just
    /// return the grammar rule.</param>
    /// <returns>A string representation of
    /// the production</returns>

    public string ToString(bool verbose)
    {
        string result = string.Format
            ("R{0}: {1}", Number, productionString);
        if (verbose)
        {
            result += "\r\n{\r\n";
            if (InlineAction != null)
                result += "    " + InlineAction.ToString() + "\r\n";
            result += "}\r\n";
        }
        return result;
    }

    /// <summary>
    /// Construct a parser production
    /// </summary>
    /// <param name="tokenCount">Number of element on 
    /// the parser stack to be popped, that will have 
    /// their values passed to the reduction action 
    /// function if it exists</param>
    /// <param name="ptt">The non-terminal token type
    /// that the rule reduces to</param>
    /// <param name="productionRule">Debug string
    /// that explains which production is reducing</param>
    /// <param name="productionNumber">The unique integer
    /// code associated with this production. Used for
    /// reporting.</param>
    /// <param name="ruleNumber">The zero-based index
    /// into the grammar of the set of rules that share
    /// the same LHS token.</param>

    public ParserProduction(short productionNumber, short ruleNumber, string productionRule,
        int tokenCount, int ptt)
        : this(productionNumber, ruleNumber, productionRule, tokenCount, ptt, null)
    { }
}
