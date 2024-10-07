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

namespace ParserGenerator
{
    /// <summary>
    /// The production item class forms part of an item
    /// set in an LR parser. It references a grammar rule
    /// production, but also captures a position between,
    /// before or after the elements in the production.
    /// In this way it represents a state the parser might
    /// be in between recognitions of tokens as it scans
    /// through the input tokens. The production state
    /// class also remembers the list of look-ahead tokens
    /// that can appear beyond this state. Note that strictly
    /// speaking, if there are multiple look-ahead tokens,
    /// there is one state per different look-ahead token.
    /// This class is a convenient abbreviation.
    /// </summary>

    public class GrammarItem
    {
        /// <summary>
        /// The production this state sits part way along
        /// </summary>

        public GrammarProduction Production
        {
            get;
            private set;
        }

        /// <summary>
        /// The position along this production the
        /// current state represents
        /// </summary>

        public int Position
        {
            get;
            private set;
        }

        /// <summary>
        /// The lookahead token that may follow
        /// this particular production state
        /// </summary>

        public GrammarElement LookAheadToken
        {
            get;
            private set;
        }

        /// <summary>
        /// Indicates whether this is a core item in
        /// an item set, or whether it is added as
        /// part of the closure of an item set.
        /// </summary>

        public bool Core
        {
            get;
            set;
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="pg">The production this state is based on</param>
        /// <param name="pos">The position within this production</param>
        /// <param name="ge">The lookahead token for this item</param>

        public GrammarItem(GrammarProduction pg, int pos, GrammarElement ge)
        {
            if (pg == null)
                throw new ArgumentException
                    ("GrammarItems must be created with a non-null production");
            else
                Production = pg;

            if (pos < 0 || pos > pg.RHS.Count)
                throw new ArgumentException
                    ("Position is not within production element range");
            else
                Position = pos;

            LookAheadToken = ge;
        }

        /// <summary>
        /// Produce a grammar item that corresponds to a
        /// GOTO operation on this grammar item, shifting
        /// one position to the right
        /// </summary>
        /// <returns>A new grammar item matching the new position, or null
        /// if there were no more shifst in this rule</returns>

        public GrammarItem Shifted()
        {
            if (Position >= Production.RHS.Count)
                return null;
            else
                return new GrammarItem(Production, Position + 1, LookAheadToken);
        }

        /// <summary>
        /// The element at the next input symbol position, or
        /// null if we are already at the end of the production
        /// </summary>

        public GrammarElement NextElement
        {
            get
            {
                if (Position >= Production.RHS.Count)
                    return null;
                else
                    return Production.RHS[Position];
            }
        }

        /// <summary>
        /// Compute a valid hash code for the grammar item
        /// </summary>
        /// <returns>A valid hash code</returns>

        public override int GetHashCode() =>
            Production.GetHashCode()
                + (LookAheadToken.GetHashCode() << 4)
                + Position;

        /// <summary>
        /// Implementation of by value comparison. Unused, as optimised
        /// the implementation of operator == instead.
        /// </summary>
        /// <param name="obj">The other object to compare against</param>
        /// <returns>True if same value</returns>

        public override bool Equals(object obj)
        {
            if (obj is not GrammarItem ge)
                return false;
            else
                return Production == ge.Production
                    && Position == ge.Position
                    && LookAheadToken == ge.LookAheadToken;
        }

        /// <summary>
        /// Compare the LR(0) part of the item against another. This means
        /// checking that the productions and positions within those productions
        /// are the same, but excludes matching the lookahead tokens
        /// </summary>
        /// <param name="gr">The other item to compare against</param>
        /// <returns>True if their cores are the same, false if not</returns>

        public bool SameExcludingLookAheadToken(GrammarItem gr)
        {
            if (gr == null)
                throw new ArgumentNullException(nameof(gr));

            return Production == gr.Production && Position == gr.Position;
        }

        /// <summary>
        /// Reimplement operator == to compare by value
        /// </summary>
        /// <param name="l">Left operand</param>
        /// <param name="r">Right operand</param>
        /// <returns>True if both null, or both have same value</returns>

        public static bool operator ==(GrammarItem l, GrammarItem r)
        {
            // Obviously equal if same instance

            if (l == (object)r)
                return true;

            // Deal with one or other being null

            if (l is null || r is null)
                return false;

            // Compare value fields

            return l.Production == r.Production
                && l.Position == r.Position
                && l.LookAheadToken == r.LookAheadToken;
        }

        /// <summary>
        /// Operator != implemented as complement
        /// of operator ==.
        /// </summary>
        /// <param name="l">Left operand</param>
        /// <param name="r">Right operand</param>
        /// <returns>True if not equal or not both null</returns>

        public static bool operator !=(GrammarItem l, GrammarItem r) => !(l == r);

        /// <summary>
        /// Render the item as a production with position
        /// marker, plus the lookahead token.
        /// </summary>
        /// <returns>The item complete with position
        /// marker and loookahead token</returns>

        public override string ToString() => $"{Production.ToString(Position)}, {LookAheadToken}";
    }
}
