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

using ParserGenerator;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Parsing
{
    // A delegate type that points at either the
    // error-handling or non-error-handling
    // versions of the per-token parser function

    public delegate bool TokenParser(IToken tok);

    /// <summary>
    /// Simple structure used as one of 
    /// the elements in the parser stack
    /// </summary>

    public struct StackState
    {
        public int State;
        public IToken Value;
    }

    /// <summary>
    /// This is a representation of the actual parser
    /// that is at least part auto-generated by the 
    /// grammar parser generator that the rest of this
    /// application implements.
    /// </summary>

    public class Parser : IParser
    {
        /// <summary>
        /// Used to return a string description of
        /// the type of parser. This is used to
        /// avoid complicated factories and
        /// reflection when validating the type of
        /// parser being created in inline parsing.
        /// Since this parser is an LR(1) parser,
        /// the returned value is "LR1".
        /// </summary>

        public string ParserType => "LR1";

        private IEnumerator<IToken> tokenSource;

        /// <summary>
        /// Default constructor. ParserTable must be
        /// initialised separately before use.
        /// </summary>

        public Parser()
        {
            parserStateStack = new Stack<StackState>();
            ErrorLevel = MessageLevel.WARN;
        }

        /// <summary>
        /// Stream used for verbose parser reporting. Use
        /// this to track the parser's progress through
        /// the input grammar. Set/leave it as null to
        /// just not report this kind of information.
        /// </summary>

        public TextWriter DebugStream
        {
            get;
            set;
        }

        /// <summary>
        /// Stream used for normal parser reporting.
        /// Use this to see error messages output by
        /// the parser. Set/leave it as null to just
        /// not report this kind of information.
        /// </summary>

        public TextWriter ErrStream
        {
            get;
            set;
        }

        /// <summary>
        /// Set the level of verbosity for reporting
        /// parser progress and errors
        /// </summary>

        public MessageLevel ErrorLevel
        {
            get;
            set;
        }

        /// <summary>
        /// Helper method for outputting text to
        /// the debug and error output streams.
        /// Has formatting behaviour compatible
        /// with TextWriter.Write(format, args)
        /// </summary>
        /// <param name="errorLevel">Level to choose when
        ///  writing error messages</param>
        /// <param name="format">Format argument, obeying
        /// string.Format syntax</param>
        /// <param name="args">Arguments as used by the
        /// format argument</param>

        public void MessageWrite
            (MessageLevel errorLevel, string format, params object[] args)
        {
            DebugStream?.Write(format, args);
            if ((int)errorLevel <= (int)ErrorLevel && ErrStream != null)
                ErrStream.Write(format, args);
        }

        /// <summary>
        /// Helper method for outputting text to
        /// the debug and error output streams.
        /// Has formatting behaviour compatible
        /// with TextWriter.WriteLine(format, args)
        /// </summary>
        /// <param name="errorLevel">True to write to the error
        /// stream as well as the debug stream</param>
        /// <param name="format">Format argument, obeying
        /// string.Format syntax</param>
        /// <param name="args">Arguments as used by the
        /// format argument</param>

        public void MessageWriteLine
            (MessageLevel errorLevel, string format, params object[] args) => MessageWrite(errorLevel, format + "\r\n", args);

        // The state of the LR(1) parser. At any one
        // moment in time, this holds the list of
        // item set numbers that are part way through
        // being parsed. These item set numbers are the
        // states for each of the nested state machines
        // that correspond to the current rules being
        // parsed. The values returned by each terminal
        // token or reduced non-terminal parsed are also
        // to be found in each element. A SHIFT operation
        // pushes an item set number and token. A REDUCE
        // operation pops the number of elements from
        // the stack that there are in the RHS of
        // the rule associated with the REDUCE action,
        // then pushes a single item onto the stack
        // for the non-terminal at the LHS of the rule.

        private readonly Stack<StackState> parserStateStack;

        private ParserTable parserTable;

        /// <summary>
        /// The set of item sets, the productions,
        /// the token lookup table, and the action
        /// functions associated with the grammar.
        /// </summary>

        public ParserTable ParserTable
        {
            get => parserTable;
            set
            {
                if (value == null)
                    return;

                if (value.Tokens == null)
                    throw new ArgumentException
                        ("Badly formed parser tables passed to parser");

                // If the parser table is changing,
                // we shall launch a new parser

                parserTable = value;

                // Ensure the debugging output from the
                // parser state columns works correctly.
                // This really should not be static, but
                // will do for now.

                ParserStateColumn.Table = value;

                // If this is a newly attached parser table,
                // the late-bound guard and action
                // delegates may not have been initialised

                if (!parserTable.Bound)
                    parserTable.Bind(GetType());

                // Wire up the correct parser algorithm

                if (parserTable.ErrorRecoveryEnabled)
                    selectedTokenParser = ParseTokenWithErrorHandling;
                else
                    selectedTokenParser = ParseTokenFirstErrorHalts;

                // Wipe out previous runs of the parser

                parserStateStack.Clear();

                // Regularly reused symbol

                eofToken = parserTable.Tokens["EOF"];

                // Begin the parse by injecting
                // the start token into the parser

                ParseTokenWithErrorHandling(new ParserToken(parserTable.Tokens["SOF"], null, 0));
            }
        }

        /// <summary>
        /// Two way lookup of token names and
        /// their integer values
        /// </summary>

        public TwoWayMap<string, int> Tokens
        {
            get
            {
                if (ParserTable != null)
                    return ParserTable.Tokens;
                else
                    return null;
            }
        }

        /// <summary>
        /// Prepare a readable representation of an input token
        /// </summary>
        /// <param name="tok">The token</param>
        /// <returns>A string representation of the token</returns>

        public string TokenDescription(IToken tok)
        {
            if (tok == null)
                return string.Empty;

            return string.Format
            (
                "{0} {1} ({2})",
                string.IsNullOrEmpty(tok.Position) ?
                    string.Empty
                    : (tok.Position + ": "),
                ParserTable.Tokens[tok.Type],
                (tok.Value ?? "(null)").ToString()
            );
        }

        // Quick access value for the end of input token

        private int eofToken;

        /// <summary>
        /// Reference to the method that parses the
        /// input one token at a time. Two options
        /// are available, depending on whether the
        /// parser supports yacc/bison style error
        /// recovery, or alternatively halts the parse
        /// at the first error.
        /// </summary>

        private TokenParser selectedTokenParser;

        /// <summary>
        /// Single token input to parser
        /// </summary>
        /// <param name="token">The token to be parsed</param>
        /// <returns>True if the parse advances the parser
        /// successfully</returns>

        public bool ParseToken(IToken token) => selectedTokenParser(token);

        // Flag to remember whether any errors were
        // encountered while parsing. Used to prevent
        // a parser that skipped errors from succeeding.

        private bool errorsRecovered = false;

        // Error recovery flag, used when stepping over
        // input tokens up to a known restarting token.

        private bool skippingInputTokens = false;

        /// <summary>
        /// Feed one token into the parser, and cause the
        /// parser to advance based on this token. This
        /// can involve zero or more reductions, followed
        /// by a single shift operation. This method
        /// also handles error recovery using the
        /// unravelling of the state stack until a known
        /// error catch point, then discarding input
        /// tokens until the expected next token
        /// appears in the input. To run the parser
        /// without this error recovery mechanism, and
        /// to just have it fail when an incorrect
        /// input token is encountered, use the method
        /// ParseNextToken instead.
        /// </summary>
        /// <param name="itok">The input token that will
        /// cause the advance of the parser.</param>
        /// <returns>True if there were no errors.</returns>

        private bool ParseTokenWithErrorHandling(IToken itok)
        {
            // All the time we are performing normal
            // token handling, skippingInputTokens will
            // be false and ParseNextToken will succeed.
            // When an error is encountered, ParseNextToken()
            // will return false and cause us to pop items off
            // the state and value stacks until we spot the
            // valid follow-on token is an ERR token. At
            // that point we perform a SHIFT so we can see
            // what the expected terminal token is after
            // the error token.

            if (!skippingInputTokens && !ParseTokenFirstErrorHalts(itok))
            {
                errorsRecovered = true;
                if (PopStackUntilErrorTokenShifted())
                    skippingInputTokens = true;
                else
                    return false;
            }

            // This flag is set once we have stepped past the
            // error token, and are throwing away input tokens
            // until we find a token that matches the terminal
            // token expected immediately after the error token.

            if (skippingInputTokens)
            {
                if (ParseComplete)
                    return false;

                // Find the current state object for the parser.
                // The index into the state table for the current
                // state is the top integer on the parser stack.

                ParserState pState = ParserTable.States[parserStateStack.Peek().State];

                // Walk along the columns of the current state until
                // the input token matches the column's token type,
                // and the guard condition functions return true

                if (pState.TerminalColumns.Any
                    (c => c.InputToken == itok.Type
                        && (c.Condition == null || c.Condition.Value(this, itok.Value))))
                {
                    skippingInputTokens = false;
                    return ParseTokenWithErrorHandling(itok);
                }
                else
                    MessageWrite(MessageLevel.VERBOSE,
                        "Token {0} skipped by error recovery\r\n", TokenDescription(itok));
            }
            return true;
        }

        /// <summary>
        /// Given that we have just encountered an input token that
        /// is not valid at this point in the grammar, we are going
        /// to remove states from the parser stack until we uncover
        /// a state for which the ERR error token is a valid next
        /// input token. When encountered, we shall shift over the
        /// error token to a state that has an expected terminal
        /// as its next input token. The caller of this method then
        /// flushes input tokens until it finds one that matches
        /// the expected terminal token.
        /// </summary>
        /// <returns>True if an error token was found closer
        /// to the root of the grammar so far. False if the
        /// stack was completely emptied looking for the
        /// error token. In this case, the parse ends as the
        /// error was unrecoverable.</returns>

        private bool PopStackUntilErrorTokenShifted()
        {
            ParserToken errToken = new(Grammar.ErrorTokenValue, null, 0);
            parserStateStack.Pop();
            while (!ParseComplete)
            {
                ParserState pState = ParserTable.States[parserStateStack.Peek().State];
                MessageWrite(MessageLevel.VERBOSE,
                        "Error recovery pops stack, uncovering {0}",
                        pState.BriefStateString(ErrorLevel));
                if (ParseTokenFirstErrorHalts(errToken))
                    return true;
                else
                    parserStateStack.Pop();
            }
            return false;
        }

        /// <summary>
        /// The production being reduced while an action function is executing
        /// </summary>

        public ParserProduction ReducingRule
        {
            get;
            private set;
        }

        /// <summary>
        /// The most recent parser error returned
        /// by an action function in a rule reduction
        /// </summary>

        public ParserError ActionError
        {
            get;
            set;
        }

        /// <summary>
        /// Create the error object for a non-fatal
        /// error discovered while executing an
        /// action function on rule reduction.
        /// Non-fatal errors should be recoverable
        /// if parser error recovery is enabled.
        /// </summary>
        /// <param name="errorMessage">The information
        /// describing the nature of the error.</param>

        public void Warn(string errorMessage) => ActionError =
                new ParserError()
                {
                    Fatal = false,
                    Message = errorMessage
                };

        /// <summary>
        /// Create the error object for a fatal
        /// error discovered while executing an
        /// action function on rule reduction.
        /// Fatal errors are not recoverable, even
        /// if parser error recovery is enabled.
        /// </summary>
        /// <param name="errorMessage">The information
        /// describing the nature of the error.</param>

        public void Error(string errorMessage) => ActionError =
                new ParserError()
                {
                    Fatal = true,
                    Message = errorMessage
                };

        /// <summary>
        /// The array of Position properties for the tokens in
        /// a rule reduction. This is filled in just before an
        /// action function is executed, and is intended to be
        /// used as a read only resource.
        /// </summary>

        public string[] TokenPosition
        {
            get;
            private set;
        }

        /// <summary>
        /// Parse the next input token, assuming it is expected to
        /// not be in error. If an unrecognised input token is
        /// encountered, or some reduction action returns an error,
        /// this method returns false to indicate that error
        /// recovery should take place.
        /// </summary>
        /// <param name="itok">The next input token</param>
        /// <returns>True if the token was recognised, false
        /// if there was a grammar error recognising this token.</returns>

        private bool ParseTokenFirstErrorHalts(IToken itok)
        {
            // Debugging output

            MessageWriteLine
            (
                MessageLevel.VERBOSE,
                "Parsing {0}",
                TokenDescription(itok)
            );

            // It is assumed that we are in a position to handle
            // the input token immediately. First we have to deal
            // with any reductions where this is the lookahead token

            ParserStateColumn psc = null;
            bool reduced = false;
            while (!reduced)
            {
                // Find the current state object for the parser.
                // The index into the state table for the current
                // state is the top integer on the parser stack.
                // The test for the empty parser stack is only
                // going to be true at the very beginning of a
                // parse, when no tokens have been encountered.

                int stateNum = 0;
                if (parserStateStack.Count > 0)
                    stateNum = parserStateStack.Peek().State;
                ParserState pState = ParserTable.States[stateNum];

                // Walk along the columns of the current state until
                // the input token matches the column's token type,
                // and the guard condition functions return true

                psc = pState.TerminalColumns.FirstOrDefault
                    (c => c.InputToken == itok.Type
                        && (c.Condition == null || c.Condition.Value(this, itok.Value)));

                // Check that this input symbol is valid for this state in the grammar

                if (psc == null)
                {
                    // No terminal token plus guard condition that
                    // matches the input token was found in the
                    // current state. This means that the current
                    // input token must be illegal for the
                    // current state. In this case we should report
                    // an error in the parse, and try to reset.

                    MessageWrite
                    (
                        MessageLevel.VERBOSE,
                        "Token {0} matches no shift or reduce rule for state {1}\r\n{2}",
                        TokenDescription(itok),
                        parserStateStack.Peek().State,
                        ParserTable.States[parserStateStack.Peek().State].BriefStateString(ErrorLevel)
                    );
                    return false;
                }

                // Deal with reductions

                if (psc.Reduce)
                {
                    // If Reduce is true, we have reached
                    // the end of a grammar rule, meaning
                    // we should reduce the items on the
                    // stack. Find the production rule
                    // that is just about to be reduced.

                    ReducingRule = ParserTable.Productions[psc.Index];

                    // Output optional debugging information

                    MessageWriteLine
                    (
                        MessageLevel.VERBOSE,
                        "Reducing by {0} on token: {1}{2}",
                        ReducingRule.ToString(true),
                        Tokens[psc.InputToken],
                        psc.Condition != null ?
                            "[" + psc.Condition.AsString() + "]"
                            : string.Empty
                    );

                    // Pop the right number of items off the
                    // two stacks, taking care to put the
                    // values into an array to be passed to the
                    // action function associated with this
                    // rule's completion. Note that slot zero
                    // of the array is reserved for the return value.

                    object[] args = new object[ReducingRule.TokenCount + 1];

                    // Allocate space to store the position strings from
                    // each of the ITokens in this reducing rule

                    TokenPosition = new string[ReducingRule.TokenCount];

                    // Copy the tokens for the rule being reduced across
                    // to the array of arguments to the action function

                    for (int i = ReducingRule.TokenCount; i > 0; i--)
                    {
                        IToken tok = parserStateStack.Pop().Value;
                        TokenPosition[i - 1] = tok.Position;
                        args[i] = tok.Value;
                    }

                    // Default is that the 'return' object from the
                    // reduction is null. It is the responsibility
                    // of the action function to put a value into '$$'.

                    args[0] = null;

                    ParserInlineAction currAction = ReducingRule.InlineAction;
                    if (currAction != null)
                    {
                        // Debug output

                        if (ErrorLevel >= MessageLevel.DEBUG)
                        {
                            MessageWrite(MessageLevel.DEBUG, "Inline action: {0}(",
                                currAction.MethodName);
                            for (int i = 0; i < args.Length; i++)
                            {
                                if (i > 0)
                                    MessageWrite(MessageLevel.DEBUG, ", ");
                                MessageWrite(MessageLevel.DEBUG, "{0}", (args[i] ?? "(null)").ToString());
                            }
                            MessageWriteLine(MessageLevel.DEBUG, ")");
                        }

                        ActionError = null;

                        // Now execute the action method

                        currAction.Function(this, args);

                        // Rudimentary error handling based on return
                        // data from user action functions. This should
                        // be beefed up to support error recovery by
                        // popping the parser stack back to a known
                        // error token in a rule, and discarding
                        // input tokens until a matched token.

                        if (ActionError != null)
                        {
                            if (ErrorLevel <= MessageLevel.WARN)
                                MessageWriteLine
                                (
                                    MessageLevel.WARN,
                                    "{0} at line {1}",
                                    ActionError.Message,
                                    TokenDescription(itok)
                                );
                            else
                                MessageWriteLine
                                (
                                    MessageLevel.VERBOSE,
                                    "{0} when reducing {1}, line {2}",
                                    ActionError.Message,
                                    ReducingRule.ToString(false),
                                    TokenDescription(itok)
                                );
                            return !ActionError.Fatal;
                        }
                    }

                    // At this point we have popped the stack following
                    // a reduction, but have not yet pushed the GOTO
                    // non-terminal and its computed action value back
                    // onto the stack. If the stack is empty at this
                    // point, we have no state to use to find the
                    // GOTO state. This either means the parse has
                    // failed following wrong input, or if the look
                    // ahead token is EOF, that the parse has
                    // finished successfully. This corresponds to
                    // the accept transition in the parser.

                    if (parserStateStack.Count == 0)
                        return itok.Type == eofToken;

                    // Look up the non-terminal token that determines
                    // what state to push back onto the stack. Now we
                    // have popped the stack several times, the uncovered
                    // top of stack is the state that needs to be
                    // shifted by the non-terminal just reduced. In
                    // common LR parser parlance, this is known as a GOTO.

                    pState = ParserTable.States[parserStateStack.Peek().State];
                    ParserStateColumn gotoColumn =
                        pState.NonterminalColumns.FirstOrDefault
                        (c => c.InputToken == ReducingRule.NonterminalType);

                    if (gotoColumn != null)
                    {
                        parserStateStack.Push
                        (
                            new StackState
                            {
                                State = gotoColumn.Index,
                                Value = new ParserToken
                                (
                                    ReducingRule.NonterminalType,
                                    args[0],
                                    TokenPosition.FirstOrDefault()
                                )
                            }
                        );

                        // Output optional debugging information

                        MessageWriteLine
                        (
                            MessageLevel.VERBOSE,
                            "Goto state {0}\r\n{1}",
                            gotoColumn.Index,
                            ParserTable.States[gotoColumn.Index].BriefStateString(ErrorLevel)
                        );
                    }
                    else
                    {
                        // Handle an unrecognised rule reduction
                        // in the grammar. We get here if a GOTO
                        // has no state transition for the reduced
                        // non-terminal symbol in the uncovered state.

                        MessageWriteLine
                        (
                            MessageLevel.VERBOSE,
                            "Reduction to {1} " +
                            "has no valid 'goto' from state {2}, " +
                            "at line {0}\r\n{3}",
                            TokenDescription(itok),
                            ParserTable.Tokens[ReducingRule.NonterminalType],
                            parserStateStack.Peek().State,
                            ParserTable.States[parserStateStack.Peek().State]
                        );
                        return false;
                    }
                }
                else
                    // Set reduced flag to indicate no more
                    // reductions needed before the shift of
                    // the pending input token. This causes
                    // a drop through to the next clause

                    reduced = true;
            }

            // For shifts, we just push the next
            // state number onto the parser stack,
            // and its corresponding token and value
            // onto the value stack.

            parserStateStack.Push
            (
                new StackState
                {
                    State = psc.Index,
                    Value = itok
                }
            );

            // Output optional debugging information

            MessageWrite
            (
                MessageLevel.VERBOSE,
                "Token {0}{1} causes shift to state {2}\r\n{3}",
                Tokens[psc.InputToken],
                psc.Condition != null ?
                    "[" + psc.Condition.AsString() + "]"
                    : string.Empty,
                psc.Index,
                ParserTable.States[psc.Index].BriefStateString(ErrorLevel)
            );
            return true;
        }

        /// <summary>
        /// Return true if the parser stack is
        /// empty because the parse is complete
        /// </summary>

        public bool ParseComplete => parserStateStack.Count == 0;

        /// <summary>
        /// Run the grammar parser over the input enumerable
        /// token stream, and execute the LR(1) state machine
        /// based on the input sequence.
        /// </summary>
        /// <param name="source">The input source from
        /// which to read tokens</param>
        /// <returns>True if the input token sequence
        /// conformed to the state machine rules, 
        /// false if not.</returns>

        public bool Parse(IEnumerable<IToken> source)
        {
            bool overrun = false;

            // Validate the input token stream handle

            if (source == null)
            {
                MessageWriteLine(MessageLevel.ERROR, "Parser.Parse() invoked with no token source argument");
                return false;
            }

            // Validate the presence of parsing rule tables

            if (ParserTable == null)
            {
                MessageWriteLine(MessageLevel.ERROR, "Attempting to parse without setting parser tables");
                return false;
            }

            // Get an iterator over the input token stream

            tokenSource = source.GetEnumerator();

            // Eat the input tokens one at a time, feeding
            // each token to the parsing engine

            while (!ParseComplete)
            {
                IToken itok;

                // Check for multiple EOF tokens being
                // consumed at the end of a parse. Usually
                // caused by badly structured error
                // recovery rules in grammar.

                if (overrun)
                    return false;

                // If the input stream dries up, inject
                // the end of file token, and set a flag
                // to prevent the parse proceeding after
                // this end of file token.

                if (tokenSource.MoveNext() == false)
                {
                    overrun = true;
                    itok = new ParserToken(eofToken, null, 0);
                }
                else
                    itok = tokenSource.Current;

                // A false value returned indicates that there
                // was an error in the parsing process

                if (!ParseToken(itok))
                    return false;
            }
            return !errorsRecovered;
        }

        /// <summary>
        /// Helper function to wrap a multiplicity
        /// argument in a strongly typed collection
        /// </summary>
        /// <typeparam name="T">The type of each of the values
        /// returned by the child rules in a multiplicity</typeparam>
        /// <param name="arg">The argument in the parent rule
        /// that is being treated as a multiple symbol, i.e.
        /// one that is followed by ?, * or + in the grammar.</param>
        /// <returns>The argument as a strongly typed list.</returns>

        public static IList<T> AsList<T>(object arg) => new ListArg<T>(arg);

        /// <summary>
        /// Helper function to wrap an optional multiplicity
        /// argument in a strongly typed class.
        /// </summary>
        /// <typeparam name="T">The type of the optional item 
        /// held by the class.</typeparam>
        /// <param name="arg">The value from the parent rule
        /// that is being treated as having optional multiplicity.</param>
        /// <returns>A reference to an interface that
        /// has a 'HasValue property and a Value property for
        /// the optional item held in the class.</returns>

        public static IOptional<T> AsOptional<T>(object arg) => new ListArg<T>(arg);
    }
}