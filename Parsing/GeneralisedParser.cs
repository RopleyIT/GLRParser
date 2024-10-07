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
using System.IO;
using System.Linq;

namespace Parsing
{
    /// <summary>
    /// Implementation of the multi-stack used in
    /// GLR parsing. This is implemented as a stack
    /// in which there are multiple stack tops, each
    /// node in the stack can have multiple nodes
    /// beneath it representing multiple stacks each
    /// with the same top node, and each node can
    /// have multiple nodes above it.
    /// </summary>

    public class GeneralisedParser : IParser
    {
        /// <summary>
        /// Used to return a string description of
        /// the type of parser. This is used to
        /// avoid complicated factories and
        /// reflection when validating the type of
        /// parser being created in inline parsing.
        /// Since this parser is a generalised LR
        /// parser, the returned value is "GLR".
        /// </summary>

        public string ParserType => "GLR";

        // Handle to the input token or event stream

        private IEnumerator<IToken> tokenSource;

        /// <summary>
        /// The number of nodes to preallocate in the
        /// free list of stack nodes. This will resize
        /// as the number exceeds that value. Would be
        /// advisable to set this sufficiently high so
        /// that the stack does not need to reallocate
        /// too often while parsing.
        /// </summary>

        public const int NodeCapacity = 32;

        /// <summary>
        /// Used to set the approximate ratio of stack links to stack
        /// nodes. This is determined by how often two stacks within
        /// the GLR stack merge to the same state as the stack
        /// deepens. This integer is used in the following way.
        /// If the number of nodes requested is set to the value
        /// N, the number of links will be set to 
        /// N + (N >> ExtraLinkFraction).
        /// </summary>

        public const int ExtraLinkFraction = 3;

        // The two free list objects for the nodes
        // used within the GLR parser stacks

        private static FreeList<StackLink> linkFreeList;
        private static FreeList<StackNode> nodeFreeList;

        /// <summary>
        /// The tops of each of the multiple parser
        /// stacks currently viable in the input grammar
        /// </summary>

        public List<StackNode> StackTops
        {
            get;
            set;
        }

        /// <summary>
        /// If the debug stream exists, write a meaningful
        /// description of the set of stacks to the stream.
        /// </summary>
        /// <param name="headerMessage">Information to appear
        /// at the head of this stack dump</param>

        public void DumpStacks(string headerMessage)
        {
            MessageWrite(MessageLevel.DEBUG, $"{headerMessage}:\r\n");
            for (int i = 0; i < StackTops.Count; i++)
            {
                MessageWriteLine(MessageLevel.DEBUG, $"Stacktop {i}");
                MessageWrite(MessageLevel.DEBUG, $"{StackTops[i].AsString(Tokens, false, 1)}\r\n");
            }
        }

        /// <summary>
        /// Default constructor
        /// </summary>

        public GeneralisedParser()
        {
            // Make sure that a suitable number of spare stack
            // data structures are available for the parsers.
            // The algorithm assumes that for every node in
            // the stack, there are LinksPerNode links to nodes
            // lower in the trellis. This is  a tunable constant
            // at the top of this class.

            if (nodeFreeList == null)
            {
                nodeFreeList = new FreeList<StackNode>(NodeCapacity);
                linkFreeList = new FreeList<StackLink>
                    (NodeCapacity + (NodeCapacity >> ExtraLinkFraction));
            }
            StackTops = [];
            paths = new Queue<GeneralisedPath>();
            ParserResults = null;
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

        public void MessageWrite(MessageLevel errorLevel, string msg)
        {
            DebugStream?.Write(msg);
            if ((int)errorLevel <= (int)ErrorLevel && ErrStream != null)
                ErrStream.Write(msg);
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
            (MessageLevel errorLevel, string format, params object[] args) => MessageWrite(errorLevel, $"{format}\r\n", args);

        private ParserTable parserTable;
        private ParserStateColumn terminationColumn;

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
                // none of the late-bound guard and action
                // delegates will have been initialised

                if (!parserTable.Bound)
                    parserTable.Bind(GetType());

                // Wipe out previous runs of the parser

                StackTops = [];

                // Symbols used to terminate the parse.
                // Note that the use of short.MaxValue
                // as the next state index for

                eofToken = parserTable.Tokens["EOF"];
                terminationColumn = new ParserStateColumn
                    (Tokens["_Start"], null, false, short.MaxValue);

                // Begin the parse by injecting
                // the start token into the parser

                DoShifts(null, new ParserToken(parserTable.Tokens["SOF"], null, 0), StackTops);
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
            string tokPos = string.IsNullOrEmpty(tok.Position) ?
                    string.Empty
                    : (tok.Position + ": ");
            string tokVal = (tok.Value ?? "(null)").ToString();
            return $"{tokPos}{ParserTable.Tokens[tok.Type]} ({tokVal})";
        }

        // Quick access value for the end of input token

        private int eofToken;

        // The queue of paths through the trellis 
        // that are awaiting reduction

        private readonly Queue<GeneralisedPath> paths;

        /// <summary>
        /// Where the results of the parsing operation
        /// will be placed. Note that there may be
        /// several valid parser outputs from the
        /// grammar, hence the list. Each NonterminalToken
        /// contains a tree of productions, each with
        /// a list of ITokens (some of which will be
        /// further NonterminalTokens) for their right
        /// hand sides. Each top level token also has
        /// a Value property that will apply all the action
        /// functions for its entire parse in the right
        /// order.
        /// </summary>

        public IToken[] ParserResults
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
        /// Parse the next input token, assuming it is expected to
        /// not be in error. If an unrecognised input token is
        /// encountered, or some reduction action returns an error,
        /// this method returns false to indicate that all surviving
        /// parser stacks cannot continue to parse the input.
        /// </summary>
        /// <param name="token">The next input token</param>
        /// <returns>True if the token was recognised, false
        /// if there were no stacks for which the current
        /// input token was permitted for the state on
        /// the top of that stack.</returns>

        public bool ParseToken(IToken token)
        {
            if (token == null)
            {
                MessageWriteLine
                (
                    MessageLevel.VERBOSE,
                    "Null token passed to generalised parser"
                );
                return false;
            }

            // Debugging output

            MessageWriteLine
            (
                MessageLevel.VERBOSE,
                $"Parsing {TokenDescription(token)}"
            );

            // Flush out any old reduction paths, then
            // populate the reduction paths list with
            // any pending reductions that have to be
            // performed before we can shift the newly
            // arriving input token itok. These pending
            // reductions use the lookahead token itok
            // when identifying any potential reductions.

            paths.Clear();
            AddPaths(token, null);

            // Perform the actual reductions for each of
            // these reduction paths that have been found.
            // Note that we perform destructive removals
            // from the paths collection, and do not use
            // an iterator. This is because a later part
            // of this process appends extra paths to the
            // end of the list of paths so that recursive
            // reductions can take place.

            while (paths.Count > 0)
                ReduceViaPath(paths.Dequeue(), token);

            // Provide debugging output for the GLR parser

            if (DebugStream != null)
                DumpStacks($"Reducing when lookahead token is {TokenDescription(token)}");

            // Trap the termination condition for the parser

            if (token.Type == eofToken)
            {
                // Grab all the stack top nodes that are in
                // the terminated state, and pull their
                // parse trees from their non terminal
                // token objects.

                List<IToken> results = [];
                foreach (StackNode sn in StackTops.Where(n => n.State == short.MaxValue))
                    for (StackLink sl = sn; sl != null; sl = sl.Next)
                    {
                        IToken rootToken = GetTrueRootToken(sl);
                        if (rootToken != null)
                            results.Add(rootToken);
                    }
                ParserResults = [.. results];

                // Release any nodes and links from
                // the stacks back onto the free list

                foreach (StackNode node in StackTops)
                    ReleasePath(node);
                ParseComplete = true;

                // Debug output yielding results of parse

                if (ErrorLevel >= MessageLevel.DEBUG)
                {
                    MessageWriteLine(MessageLevel.DEBUG, "Parser results:");
                    foreach (IToken result in results)
                        MessageWriteLine(MessageLevel.DEBUG, result.AsString(Tokens, true, 0));
                }
                return ParserResults.Length > 0;
            }

            // Prepare the new top of stack collection ready
            // to carry out the shift of the current input
            // token onto the top of all stacks that this is
            // a valid shift symbol for.

            List<StackNode> newStackTops = [];

            // Now perform any shifts of input tokens that
            // are permitted for the current stack top states.
            // This also culls any paths through the stacks
            // for which this is not a valid next input token.

            foreach (StackNode topNode in StackTops)
                DoShifts(topNode, token, newStackTops);

            // Assign the new set of stack top nodes now that
            // all the shift and path termination operations
            // have been completed.

            StackTops = newStackTops;

            // Debugging output from GLR parser

            if (DebugStream != null)
                DumpStacks($"Shifting token {TokenDescription(token)}");

            // Check lest no path through the parser
            // stacks survived after this token

            if (StackTops.Count == 0)
            {
                MessageWriteLine(MessageLevel.DEBUG,
                    $"Parser halting - no shifts match token {TokenDescription(token)}");
                return false;
            }
            else
                return true;
        }

        /// <summary>
        /// Given a stack link that should be pointing to the
        /// _Start non terminal, obtain the true root non terminal
        /// in the rule: _Start: SOF trueRootNonterminal
        /// </summary>
        /// <param name="sl">The link to a stack node surviving
        /// at the end of a parse</param>
        /// <returns>The true root non terminal token for the grammar</returns>

        private static IToken GetTrueRootToken(StackLink sl)
        {
            if (sl.Token is NonterminalToken startToken && startToken.Children.Length == 2)
                return startToken.Children[1];
            else
                return null;
        }

        /// <summary>
        /// Having performed all the reductions on the various stack tops
        /// for a given lookahead input token, now perform the shift of
        /// that token for every stack top on which it is a valid shift
        /// token. Any stacks that it is not a valid shift token, remove 
        /// the corresponding stack back to where it branches from some
        /// other stack. Any shift that takes us to a state that is already
        /// on the top of another stack, merge the stack tops at that state.
        /// </summary>
        /// <param name="topNode">The specific top of stack
        /// we are applying the shift operation to</param>
        /// <param name="itok">The input token to be shifted</param>
        /// <param name="newStackTops">The list of stack tops 
        /// that will be the set of new stack tops once the
        /// shift operation has completed.</param>

        private void DoShifts(StackNode topNode, IToken itok, List<StackNode> newStackTops)
        {
            // Find the current state object from this top stack node.
            // The index into the state table for the current
            // state is the top integer on the parser stack.
            // For the state of the current stack top,
            // see if any reductions are specified for
            // the state at the top of the stack. Note
            // that if the top node is null, as occurs
            // at the very beginning of a parse when the
            // stack is empty, we default to state zero.
            // This state is the state that precedes any
            // input tokens having been parsed.

            int stateNum = 0;
            if (topNode != null)
                stateNum = topNode.State;
            ParserState pState = ParserTable.States[stateNum];

            // Find the set of columns of the current state for
            // which the input token matches the column's token
            // type, the action to be taken is a shift, and 
            // the guard condition returns true.

            ParserStateColumn shiftCol =
                pState.TerminalColumns.FirstOrDefault
                (
                    c => c.InputToken == itok.Type && !c.Reduce
                    && (c.Condition == null || c.Condition.Value(this, itok.Value))
                );

            if (shiftCol != null)
            {
                // Add an extra shifted state onto the current stack.
                // If the specified state is already on the new
                // list of stack tops, add an extra link to it.

                StackNode existingTopNode = newStackTops
                    .FirstOrDefault(n => n.State == shiftCol.Index);
                if (existingTopNode != null)
                {
                    // The node exists, so we just need to build an
                    // extra link from the existing top node.

                    StackLink newLink = linkFreeList.Alloc();
                    newLink.Next = existingTopNode.Next;
                    existingTopNode.Next = newLink;
                    newLink.Token = itok;
                    if (topNode == null)
                        newLink.TargetNode = null;
                    else
                        newLink.TargetNode = topNode.AddReference();
                }
                else
                {
                    StackNode newNode = nodeFreeList.Alloc();
                    newNode.Next = null;
                    newNode.ReferenceCount = 1;
                    newNode.State = shiftCol.Index;
                    newNode.Token = itok;
                    if (topNode == null)
                        newNode.TargetNode = null;
                    else
                        newNode.TargetNode = topNode.AddReference();
                    newStackTops.Add(newNode);
                }
            }
            else
            {
                // Attempt a release of stack node and stack link
                // objects on the current stack top, as it clearly
                // has no valid shift beyond this state for the
                // current input token.

                ReleasePath(topNode);
            }
        }

        /// <summary>
        /// Clean up paths through the tree that
        /// are no longer part of a surviving stack.
        /// Uses recursion to clean up nodes further
        /// along the data structure.
        /// </summary>
        /// <param name="node">The top of stack node
        /// that is to be freed.</param>

        public static void ReleasePath(StackNode node)
        {
            // Deal with the edge of the tree.
            // In theory this shouldn't happen.

            if (node == null)
                return;

            // Only release nodes to the free list
            // if there are no other nodes or
            // links in the stack pointing at them.

            if (--node.ReferenceCount <= 0)
            {
                StackLink link = node.Next;
                while (link != null)
                {
                    StackLink nextLink = link.Next;
                    ReleasePath(link.TargetNode);
                    linkFreeList.Free(link);
                    link = nextLink;
                }
                ReleasePath(node.TargetNode);
                nodeFreeList.Free(node);
            }
        }

        /// <summary>
        /// Search for reduction paths through the various
        /// stacks given the look-ahead token itok.
        /// </summary>
        /// <param name="itok">The next input token that
        /// has yet to be shifted.</param>
        /// <param name="linkRequiredInPath">Set to
        /// a link if only paths going via that
        /// link should be included in the added 
        /// paths. Set to null to include all
        /// matching paths.</param>

        private void AddPaths(IToken itok, StackLink linkRequiredInPath)
        {
            // First, using the input token as the lookahead
            // token, perform reductions on any stacks for
            // which a reduction rule is specified for the
            // input token.

            foreach (StackNode topNode in StackTops)
                AddPathsForState(topNode, itok, linkRequiredInPath);
        }

        /// <summary>
        /// Search for reduction paths for the specific stacks for
        /// which the node parameter is the top of those stacks.
        /// </summary>
        /// <param name="node">The top of stack node from
        /// which we seek reduction paths</param>
        /// <param name="itok">The look-ahead token</param>
        /// <param name="linkRequiredInPath">Set to
        /// a link if only paths going via that
        /// link should be included in the added 
        /// paths. Set to null to include all
        /// matching paths.</param>

        private void AddPathsForState
            (StackNode node, IToken itok, StackLink linkRequiredInPath)
        {
            // Find the current state object from this top stack node.
            // The index into the state table for the current
            // state is the top integer on the parser stack.
            // For the state of the current stack top,
            // see if any reductions are specified for
            // the state at the top of the stack.

            ParserState pState = ParserTable.States[node.State];

            // Find the set of columns of the current state for
            // which the input token matches the column's token
            // type, the action to be taken is a reduction,
            // and the guard condition returns true. In an
            // LR(1) parser, this will always return a single
            // column. For generalised parsers, there may be
            // multiple reductions for the same input token.

            IEnumerable<ParserStateColumn> cols =
                pState.TerminalColumns.Where
                (
                    c => c.InputToken == itok.Type && c.Reduce
                    && (c.Condition == null || c.Condition.Value(this, itok.Value))
                );

            // For each column found, find all the reduction paths through
            // the stack trellis from the specified top of stack node.

            foreach (ParserStateColumn psc in cols)
            {
                // Build up a list of paths through the stack that will need
                // to be reduced by their respective productions

                GeneralisedPath.AddGeneralisedPaths
                    (paths, ParserTable.Productions[psc.Index], node, linkRequiredInPath);
            }
        }

        // Perform reductions associated with a particular
        // path back through the trellis from a stack top

        private void ReduceViaPath(GeneralisedPath gp, IToken itok)
        {
            // Deal with the reduction action using the
            // values from the tokens in gp.TrellisPath,
            // which are the arguments to the action
            // function for the production gp.Production.
            // Note that in this parser we build a token
            // object that is capable of deferring the
            // execution of the action function until the
            // survivor path through the trellis is known.

            IEnumerable<IToken> pathTokens =
                from sl in gp.TrellisPath
                select sl.Token;

            NonterminalToken reductionToken
                = new(gp.Production, this, pathTokens);

            // Look up the non-terminal token that determines
            // what state to push back onto the stack. Now we
            // have popped the stack several times, the uncovered
            // top of stack is the state that needs to be
            // shifted by the non-terminal just reduced. In
            // common LR parser parlance, this is known as a GOTO.

            ParserState pState = null;
            ParserStateColumn gotoColumn = null;

            StackNode uncoveredNode;
            if (gp.TrellisPath.Length > 0)
                uncoveredNode = gp.TrellisPath[0].TargetNode;
            else
                uncoveredNode = gp.TopNode;

            if (uncoveredNode == null)
            {
                gotoColumn = terminationColumn;
            }
            else
            {
                pState = ParserTable
                    .States[uncoveredNode.State];
                gotoColumn = pState.NonterminalColumns.FirstOrDefault
                    (c => c.InputToken == gp.Production.NonterminalType);
            }
            if (gotoColumn == null)
            {
                // Handle an unrecognised rule reduction
                // in the grammar. We get here if a GOTO
                // has no state transition for the reduced
                // non-terminal symbol in the uncovered state.

                MessageWrite
                (
                    MessageLevel.DEBUG,
                    $"Reduction to {ParserTable.Tokens[gp.Production.NonterminalType]} has no " +
                    $"valid 'goto' from state {uncoveredNode.State}, " +
                    $"at line {TokenDescription(itok)}\r\n{pState}"
                );
                return;
            }
            else
            {
                // gotoColumn.Index is the new proposed state that
                // is to be pushed onto the stack after the reduction.
                // We have to explore to see if there is a stack top
                // with that state already among the stack tops.

                StackNode topNode = StackTops
                    .FirstOrDefault(n => n.State == gotoColumn.Index);

                // If there is no existing node in the list of top of
                // stack nodes that are in this state, we need to add
                // a new node to the top of stack list.

                if (topNode == null)
                {
                    StackNode newStackTop = nodeFreeList.Alloc();
                    newStackTop.ReferenceCount = 1;
                    newStackTop.Next = null;
                    newStackTop.State = gotoColumn.Index;
                    if (uncoveredNode == null)
                        newStackTop.TargetNode = null;
                    else
                        newStackTop.TargetNode
                            = uncoveredNode.AddReference();
                    newStackTop.Token = reductionToken;
                    StackTops.Add(newStackTop);

                    // This new node might also require reduction by
                    // further production rules. Note that when we
                    // reach the end of the parse, we don't bother
                    // to look for extra paths.

                    if (uncoveredNode != null)
                        AddPathsForState(newStackTop, itok, null);
                }

                // If there is already a stack top with this state, we
                // shall share the new pushed 'goto' node in that stack
                // top. There are two situations that have to be
                // considered here. First, if there is already a link
                // directly between the uncovered node and the found
                // top node, we need to give the parser user a chance 
                // to choose which of the two paths they wish to keep.
                // This could be to keep both paths and deal with the
                // preferred path later, or to drop one path now.

                else
                {
                    // Find out if there is a link between
                    // the new top of stack node and the
                    // node uncovered by the reduction.

                    StackLink directLink = FirstLinkOrDefault(topNode, uncoveredNode);

                    // If there is already a direct link, this is
                    // because there are two versions of exactly
                    // the same non-terminal at this point in the
                    // grammar parse. The user gets the option to
                    // choose which to keep at this point.
                    // If there is no direct link, just share the
                    // same top of stack node between two stacks,
                    // but link to the different descendents.

                    if (directLink == null)
                        LinkNodes(itok, reductionToken, uncoveredNode, topNode);
                    else
                    {
                        // Make a user callout to let them
                        // choose which of two paths to keep.
                        // One path given by (topNode, directLink, 
                        // uncoveredNode), the other by (topNode,
                        // new link based on gotoColumn.InputToken
                        // and itok, between topNode and uncoveredNode.
                        // First, build the arguments to the merge function.
                        // Slot 1 has the non-terminal token from
                        // the previously found reduction. Slot 2 has
                        // the one that has just been identified.
                        // The result will be returned from the merge
                        // function in slot 0 of the array. If non-null,
                        // it determines the actual survivor path from
                        // the two passed in as arguments. If null,
                        // both paths will be kept, and presumably
                        // one will be eliminated later in the parse.

                        NonterminalToken[] mergeArgs = new NonterminalToken[3];
                        mergeArgs[0] = null;
                        ParserMergeAction pma = null;
                        if (ParserTable.MergeActions != null
                            && ParserTable.MergeActions.TryGetValue(reductionToken.Type, out pma))
                        {
                            MessageWriteLine(MessageLevel.DEBUG,
                                $"Performing merge for non-terminal {Tokens[reductionToken.Type]}" +
                                $"between new reduction:\r\n{reductionToken.AsString(Tokens, false, 4)}");
                            MessageWriteLine(MessageLevel.DEBUG, "and existing reduction:\r\n" +
                                $"{(directLink.Token as NonterminalToken).AsString(Tokens, false, 4)}");

                            mergeArgs[1] = directLink.Token as NonterminalToken;
                            mergeArgs[2] = reductionToken;
                            pma.Function(this, mergeArgs);
                        }
                        else
                            MessageWriteLine(MessageLevel.DEBUG,
                                $"No merge function for non-terminal {Tokens[reductionToken.Type]}");

                        // If the merge function did not exist, or calling it
                        // did not cause it to make a choice between the two
                        // reductions to the non-terminal token, keep both paths.

                        if (mergeArgs[0] == null)
                        {
                            if (pma != null)
                                MessageWriteLine(MessageLevel.DEBUG, "Merge function kept both paths");
                            LinkNodes(itok, reductionToken, uncoveredNode, topNode);
                        }
                        else
                        {
                            if (pma != null)
                                MessageWriteLine(MessageLevel.DEBUG,
                                    $"Merge function selected:\r\n{mergeArgs[0].AsString(Tokens, false, 4)}");

                            // A choice was made between the two options, so
                            // only one of the paths needs to be preserved

                            directLink.Token = mergeArgs[0];
                        }
                    }
                }
            }
        }

        private void LinkNodes(IToken itok, NonterminalToken reductionToken, StackNode uncoveredNode, StackNode topNode)
        {
            // No direct link exists between the top
            // node and the uncovered node, so make one.

            StackLink newLink = linkFreeList.Alloc();
            newLink.Next = topNode.Next;
            if (uncoveredNode == null)
                newLink.TargetNode = null;
            else
                newLink.TargetNode
                    = uncoveredNode.AddReference();
            newLink.Token = reductionToken;
            topNode.Next = newLink;

            // This new link might open the opportunity
            // for further reductions in states whose
            // reductions have already been expanded. We
            // do need to avoid duplicate paths being
            // found. To do this, we only enqueue
            // reductions that explicitly traverse the
            // newly added link. The check on uncoveredNode
            // is to prevent looking for reductions at
            // the very end of a parse.

            if (uncoveredNode != null)
                AddPaths(itok, topNode.Next);
        }

        /// <summary>
        /// Ascertain whether there is a direct link between two stack nodes.
        /// If so, a reference to the link is returned. If not, null is returned.
        /// </summary>
        /// <param name="top">A node on a stack top from which a direct link
        /// to the lower node is sought.</param>
        /// <param name="uncovered">The node lower in the stack to which
        /// any found link would be referring.</param>
        /// <returns>The link that was found, or null if none found.</returns>

        private static StackLink FirstLinkOrDefault(StackNode top, StackNode uncovered)
        {
            for (StackLink foundLink = top; foundLink != null; foundLink = foundLink.Next)
                if (foundLink.TargetNode == uncovered)
                    return foundLink;
            return null;
        }

        /// <summary>
        /// Indicate whether the parser has reached a successful
        /// conclusion from parsing the stream of input tokens
        /// </summary>

        public bool ParseComplete
        {
            get;
            private set;
        }

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
            // Validate the input token stream handle

            if (source == null)
                throw new ArgumentNullException
                    (nameof(source), "Need an input token source to parse");

            // Validate the presence of parsing rule tables

            if (ParserTable == null)
                throw new InvalidOperationException
                    ("Attempting to parse without setting parser tables");

            // Get an iterator over the input token stream

            tokenSource = source.GetEnumerator();

            // Eat the input tokens one at a time, feeding
            // each token to the parsing engine

            while (!ParseComplete)
            {
                IToken itok;

                // If the input stream dries up, inject
                // the end of file token, and set a flag
                // to prevent the parse proceeding after
                // this end of file token.

                if (tokenSource.MoveNext() == false)
                    itok = new ParserToken(eofToken, null, 0);
                else
                    itok = tokenSource.Current;

                // A false value returned indicates that there
                // was an error in the parsing process

                if (!ParseToken(itok))
                    return false;
            }
            return true;
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
