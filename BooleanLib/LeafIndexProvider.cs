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

namespace BooleanLib
{
    public class LeafIndexProvider
    {
        /// <summary>
        /// The maximum number of different boolean
        /// variables that can be supported by this
        /// leaf index provider. This cannot be
        /// bigger than 62 because some of the
        /// algorithms would extend into the sign
        /// bit of a long integer causing them to
        /// fail.
        /// </summary>

        public const int MaxVarCount = 62;

        private readonly List<string> leafNames;
        private readonly Dictionary<string, int> leaves;

        /// <summary>
        /// Default constructor
        /// </summary>

        public LeafIndexProvider()
        {
            leafNames = new List<string>(MaxVarCount);
            leaves = new Dictionary<string, int>(MaxVarCount);
        }

        /// <summary>
        /// Lookup the leaf object from
        /// the set of leaves registered with
        /// the expression factory. If it is
        /// not found, create it and add it to
        /// the set of registered leaves. Note
        /// that there is an upper limit of 64
        /// boolean variables (leaves) in this
        /// expression library at present.
        /// </summary>
        /// <param name="name">Name of the leaf variable
        /// that we seek.</param>
        /// <returns>The unique index for this leaf name</returns>

        public int FindLeafIndex(string name)
        {
            int nextFreeIndex = leafNames.Count;
            if (nextFreeIndex >= MaxVarCount)
                throw new InvalidOperationException
                    ("Number of variables in parser guard expressions"
                        + $" exceeds maximum of {MaxVarCount}");
            else if (leaves.TryGetValue(name, out int index))
                return index;
            else
            {
                leafNames.Add(name);
                leaves.Add(name, nextFreeIndex);
                return nextFreeIndex;
            }
        }

        /// <summary>
        /// Given a mask word that defines which leaves
        /// are used in an expression, return the list
        /// of names for those leaf nodes.
        /// </summary>
        /// <param name="mask">the mask word that defines
        /// which leaf nodes are used by an expression</param>
        /// <returns>The list of names of leaves in use</returns>

        public List<string> LeafNamesFromMask(long mask)
        {
            ulong umask = (ulong)mask;
            List<string> names = [];
            for (int i = 0; umask != 0; i++)
            {
                if ((umask & 1) != 0)
                    names.Add(leafNames[i]);
                umask >>= 1;
            }
            return names;
        }
    }
}
