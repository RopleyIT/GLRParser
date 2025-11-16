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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ParserGenerator;

/// <summary>
/// An instance of this class accumulates the
/// entire parsed grammar, and processes it to
/// construct the automaton.
/// </summary>

public class Grammar
{
    /// <summary>
    /// Named constants used for defining ranges
    /// of values allocated to terminal and
    /// non-terminal tokens.
    /// </summary>

    public const int UnassignedTokenValue = -1;
    public const int MinUserTokenValue = 0;
    public const int EmptyTokenValue = 16384;
    public const int MaxUserTokenValue = EmptyTokenValue - 1;
    public const int EOFTokenValue = EmptyTokenValue + 1;
    public const int SOFTokenValue = EmptyTokenValue + 2;
    public const int ErrorTokenValue = EmptyTokenValue + 3;
    public const int MinAutoTokenValue = EmptyTokenValue + 4;
    public const int MinNonterminalValue = 32768;
    public const int MaxAutoTokenValue = MinNonterminalValue - 1;
    public const int MinGuardValue = 49152;

    /// <summary>
    /// Optional data to be passed across to the parser
    /// generator when rendering takes place
    /// </summary>

    public Dictionary<string, object> Options
    {
        get;
        private set;
    }

    /// <summary>
    /// The set of known terminal tokens recognised
    /// by the grammar
    /// </summary>

    public List<GrammarToken> Terminals
    {
        get;
        private set;
    }

    /// <summary>
    /// The list of non-terminal tokens for which
    /// productions exist within the grammar
    /// </summary>

    public List<GrammarToken> Nonterminals
    {
        get;
        private set;
    }

    /// <summary>
    /// The sparsely populated list of actions
    /// that handle merges between grammar
    /// reductions in a GLR parser. When there
    /// are two valid reductions to a non-terminal
    /// in the input grammar, the merge function
    /// is called to decide which one to keep
    /// and which one to discard.
    /// </summary>

    public Dictionary<string, GrammarGuardOrAction> MergeActions
    {
        get;
        private set;
    }

    /// <summary>
    /// The list of defined guard conditions
    /// defined for use in the grammar
    /// </summary>

    public List<GrammarToken> Guards
    {
        get;
        private set;
    }

    /// <summary>
    /// The list of function bodies for
    /// guard functions
    /// </summary>

    public List<GrammarGuardOrAction> GuardBodies
    {
        get;
        private set;
    }

    /// <summary>
    /// A cache that ensures equivalent boolean
    /// expressions used within the grammar only
    /// appear once within the cache.
    /// </summary>

    public ExpressionCache BooleanExpressionCache
    {
        get;
        private set;
    }

    /// <summary>
    /// The list of productions captured from the grammar
    /// </summary>

    public List<GrammarProduction> Productions
    {
        get;
        private set;
    }

    /// <summary>
    /// The item sets used to construct the LR(1) parser
    /// </summary>

    public List<GrammarItemSet> ItemSets
    {
        get;
        private set;
    }

    /// <summary>
    /// The token representing the empty set, as
    /// used in First and Follow sets. The
    /// empty Text property is reserved for the
    /// empty token.
    /// </summary>

    public GrammarElement EmptyElement
    {
        get;
        private set;
    }

    /// <summary>
    /// The non-terminal symbol that is the
    /// top level rule of the grammar. When
    /// this symbol has been recognised after
    /// reduction, the whole input grammar
    /// has been recognised.
    /// </summary>

    public GrammarToken StartingNonterminal
    {
        get;
        private set;
    }

    /// <summary>
    /// The terminal symbol that represents
    /// the end of input to the parser generator
    /// </summary>

    public GrammarElement EndingElement
    {
        get;
        private set;
    }

    /// <summary>
    /// The terminal symbol that represents
    /// the start of input to the parser generator
    /// before any of the user's input tokens
    /// are captured.
    /// </summary>

    public GrammarElement StartingElement
    {
        get;
        private set;
    }

    /// <summary>
    /// The production with "_Start" as its
    /// LHS, that identifies the real entry
    /// token for the grammar.
    /// </summary>

    public GrammarProduction RootProduction
    {
        get;
        private set;
    }

    /// <summary>
    /// Sets the entry symbol for the grammar, while
    /// performing a check that the entry symbol with
    /// the specified name exists among the non-terminals.
    /// </summary>
    /// <param name="nonTermName">The name of the non-terminal
    /// that is to become the entry point for the grammar</param>
    /// <returns>True if successful, false if a non-terminal
    /// with the specified name is not found.</returns>

    public bool SetStartingNonterminal(string nonTermName)
    {
        StartingNonterminal = Nonterminals
            .FirstOrDefault(nt => nt.Text == nonTermName);
        return StartingNonterminal != null;
    }

    /// <summary>
    /// Constructor. Initialises empty data structures
    /// </summary>

    public Grammar()
    {
        Options = [];
        Terminals = [];
        Nonterminals = [];
        MergeActions = [];
        Guards = [];
        GuardBodies = [];
        BooleanExpressionCache = new ExpressionCache();
        Productions = [];
        ItemSets = [];
        EmptyElement =
            new GrammarElement(new GrammarToken
                (EmptyTokenValue, TokenType.Terminal, string.Empty), null);

        // Simple state machine setup

        MachineStates = [];
        StartingState = null;
    }

    /// <summary>
    /// Check that the non-terminals appear to the left
    /// of and to the right of every production. Also
    /// add an extra starting symbol to form the extended
    /// context free grammar, as required by LR parsers.
    /// </summary>
    /// <returns>An error message if the grammar
    /// does not validate. An empty string if the
    /// grammar validates OK.</returns>

    private string InitializeAndValidateGrammar()
    {
        string err = InitializeGrammarTokens();
        InitializeRules();
        if (string.IsNullOrEmpty(err))
            err = ValidateGrammar();
        return err;
    }

    /// <summary>
    /// Set up the fixed predefined tokens
    /// needed to make the guarded
    /// parser start up and terminate
    /// </summary>
    /// <returns>An error message if the grammar
    /// does not initialise. An empty string if the
    /// grammar initialises OK.</returns>

    private string InitializeGrammarTokens()
    {
        // Just check that nobody has been daft
        // enough to add a grammar rule with the
        // same name as the synthetic start symbol.

        string err = ValidateToken("_Start");
        if (!string.IsNullOrEmpty(err))
            return err;

        // Just check that nobody has been daft
        // enough to add a grammar rule with the
        // same name as the synthetic end token.

        err = ValidateToken("EOF");
        if (!string.IsNullOrEmpty(err))
            return err;

        // Insert the synthetic end of input symbol into
        // the list of terminal tokens recognised by the
        // parser

        GrammarToken endingToken
            = new(EOFTokenValue, TokenType.Terminal, "EOF");
        EndingElement = new GrammarElement
            (endingToken, null);
        Terminals.Add(endingToken);

        // Just check that nobody has been daft
        // enough to add a grammar rule with the
        // same name as the synthetic start token.

        err = ValidateToken("SOF");
        if (!string.IsNullOrEmpty(err))
            return err;

        // Insert the synthetic start of input symbol into
        // the list of terminal tokens recognised by the
        // parser

        GrammarToken startingToken = new
            (SOFTokenValue, TokenType.Terminal, "SOF");
        StartingElement = new GrammarElement
            (startingToken, null);
        Terminals.Add(startingToken);

        // Fixed token instertion completed successfully

        return string.Empty;
    }

    /// <summary>
    /// Check a token name is not already used by the parser
    /// </summary>
    /// <param name="tokName">Token name to check</param>
    /// <returns>Error message or empty string if no error</returns>

    private string ValidateToken(string tokName)
    {
        if (Terminals.Exists(t => t.Text == tokName)
            || Nonterminals.Exists(t => t.Text == tokName)
            || Guards.Exists(t => t.Text == tokName))
            return $"The identifier '{tokName}' is used internally "
                + "by the parser. Please change this symbol name.";
        else
            return string.Empty;

    }

    /// <summary>
    /// Set up the additional grammar rules
    /// required by the guarded parser
    /// </summary>

    private void InitializeRules()
    {
        // Inject one extra rule as the root symbol
        // of the grammar. This is because the root
        // symbol must not appear anywhere on the RHS
        // of any grammar rule. This rule has the
        // form:
        //
        // _Start: SOF startingNonterminal
        //         {
        //             $$ = $1;
        //         };
        //
        // where "startingNonterminal" is whichever
        // non-terminal token was identified as the
        // argument to the grammar(rootToken)
        // part of the input grammar.

        List<GrammarElement> startGel =
        [
            StartingElement,
            new GrammarElement(StartingNonterminal, null)
        ];
        RootProduction = new GrammarProduction(this, 0, startGel)
        {
            LHS = new GrammarToken(MinNonterminalValue, TokenType.Nonterminal, "_Start")
        };
        Nonterminals.Add(RootProduction.LHS);
        StartingNonterminal = RootProduction.LHS;
        Productions.Add(RootProduction);
    }

    /// <summary>
    /// Perform grammar consistency checks
    /// </summary>
    /// <returns>An error message if the grammar
    /// does not validate. An empty string if the
    /// grammar validates OK.</returns>

    private string ValidateGrammar()
    {
        // Perform grammar consistency checks

        foreach (GrammarToken gt in Nonterminals)
        {
            // Skip the synthetic starting symbol

            if (gt.Text == "_Start")
                continue;

            // Check that every non-terminal listed in the grammar
            // that appears to the right of a grammar rule also appears
            // at the left of a rule.

            if (!Productions.Exists(p => p.LHS == gt))
                return $"Non-terminal {gt.Text}"
                        + " never appears to the left of a production";

            // Check that every non-terminal except the synthetic
            // start symbol is referred to within a production

            bool used = false;
            foreach (GrammarProduction p in Productions)
            {
                if (p.RHS.Any(e => e.Token == gt))
                {
                    used = true;
                    break;
                }
            }

            if (!used)
                return $"Non-terminal {gt.Text}"
                    + " is never used on the right of a production";
        }

        // Validations succeeded

        return string.Empty;
    }

    /// <summary>
    /// Once populated, contains the first sets
    /// for the Nonterminals in the grammar
    /// </summary>

    public Dictionary<string, List<GrammarElement>> FirstSets
    {
        get;
        private set;
    }

    /// <summary>
    /// Once the entire grammar has been parsed, this method
    /// calculates the list of First sets for the non-terminal
    /// tokens in the grammar
    /// </summary>

    public void ComputeFirstSets()
    {
        // Allocate an empty dictionary with enough empty
        // slots to hold firsts for all non terminals,
        // and the empty token.

        FirstSets = new Dictionary<string, List<GrammarElement>>
            (Nonterminals.Count + 1);

        // Add the empty element to the first set

        List<GrammarElement> termList =
        [
            EmptyElement
        ];
        FirstSets.Add(string.Empty, termList);

        // Initialise the list of non-terminals tokens
        // with empty first sets to start with. Iteration
        // will gradually fill these with elements.

        foreach (GrammarToken t in Nonterminals)
            FirstSets.Add(t.Text, []);

        // Repeatedly update the first sets until none of the
        // sets changes in an iteration. Then we know we are done.

        bool setsChanged = true;
        while (setsChanged)
        {
            setsChanged = false;
            foreach (GrammarToken gt in Nonterminals)
            {
                // Find the list of productions that share the
                // same LHS with name gt.Text. We need to obtain
                // the union of the First(X1 X2 ... Xk)
                // productions that all share the same LHS.

                IEnumerable<GrammarProduction> rulesWithSameLHS =
                    from p in Productions
                    where p.LHS.Text == gt.Text
                    select p;

                // Update the first set for the non terminal in question. If
                // the list of tokens changes, capture this is setsChanged, as
                // this will cause another iteration through the list of Nonterminals.

                foreach (GrammarProduction prod in rulesWithSameLHS)
                    if (Merge(First(prod.RHS), FirstSets[gt.Text]))
                        setsChanged = true;
            }
        }
    }

    /// <summary>
    /// Attempt to merge a new element into a list. Don't
    /// bother if the element is already in the list.
    /// </summary>
    /// <param name="ge">The element to potentially merge</param>
    /// <param name="dst">The list to merge the element into</param>
    /// <returns>True if the element was merged, false if it
    /// was already in the list</returns>

    public static bool Merge(GrammarElement ge, IList<GrammarElement> dst)
    {
        if (ge != null && dst != null && !dst.Any(e => e == ge))
        {
            dst.Add(ge);
            return true;
        }
        else
            return false;
    }

    /// <summary>
    /// Merge one list of grammar elements into another. If the
    /// list contents actually grew, return true. Otherwise
    /// return false.
    /// </summary>
    /// <param name="src">The source list to be merged into
    /// the destination</param>
    /// <param name="dst">The destination list into which the
    /// source list will be merged</param>
    /// <returns>True if the contents of the destination list
    /// changed. False if every element in the source list
    /// was already in the destination list.</returns>

    public static bool Merge(IList<GrammarElement> src, IList<GrammarElement> dst)
    {
        bool listChanged = false;
        if (src != null)
            foreach (GrammarElement se in src)
                if (Merge(se, dst))
                    listChanged = true;
        return listChanged;
    }

    /// <summary>
    /// Returns First(X1 X2 ... Xk) where the argument is a
    /// sequence of grammar elements that appear on the right
    /// side of a production.
    /// 
    /// Algorithm:
    /// 
    /// For a production N : X1 X2 X3 ... Xk
    /// If X1 is a terminal token, First(N) is X1.
    /// If there are no Xi, meaning N is an empty
    /// production, First(N) is the empty token.
    /// Otherwise First(N) is First(X1) if First(X1)
    /// does not contain the empty token. If it does,
    /// First(N) is First(X1) - EmptyToken + First(X2).
    /// If First(X2) contains the empty token, First(N)
    /// is First(X1) - EmptyToken + First(X2)
    /// - EmptyToken + First(X3), and so on.
    /// If the empty token is in every Xi, then finally
    /// the empty token is added to First(N).
    /// </summary>
    /// <param name="gel">A sequence of grammar elements that
    /// appear on the right side of a production.</param>
    /// <returns>The first set for the sequence of elements</returns>

    public IList<GrammarElement> First(IList<GrammarElement> gel)
    {
        if (gel == null)
            throw new ArgumentNullException
                (nameof(gel), "Need a valid grammar element list");

        List<GrammarElement> result =
        [

            // An empty production has the empty element as its
            // only member in the first set. This is removed by
            // other elements in the production, unless they all
            // have the empty element within their respective
            // first sets.

            EmptyElement
        ];
        foreach (GrammarElement ge in gel)
        {
            IList<GrammarElement> firstList = First(ge);
            Merge(firstList, result);
            if (!ContainsEmpty(firstList))
                return WithoutEmpty(result);
        }
        return result;
    }

    /// <summary>
    /// Find the first set for the specified token in
    /// the grammar element passed as argument.
    /// </summary>
    /// <param name="ge">The grammar element for which
    /// we need the first set</param>
    /// <returns>The first set for the specified grammar element,
    /// or null if the token name in the grammar element
    /// does not belong to the grammar.</returns>

    public List<GrammarElement> First(GrammarElement ge)
    {
        if (ge == null)
            throw new ArgumentNullException
                (nameof(ge), "Need a valid grammar element object");

        if (ge.Token.TokenType == TokenType.Terminal)
            return
            [
                ge
            ];
        else if (FirstSets.TryGetValue(ge.Token.Text, out List<GrammarElement> result))
            return result;
        else
            return null;
    }

    /// <summary>
    /// Given a list of terminal tokens, return
    /// the list without the empty token if the
    /// list contains the empty token
    /// </summary>
    /// <param name="gel">The terminal token list</param>
    /// <returns>The same list minus any empty token</returns>

    public IList<GrammarElement> WithoutEmpty(IList<GrammarElement> gel)
    {
        if (gel != null && ContainsEmpty(gel))
        {
            List<GrammarElement> result
                = new(gel.Count);
            foreach (GrammarElement gt in gel)
                if (!string.IsNullOrEmpty(gt.Token.Text))
                    result.Add(gt);
            return result;
        }
        else
            return gel;
    }

    /// <summary>
    /// Find out if a list of elements includes the empty element
    /// </summary>
    /// <param name="gel">The list to search</param>
    /// <returns>True if the empty element is in the list,
    /// false if not</returns>

    public bool ContainsEmpty(IList<GrammarElement> gel) => gel != null && gel.Any(ge => ge == EmptyElement);

    /// <summary>
    /// Debugging routine. Renders the list of non-terminals and their
    /// first sets, as computed by the FIRST(X) algorithm used here.
    /// </summary>
    /// <returns>String list of tokens and their FIRST sets</returns>

    public string RenderFirstSets()
    {
        StringBuilder sb = new("\r\nNON-TERMINAL FIRST SETS\r\n");
        foreach (string key in FirstSets.Keys)
        {
            if (string.IsNullOrEmpty(key))
                sb.Append("\u03B5:");
            else
                sb.Append(key + ":");
            foreach (GrammarElement ge in FirstSets[key])
                sb.Append(" " + ge);
            sb.AppendLine();
        }
        return sb.ToString();
    }

    /// <summary>
    /// Produce a string representation of all the
    /// created item sets in the grammar
    /// </summary>
    /// <returns>String representation of all
    /// item sets in the grammar</returns>

    public string RenderItemSetsAndGotos()
    {
        StringBuilder sb = new("\r\nSHIFTS AND REDUCTIONS\r\n");
        foreach (GrammarItemSet gis in ItemSets)
            sb.AppendLine(gis.ToString(true));
        return sb.ToString();
    }

    /// <summary>
    /// Produce a string representation of all the
    /// productions in the input grammar, preceded
    /// by their production numbers. This enables
    /// the reader to correlate the reductions
    /// against their corresponding reducing
    /// production.
    /// </summary>
    /// <returns>String representation of all
    /// productions in the grammar</returns>

    public string RenderProductions()
    {
        StringBuilder sb = new("RULES\r\n");
        IOrderedEnumerable<GrammarProduction> gpl
            = Productions.OrderBy(p => p.ProductionNumber);
        foreach (GrammarProduction gp in gpl)
            sb.Append($"R{gp.ProductionNumber}: {gp}\r\n");
        return sb.ToString();
    }

    /// <summary>
    /// Given a completely parsed grammar, create
    /// the tables and data structures needed for
    /// creating the perser state machine.
    /// </summary>
    /// <param name="allowConflicts">True to allow
    /// item sets to include mutliple actions for
    /// the same lookahead token. This would be
    /// set to true if implementing a GLR
    /// parser, to false if just implementing LR(1).</param>

    public string CreateItemSetsAndGotoEntries(bool allowConflicts)
    {
        // Make sure that the grammar has been extended to include
        // a specified start symbol that never appears on the
        // right of a production anywhere. Also look for non-terminals
        // that have not been defined, or that are not used.

        string errMessage = InitializeAndValidateGrammar();
        if (!string.IsNullOrEmpty(errMessage))
            return errMessage;

        // Handle non-terminals that have guard conditions set on them

        ExpandProductionsForGuards();

        // First create set 0, which is the item set associated
        // with the extended grammar's start symbol, "_Start".

        GrammarToken startToken = Nonterminals.FirstOrDefault(t => t.Text == "_Start");
        if (startToken == null)
            return "Can't create item sets before initializing and validating grammar";

        List<GrammarItem> startItemList =
        [
            new GrammarItem(RootProduction, 0, EndingElement)
        ];
        ItemSets.Add(new GrammarItemSet(startItemList));

        // Prepare the collection of first sets for the grammar tokens

        ComputeFirstSets();

        // Walk the list of item sets, computing their closures, then finding
        // any next item sets they jump to when next tokens in the grammar have
        // been recognised. This means that the list of item sets is getting
        // longer as we walk along it. We rely on the growth of the list
        // happening slower than we walk along it to terminate the process. This
        // happens because eventually later item sets find themselves referring
        // back to earlier sets in the list rather than to new ones.

        int itemSetCount = 1;
        for (int currItemSet = 0; currItemSet < itemSetCount; currItemSet++)
        {
            GrammarItemSet currSet = ItemSets[currItemSet];

            // First compute the closure on this set. This makes sure any
            // other rules in the grammar that could represent the current
            // position in the input token stream are added to the list of
            // items in the current item set.

            ComputeClosure(currSet);

            // Now work out what other item sets this closure would jump to
            // for each possible next input token within the productions
            // in the item set. Note that this method extends the length
            // of the ItemSets collection with new item sets, particularly
            // towards the beginning of this iterative process. Note that
            // any error message coming back from this function call will
            // tell you about the first reduce/reduce or shift/reduce
            // conflict found in the grammar.

            errMessage = ComputeShiftsAndReductions(currSet, allowConflicts);
            if (!string.IsNullOrEmpty(errMessage))
                return errMessage;

            // Update the length of the list of item sets again, as it is
            // growing while the item set construction process is going on

            itemSetCount = ItemSets.Count;
        }
        return string.Empty;
    }

    /// <summary>
    /// Guard conditions are permitted on both terminal and non-terminal
    /// symbols. When they occur on non-terminals, we have to map these
    /// back down the grammar tree until they are placed on terminals.
    /// This process also adds new grammar rules to the grammar.
    /// Examples: a: b c[C1] d where c is a non-terminal with guard C1
    /// applied. In this case the [C1] really applies to thelast terminal
    /// token in c rather than the rule c. Hence if there are a set of
    /// rules for which c is the LHS, an additional set of c rules must be
    /// added that include [C1] after their last token. For example:
    /// c: y Z | u V[C2] | w x | m n[C3] |; represents the five possible
    /// cases. In the first case we get a new rule of the form: c[C1]: y Z[C1]
    /// where the guard has now been applied to a terminal symbol. In the
    /// second rule we get c[C1]: u V[C2 & C1] where the guard conditions
    /// have to be combined, but still on a terminal token so this will
    /// be subject to no further rewriting. In the third case, we get:
    /// c[C1]: w x[C1] where C1 has been applied to a non-terminal. The
    /// rules for x will now also need expanding. In the fourth case,
    /// we get: c[C1]: m n[C3 & C1] meaning that the guards are combined,
    /// then the rules for n are expanded. The last case is the difficult
    /// one, as this represents the empty rule. This means that the guard
    /// should be applied in rule a: b c[C1] as a guard on the
    /// last token of b for the c[c1]: ; reduction to be applied. This
    /// is difficult to set up, and represents most of the complexity
    /// in the code below.
    /// </summary>

    private void ExpandProductionsForGuards()
    {
        // Loop termination occurs when all non-terminals that contain
        // guards have been remapped to new grammar rules.

        bool allFound = false;
        while (!allFound)
        {
            // Find the first remaining production for which
            // there exists a non-terminal that has a guard
            // condition applied to it.

            GrammarProduction gp = Productions.FirstOrDefault
                (p => p.RHS.Any(e =>
                    e.Token.TokenType == TokenType.Nonterminal
                        && e.Guard != null));

            // All the time we keep finding right hand sides
            // that contain non-terminals with guard conditions
            // attached to them, we have to keep rewriting
            // rules.

            if (gp is not null)
            {
                // Find the guarded non-terminal element,
                // then create a new set of productions
                // based on it.

                GrammarElement ge = gp.RHS.First(e =>
                    e.Token.TokenType == TokenType.Nonterminal
                        && e.Guard != null);

                // Find the set of productions that have this
                // same non-terminal as their LHS

                List<GrammarProduction> gpSet =
                    [.. (from p in Productions
                     where p.LHS.Text == ge.Token.Text
                     select p)];

                // Generate the new productions based on the
                // non-terminal name and the guard condition
                // that appears to its right. These new
                // productions will have the same guard condition
                // applied to their rightmost element, or will
                // have a carry-back guard condition to be
                // applied to predecessors.

                GrammarToken tokε = null;
                GrammarToken tokφ = null;
                foreach (GrammarProduction prod in gpSet)
                {
                    GrammarProduction newProd
                        = ApplyNonterminalGuard(prod, ge.Guard);
                    if (newProd.CarryBack == null)
                        tokφ = newProd.LHS;
                    else
                        tokε = newProd.LHS;
                }

                // Having created the new child rules that
                // have the guard condition applied to their
                // last token, now go and find all rules that
                // refer to the guarded element ge, and
                // apply the rule rewrites to them.

                RewriteProductions(ge, tokφ, tokε, ge.Guard);
            }
            else
                allFound = true;
        }
    }

    /// <summary>
    /// Throughout the grammar, find all rules that contain the element ge
    /// in their right hand expansion, and apply the standard rewrite using
    /// new rules based on the second and third parameters.
    /// </summary>
    /// <param name="ge">The non-terminal token and associated guard
    /// condition that must be replaced wherever they occur on the
    /// right of a grammar rule.</param>
    /// <param name="tokφ">A guardless element based on this token replaces
    /// the guarded token wherever it occurs. If null, this replacement
    /// does not take place.</param>
    /// <param name="tokε">A guardless element used for any rules that must
    /// carry back a guard condition to the previous token. An additional
    /// rule is built into the grammar for this if non null.</param>
    /// <param name="carryBack">The guard condition to be applied
    /// in the carry back rules.</param>

    private void RewriteProductions(GrammarElement ge, GrammarToken tokφ, GrammarToken tokε, BoolExpr carryBack)
    {
        // At this point, tokφ identitifes one
        // or more rules with the guard condition
        // applied to their last token, or is null if
        // there were none. The variable tokε identifies
        // one or more rules in which the guard
        // condition needs to be carried back to the
        // previous element of any parent rules, or
        // null if there are no productions that cause
        // a carry back to the previous token.
        // The task here is to search all the rules
        // rewriting any rules in which the original
        // non-terminal and guard appeared, so that
        // they become two rules. One with the non-
        // terminal and guard replaced by the tokφ
        // LHS token, and one with the guard condition
        // applied to the token before the non-terminal
        // and guard, the non-terminal and guard
        // now being replaced by the tokε token.

        for (int pPos = 0; pPos < Productions.Count; pPos++)
        {
            GrammarProduction p = Productions[pPos];
            for (int elIndex = 0; elIndex < p.RHS.Count; elIndex++)
            {
                // Filter out any tokens that do not match the
                // guarded token we are replacing

                if (p.RHS[elIndex] != ge)
                    continue;

                // We have found a token and guard that match the one
                // being rewritten by this method. Next create the
                // modified productions with the guarded element
                // replaced.

                GrammarProduction pε = null;
                GrammarProduction pφ = null;

                // Create a new production based on the phi rule. This
                // production merely replaces the element with the
                // guard condition with the phi element.

                if (tokφ != null)
                {
                    List<GrammarElement> newElems = [];
                    for (int i = 0; i < p.RHS.Count; i++)
                    {
                        if (i == elIndex)
                            newElems.Add(new GrammarElement(tokφ, null));
                        else
                            newElems.Add(p.RHS[i]);
                    }
                    pφ = new GrammarProduction(this, 0, newElems, p.Code)
                    {
                        LHS = p.LHS,
                        NonterminalRuleNumber = p.NonterminalRuleNumber
                    };

                    // We want to reuse the slot in the list of productions that
                    // was allocated to the original production p.

                    Productions[pPos] = pφ;
                    pφ.ProductionNumber = p.ProductionNumber;
                    p = pφ; // Since entry in Productions list changed
                }

                // Create a new production based on the epsilon rule.
                // This production has the guard condition migrated
                // one token earlier in the rule, and applied to/merged
                // with the condition on that preceding token.

                if (tokε != null)
                {
                    List<GrammarElement> newElems = [];
                    for (int i = 0; i < p.RHS.Count; i++)
                    {
                        if (i == elIndex - 1)
                        {
                            // Create a new guard condition that merges the
                            // existing guard condition on the preceding
                            // element with the one that is being carried back

                            BoolExpr guard = p.RHS[i].Guard;
                            if (guard != null)
                                guard = new AndExpr(guard, carryBack);
                            else
                                guard = carryBack;

                            // Now create a new replacement element with this
                            // new guard condition applied

                            guard = BooleanExpressionCache.InternExpression(guard);
                            newElems.Add(new GrammarElement(p.RHS[i].Token, guard));
                        }
                        else if (i == elIndex)
                            newElems.Add(new GrammarElement(tokε, null));
                        else
                            newElems.Add(p.RHS[i]);
                    }
                    pε = new GrammarProduction(this, 0, newElems, p.Code);

                    // Deal with a carry back from the zeroth element of the rule

                    if (elIndex == 0)
                    {
                        pε.CarryBack = carryBack;

                        // Rename the rule LHS since it has a carry back.
                        // Name should include the epsilon and the guard as identifier

                        string ruleName = p.LHS.Text + "_\u03B5"
                            + pε.CarryBack.AsIdentifier(BooleanExpressionCache.IndexProvider);
                        pε.LHS = FindOrCreateNonterminal(ruleName);
                    }
                    else
                        pε.LHS = p.LHS;

                    // Add the new rule to the end of the list of productions
                    // if this rule was not previously already there.

                    if (!Productions.Exists(ep => ep == pε))
                    {
                        pε.ProductionNumber = Productions.Count;
                        pε.NonterminalRuleNumber
                            = Productions.Count(pr => pr.LHS.Text == pε.LHS.Text);
                        Productions.Add(pε);
                    }

                    // Now recursively propagate any carry back guards
                    // that carried to the front of the corresponding rule

                    if (pε.CarryBack != null)
                    {
                        // Identify the RHS element that will need
                        // to be found and have new rules added for
                        // the carry back from this rule.

                        GrammarElement replacee = new(p.LHS, null);

                        // Now propagate the rewriting to any parent rules

                        RewriteProductions(replacee, null, pε.LHS, pε.CarryBack);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Find a grammar token in the list of non-terminal
    /// tokens. If not found, create a new token with the
    /// specified name, and add it to the list of non-terminal
    /// tokens. Return the found or created token to the caller.
    /// </summary>
    /// <param name="tokenName">The name of the token to find
    /// or create</param>
    /// <returns>The found or created token</returns>

    private GrammarToken FindOrCreateNonterminal(string tokenName)
    {
        // First ensure the new non-terminal token is listed
        // in the list of non-terminals

        GrammarToken newToken = Nonterminals
            .FirstOrDefault(t => t.Text == tokenName);
        if (newToken == null)
        {
            newToken = new GrammarToken
                (Nonterminals.Count + MinNonterminalValue + 1, TokenType.Nonterminal, tokenName);
            Nonterminals.Add(newToken);
        }
        return newToken;
    }

    /// <summary>
    /// Given an existing production, create a new production that
    /// has the guard condition passed as an argument applied to its
    /// last element.
    /// </summary>
    /// <param name="p">The production to replicate</param>
    /// <param name="e">The condition to be applied to
    /// the production's last element</param>
    /// <returns>A new production based on the first, 
    /// but with the guard condition applied</returns>

    private GrammarProduction ApplyNonterminalGuard(GrammarProduction p, BoolExpr e)
    {
        GrammarProduction newProd;
        string ruleName;

        // Deal with empty rules first

        if (p.RHS.Count == 0)
        {
            // Assume that the boolean expression is not empty, even
            // though the expansion of the grammar rule is. Note
            // the use of the epsilon character in the identifier, as
            // this is the empty version of the rule, and has a
            // carry-back guard condition.

            newProd = new GrammarProduction(this, 0, p.RHS, p.Code)
            {
                CarryBack = e
            };
            ruleName = $"{p.LHS.Text}_\u03B5"
                + e.AsIdentifier(BooleanExpressionCache.IndexProvider);
        }
        else
        {
            // First create the list of elements for the new production. Copy
            // over all except the last element in the production into the
            // new list of elements for the new rule.

            List<GrammarElement> elements = new(p.RHS.Count);
            for (int i = 0; i < p.RHS.Count - 1; i++)
                elements.Add(p.RHS[i]);

            // Now handle the last element in the production. If the last
            // element already has a guard condition applied, form a new
            // guard condition by anding with the condition to be applied.
            // Otherwise, simply set the guard condition.

            GrammarElement lastElem = p.RHS.Last();
            BoolExpr guard = lastElem.Guard;
            if (guard != null)
                guard = new AndExpr(guard, e);
            else
                guard = e;

            // Now add the new guard-adjusted element to the end of the
            // production expansion

            guard = BooleanExpressionCache.InternExpression(guard);
            lastElem = new GrammarElement(lastElem.Token, guard);
            elements.Add(lastElem);

            // Now create the new candidate production with
            // no carry-back guard condition, since the condition
            // was applied to the last token in the list.

            newProd = new GrammarProduction(this, 0, elements, p.Code);

            // Create the new expansion name for this rule. This rule has
            // the character phi in it to indicate it has no carryback
            // guard condition.

            ruleName = $"{p.LHS.Text}_\u03D5"
                + e.AsIdentifier(BooleanExpressionCache.IndexProvider);
        }

        // First ensure the new non-terminal token is listed
        // in the list of non-terminals, then set the rule
        // name into the production.

        newProd.LHS = FindOrCreateNonterminal(ruleName);

        // Look to see if this rule is already present in the list
        // of grammar rules created so far. If not, add it to the
        // set of productions.

        GrammarProduction foundProd =
            Productions.FirstOrDefault(prod => prod == newProd
                && prod.CarryBack == newProd.CarryBack);
        if (foundProd == null)
        {
            // Now add the new production to the list of productions

            newProd.ProductionNumber = Productions.Count;
            newProd.NonterminalRuleNumber
                = Productions.Count(pr => pr.LHS.Text == ruleName);
            Productions.Add(newProd);
            return newProd;
        }
        else
            return foundProd;
    }

    /// <summary>
    /// Given an item set initialised with one or more items,
    /// complete it with the remaining items that close the set.
    /// Before computing the closure, we have a number of items
    /// in the list of items that all begin with the same few
    /// tokens, and have position set to the same value. Tokens
    /// to the left of the position are the same for each rule
    /// in the list. This set of items is known as the kernel
    /// of the item set. We now need to add every child rule
    /// that is implied by the kernel. This process is known
    /// as computing the closure. Because we are implementing
    /// an LR(1) parser, we also need to track the look-ahead
    /// sets for each item.
    /// 
    /// ALGORITHM
    /// 
    /// For each item in the item set
    ///   where an item is of the form: A : β ^ B δ, {a}
    ///   in which β is a sequence of symbols that have already
    ///   been parsed, '^' represents the position in the
    ///   parse so far, B is the next non-terminal token
    ///   to be parsed, δ is the sequence of symbols beyond
    ///   B in the production, and {a} is the terminal token
    ///   expected after the whole rule has been parsed that
    ///   will allow the rule to be reduced (replaced on stack
    ///   by its LHS non-terminal A):
    ///   * Find all the productions that have B as their LHS
    ///     For each production that has B as its LHS, find all
    ///     terminals {b} that are in FIRST(δ a).
    ///   * For every terminal {b} add the production for B
    ///     to the item set, with the position set to its
    ///     beginning, and the look ahead token set to {b}.
    ///     Note that if this production is already in the
    ///     closure, there is no need to add it a second time.
    /// </summary>
    /// <param name="gis">The grammar item set that we
    /// are computing the closure for</param>

    public void ComputeClosure(GrammarItemSet gis)
    {
        if (gis == null)
            throw new ArgumentNullException(nameof(gis), "Need a valid item set object");

        // First mark all the current items in the set as
        // being core items, to distinguish them from
        // closure items. This is only used for debugging
        // or printout purposes, and serves no
        // algorithmic purpose.

        foreach (GrammarItem gi in gis.Items)
            gi.Core = true;

        // Walk the list of items applying the closure
        // algorithm to each item. Note that Items.Count
        // is getting larger at the same time as currItem
        // is incrementing. We rely on currItem growing
        // faster than Items.Count to terminate the closure.
        // This happens because some of the items we intend
        // to add are already in the list.

        for (int currItem = 0; currItem < gis.Items.Count; currItem++)
        {
            GrammarItem item = gis.Items[currItem];
            IList<GrammarElement> rhs = item.Production.RHS;
            int pos = item.Position;

            // Deal with position at end of rule

            if (pos >= rhs.Count)
                continue;

            // Deal with terminal token as next input symbol

            if (rhs[pos].Token.TokenType == TokenType.Terminal)
                continue;

            // Compute the look ahead tokens that will apply
            // for the closure rules. We shall first calculate
            // FIRST(δ).

            List<GrammarElement> δ = [];
            for (int i = pos + 1; i < rhs.Count; i++)
                δ.Add(rhs[i]);

            // Calculate the first set

            IList<GrammarElement> firstOfδ = First(δ);

            // If the first set contains the empty token, we
            // should also add in the lookahead tokens of the
            // kernel item we are expanding.

            if (ContainsEmpty(firstOfδ))
            {
                firstOfδ = WithoutEmpty(firstOfδ);
                Merge(item.LookAheadToken, firstOfδ);
            }

            // Next token is a non-terminal, so we need to add one
            // or more items to the item set, if they are not
            // already there.

            IEnumerable<GrammarProduction> closureProductions =
                from p in Productions
                where p.LHS == rhs[pos].Token
                select p;

            // Is an item of the type we are seeking already in
            // the item set? If not, add it to the end of the set
            // once for each possible lookahead token. Note: 80%
            // of the CPU time for the parser generator is spent
            // in this set of nested loops.

            foreach (GrammarProduction p in closureProductions)
                foreach (GrammarElement ge in firstOfδ)
                    if (!gis.Items.Exists(gi => gi.Production == p
                        && gi.Position == 0
                        && gi.LookAheadToken == ge))
                        gis.Items.Add(new GrammarItem(p, 0, ge));
        }
    }

    /// <summary>
    /// Given an item set for which the closure has been
    /// computed, find the set of itemsets that it can
    /// transit to on recognition of an input grammar token.
    /// </summary>
    /// <param name="origin">The item set from which we are jumping</param>
    /// <param name="allowConflicts">Set true to build a GLR
    /// parser that branches the parser stack when a conflict
    /// occurs. Set false for a traditional LR single
    /// stack parser.</param>
    /// <returns>Empty string if successful, error report if there 
    /// are shift and reduce conflicts</returns>

    public string ComputeShiftsAndReductions(GrammarItemSet origin, bool allowConflicts)
    {
        if (origin == null)
            throw new ArgumentNullException(nameof(origin), "Need a valid item set object");

        // Duplicate the origin list so that we can remove items in
        // groups corresponding to the same preamble token sequences

        List<GrammarItem> org = [.. origin.Items];

        while (org.Count > 0)
        {
            // Find the shifts and reductions that apply
            // to the specified lookahead token


            origin.Reductions.TryGetValue
                (org[0].LookAheadToken, out List<GrammarProduction> reductionProductions);
            origin.Shifts.TryGetValue
                (org[0].LookAheadToken, out List<GrammarItemSet> shiftSets);

            // Build REDUCE links for rules that
            // terminate at this item set

            if (org[0].Position >= org[0].Production.RHS.Count)
            {
                // First look for reduce/reduce conflicts. These
                // occur when there is already an entry in the
                // reductions for this item set at the specified
                // input token.

                string tokName = org[0].LookAheadToken.ToString();
                if (!allowConflicts)
                {
                    if (reductionProductions != null)
                        return $"Reduce/reduce conflict on token {tokName}"
                            + $" for productions [{reductionProductions[0]}]"
                            + $" and [{org[0].Production}]";

                    // Check for a shift/reduce conflict on this input token

                    if (shiftSets != null)
                        return $"Shift/reduce conflict on token {tokName}"
                            + $" for shift to [{shiftSets[0]}]"
                            + $" or reduction by [{org[0].Production}]";
                }

                // No conflict, so just add the reduction

                if (reductionProductions == null)
                    origin.Reductions.Add
                    (
                        org[0].LookAheadToken,
                        [
                            org[0].Production
                        ]
                    );
                else
                    reductionProductions.Add(org[0].Production);
                org.RemoveAt(0);
            }
            else
            {
                // Build SHIFT links for input tokens that cause the
                // parser to shift one token along the current rule.
                // Find the type of token immediately to the
                // right of the current position in this grammar item

                GrammarElement ge = org[0].Production.RHS[org[0].Position];

                // Get the items that will consume the same next 
                // token immediately to the right of the position
                // indicator in the zeroth grammar item

                List<GrammarItem> itemGroup =
                    [.. (from i in org
                     where i.Position < i.Production.RHS.Count
                     && i.Production.RHS[i.Position] == ge
                     select i)];

                // Now form the shifted set of these grammar items,
                // removing them from the origin list so that next
                // time around the while loop we find a different
                // next token, and hence a different target item set.

                List<GrammarItem> shiftedItems = [];
                foreach (GrammarItem gi in itemGroup)
                {
                    org.Remove(gi);
                    shiftedItems.Add(gi.Shifted());
                }

                // Now search the list of item sets to see if this cluster
                // of items can be found anywhere among the already found sets

                GrammarItemSet foundSet = null;
                foreach (GrammarItemSet gis in ItemSets)
                {
                    // True if all GrammarItems in the source ItemSet exist within the
                    // item set referenced by the current gis we are inspecting

                    if (shiftedItems.All(gi => gis.Items.Exists(gs => gs == gi)))
                    {
                        foundSet = gis;
                        break;
                    }
                }

                // If the items we are looking for are not found in any 
                // existing item set, create a new one, and add it to the
                // list of item sets known by the parser.

                if (foundSet == null)
                {
                    foundSet = new GrammarItemSet(shiftedItems);
                    ItemSets.Add(foundSet);
                }

                // Check for a shift/reduce conflict on this input token

                origin.Reductions.TryGetValue(ge, out reductionProductions);

                if (!allowConflicts && reductionProductions != null)
                    return "Shift/reduce conflict on token "
                        + $"{ge} for shift to [{foundSet.Items[0]}]"
                        + $" or reduction by [{reductionProductions[0]}]";

                // Update the Goto dictionary so that it contains an
                // entry for which item set to jump to from this
                // item set when the current token is recognised

                origin.Shifts.TryGetValue(ge, out shiftSets);
                if (shiftSets == null)
                    origin.Shifts.Add(ge, [foundSet]);
                else
                    shiftSets.Add(foundSet);
            }
        }
        return string.Empty;
    }

    //                   Pager's algorithm
    //
    // An LR(1) canonical parser contains many more states than found
    // in the equivalent LALR(1) parser, but removes any fear of
    // introducing spurious reduce/reduce conflicts where the same
    // itemset is shared between what would be separate sets in
    // LR(1). Pager's algorithm takes a complete LR(1) parsing
    // table, and uses the same state-folding approach that LALR(1)
    // uses, but only folds those states that would not result in
    // spurious reduce/reduce conflicts.
    //
    // How the algorithm works: Two states can be folded if:
    // * They represent the same group of LR(0) items. This means
    //   the lists of productions with position markers in them
    //   are the same for both sets. Note that the lookahead
    //   tokens are not included in this comparison.
    // * Any reductions implemented in the state do not collide.
    //   This means that for any reduce operation specified
    //   for lookahead token and guard T[G] there is not a different
    //   production specified for the reductions in each set.
    //
    // Once pairs of states have been found, they can be merged by:
    // * Forming the expanded list of items within the state, expanded
    //   for the union of all lookahead symbols on items.
    // * Creating the union of shifts and reductions in the single
    //   replacement set.
    // * Redirecting all previous shifts and reductions that jumped
    //   to the former states to jump to the newly formed joined
    //   state instead.
    // * Removing the no longer needed former states.

    /// <summary>
    /// Apply Pager's algorithm to the parse tables in order to
    /// reduce their size close to the optimum minimal size. If
    /// no additional reduce/reduce conflicts would be introduced
    /// by doing this, the parser reduces to the size of the
    /// equivalent LALR(1) parser.
    /// </summary>

    public void CompressItemSets(bool applyPagersAlgorithm)
    {
        // As reductions in state numbers could introduce
        // later opportunities to collapse the state table,
        // we run the nested loop several times until no
        // more optimisations are taking place.

        if (applyPagersAlgorithm)
        {
            // Compare every pair of item sets and
            // apply removal of the second item set
            // in the pair if state folding is found
            // to be valid for this pair of item sets.

            for (int i = 0; i < ItemSets.Count; i++)
            {
                GrammarItemSet gdst = ItemSets[i];
                for (int j = i + 1; j < ItemSets.Count; j++)
                {
                    GrammarItemSet gsrc = ItemSets[j];
                    if (CanFold(gdst, gsrc))
                    {
                        Fold(gdst, gsrc);
                        RedirectReferences(gdst, gsrc);
                    }
                }
            }
        }

        // Formally renumber the grammar item sets so that their
        // set numbers are correct ready for parser generation.
        // This numbering has been deferred to here because
        // the compression process removes item sets from the
        // middle of the list if they are redundant.

        for (int i = 0; i < ItemSets.Count; i++)
            ItemSets[i].SetNumber = i;
    }

    /// <summary>
    /// After all other processing has taken place for a non-GLR
    /// parser, make sure that the shifts and reductions are in
    /// the correct order within each item set. This really means
    /// looking for overlapping guard conditions on the same token
    /// within each state, and ensuring the order deals with the
    /// overlap in a well-ordered way.
    /// </summary>
    /// <returns>An empty string if the grammar can be ordered
    /// deterministically. An error message if there are any
    /// itemsets that contain guard conditions on a token where
    /// these guard conditions just interset, and are therefore
    /// ambiguous. The grammar writer is then expected to
    /// rewrite the grammar with additional unambiguous guards.</returns>

    public string EstablishShiftReduceOrdersForItemSets()
    {
        StringBuilder errResult = new();
        foreach (GrammarItemSet itemSet in ItemSets)
            errResult.Append(itemSet.ComputeShiftReduceOrder());
        return errResult.ToString();
    }

    /// <summary>
    /// Compare two item sets to see if they can be folded to form
    /// a single item set using Pager's algorithm. NOTE: This
    /// implementation of Pager's algorithm will not work with a
    /// GLR parser, in which the same token can appear multiple
    /// times in an item set's shift or reduce lists.
    /// </summary>
    /// <param name="gl">One of the two item sets to compare</param>
    /// <param name="gr">The second item set to compare</param>
    /// <returns>True if the item sets can be folded</returns>

    public static bool CanFold(GrammarItemSet gl, GrammarItemSet gr)
    {
        if (gl == null || gr == null)
            throw new ArgumentException("CanFold must be called with neither parameter null");

        // For every item in gl, the same item must exist in gr
        // ignoring the look ahead tokens in each case, for the
        // states to be candidates for merging. If the cores are
        // equivalent in this way, we also check the list of
        // reduction operations in each set, to see if any
        // reduce/reduce collisions would be introduced by
        // merging these two states.

        return gl.Items.All
        (
            gli => gr.Items.Exists
            (
                grItem => gli.SameExcludingLookAheadToken(grItem)
            )
        )
        && !gl.Reductions.Keys.Any
        (
            kl => gr.Reductions.ContainsKey(kl)
                && gl.Reductions[kl][0] != gr.Reductions[kl][0]
        );
    }

    /// <summary>
    /// Fold the items in gsrc into gdst, forming the merged
    /// single item set in gdst. Gsrc can then be released.
    /// NOTE: In its present form, this implementation of
    /// Pager's algorithm cannot be used with a GLR parser.
    /// </summary>
    /// <param name="gdst">The set into which another set
    /// will be merged</param>
    /// <param name="gsrc">The set from which items will
    /// be taken to be merged into gdst</param>

    public static void Fold(GrammarItemSet gdst, GrammarItemSet gsrc)
    {
        if (gdst == null || gsrc == null)
            throw new ArgumentNullException
                (gdst == null ? "gdst" : "gsrc",
                "Fold must be called with neither parameter null");

        // Merge the lists of items

        foreach (GrammarItem gi in gsrc.Items)
            if (!gdst.Items.Exists(gd => gd == gi))
                gdst.Items.Add(gi);

        // Merge the shifts

        foreach (GrammarElement ge in gsrc.Shifts.Keys)
        {
            // Merge the shift

            if (!gdst.Shifts.ContainsKey(ge))
                gdst.Shifts.Add
                (
                    ge,
                    [
                        gsrc.Shifts[ge][0]
                    ]
                );
        }

        // Merge the reductions

        foreach (GrammarElement gi in gsrc.Reductions.Keys)
        {
            // Merge the reduction

            if (!gdst.Reductions.ContainsKey(gi))
                gdst.Reductions.Add
                (
                    gi,
                    [
                        gsrc.Reductions[gi][0]
                    ]
                );
        }
    }

    /// <summary>
    /// Once states have been merged, we need to redirect all references
    /// to the obsolete state to the newly merged state, prior to the
    /// obsolete state being released. NOTE: In its present form, this
    /// implementation of Pager's algorithm cannot be used in a GLR parser.
    /// </summary>
    /// <param name="gdst">The new state we should be referring to</param>
    /// <param name="gsrc">The state that is being removed</param>

    public void RedirectReferences(GrammarItemSet gdst, GrammarItemSet gsrc)
    {
        // Perform the redirection of references

        foreach (GrammarItemSet gis in ItemSets)
        {
            // Find the list of keys into the shifts and
            // gotos collection that need to be reassigned

            List<GrammarElement> changees = [.. gis.Shifts.Keys
                .Where(k => gis.Shifts[k][0] == gsrc)];

            // For each of these found keys, reassign the
            // shift operation so that it jumps to the
            // new merged state rather than the previous
            // separate state.

            foreach (GrammarElement ge in changees)
                gis.Shifts[ge][0] = gdst;
        }

        // Now remove the redundant grammar item set from the list

        ItemSets.Remove(gsrc);
    }

    // ------------ Support for simple inline state machines -------------- //

    /// <summary>
    /// A state machine is recorded as a list of states
    /// </summary>

    public List<State> MachineStates
    {
        get;
        private set;
    }

    /// <summary>
    /// The declared entry state for the machine. This
    /// is the only state that need not have a transition
    /// into it, when validating states.
    /// </summary>

    public State StartingState
    {
        get;
        set;
    }

    /// <summary>
    /// Make sure that the set of states in the state machine
    /// is consistent with being a single completely connected
    /// set of states with an entry state.
    /// </summary>
    /// <param name="entryStateName">The name of the state
    /// that is the entry point for the machine</param>
    /// <returns>An error message or string.Empty
    /// if successfully validated.</returns>

    public string ValidateStates(string entryStateName)
    {
        // Capture the starting state

        State s = MachineStates.FirstOrDefault
            (t => t.StateName.Text == entryStateName);
        if (s == null)
            return $"Starting state '{entryStateName}'"
                + " does not appear among state rules";
        else
            StartingState = s;

        // All states except the starting state must have at
        // least one transition to them from a different state.
        // This is an incomplete test, as it is possible to
        // create two disjoint sets of states that pass the
        // following test. Better than no test at all, though!

        if
        (
            !MachineStates.All
            (
                sl => sl == StartingState
                || MachineStates.Any
                (
                    sr => sr != sl && sr.Transitions.Any
                    (
                        t => t.TargetState == sl.StateName
                    )
                )
            )
        )
            return "States exist that have no transitions to them";

        // All states must have a transition out of them to
        // a different state, or to the termination of the
        // state machine.

        if (
            !MachineStates.All
            (
                sl => sl.Transitions.Any
                (
                    t => t.TargetState != sl.StateName
                )
            )
        )
            return "States exist that have no transitions from them";

        // Entry state exists, and every state is connected via
        // transitions so that they are part of the state machine.

        return string.Empty;
    }
}
