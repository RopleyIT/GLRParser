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

using System.Collections.Generic;

namespace Parsing
{
    public class StackNode : StackLink, IFreeable<StackNode>
    {
        /// <summary>
        /// The parser state that is being pushed for this node
        /// </summary>

        public int State
        {
            get;
            set;
        }

        /// <summary>
        /// Keeps track of the number of nodes higher in the
        /// stack that have a reference to this node.
        /// </summary>

        public int ReferenceCount
        {
            get;
            set;
        }

        /// <summary>
        /// Make a reference to this stack node to prevent
        /// it being put back onto a freelist.
        /// </summary>
        /// <returns>A self reference</returns>

        public StackNode AddReference()
        {
            ReferenceCount++;
            return this;
        }

        /// <summary>
        /// Given the current stack node that is assumed to be
        /// the node at the top of a stack, find the set of
        /// paths through the stack that are of the specified depth
        /// from the top of the selected stack.
        /// </summary>
        /// <param name="depth">The number of nodes into the stack
        /// from the current stack node (this) to descend.</param>
        /// <param name="linkRequiredInPath">Set to a stack
        /// link that must be in the path somewhere for the
        /// path to be included in the results. Set to
        /// null to include all paths with the required
        /// depth.</param>
        /// <returns>A list of arrays of stack link references
        /// that shows the path from the node 'depth' nodes
        /// down the stack up to the level beneath the
        /// current stack node. Hence a depth of 2 will give
        /// a reference to the StackLink in the top node that
        /// points at the second level in slot 1, and a
        /// reference to the StackLink beneath that node that
        /// points down to the third level in slot 0.
        /// Note the order. The deeper nodes are in
        /// the earlier slots in the array, as befits
        /// the order in which arguments are passed
        /// to the action functions when a rule is
        /// reduced.</returns>

        public List<StackLink[]> FindPathsOfDepth
            (int depth, StackLink linkRequiredInPath)
        {
            StackLink[] path;
            List<StackLink[]> foundPaths = new List<StackLink[]>();
            path = new StackLink[depth];
            WalkStack(depth - 1, foundPaths, path, linkRequiredInPath);
            return foundPaths;
        }

        /// <summary>
        /// Implementation of the free list for
        /// second hand stack nodes.
        /// </summary>

        StackNode IFreeable<StackNode>.NextFree
        {
            get => TargetNode;
            set => TargetNode = value;
        }

        /// <summary>
        /// Produce a string representation of the StackLink for debugging
        /// </summary>
        /// <param name="tokenMap">The lookup table for tokens</param>
        /// <param name="showValue">True to include the token value
        /// in the string, false to only show the type</param>
        /// <param name="indent">Indent spacing for resulting string</param>
        /// <returns>String representation of the token</returns>

        public string AsString(TwoWayMap<string, int> tokens, bool showValues, int indent)
        {
            string indentStr = string.Empty;
            for (int i = 0; i < indent; i++)
                indentStr += "  ";
            string result = string.Format(
                "{0}Node {1:X}: State={2} RefCount={3}\r\n" +
                "{0}{{\r\n" +
                "{4}\r\n" +
                "{0}}}\r\n",
                indentStr, Number, State, ReferenceCount,
                Token.AsString(tokens, showValues, indent + 1));
            if (TargetNode != null)
                result += TargetNode.AsString(tokens, showValues, indent + 2);
            for (StackLink nxt = Next; nxt != null; nxt = nxt.Next)
            {
                result += string.Format(
                    "{0}  Link {1:X}:\r\n" +
                    "{0}  {{\r\n" +
                    "{2}\r\n" +
                    "{0}  }}\r\n",
                    indentStr, nxt.Number, nxt.Token.AsString(tokens, showValues, indent + 2));
                if (nxt.TargetNode != null)
                    result += nxt.TargetNode.AsString(tokens, showValues, indent + 2);
            }
            return result;
        }

        public override string ToString() => base.ToString() + string.Format(", State={0}, RefCnt={1}",
                State, ReferenceCount);
    }
}
