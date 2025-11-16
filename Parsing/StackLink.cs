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

namespace Parsing;

/// <summary>
/// Represents a link from a node near the
/// top of the generalised parser stack to
/// the node immediately beneath it.
/// </summary>

public class StackLink : IFreeable<StackLink>
{
    // An index number for stack links and nodes
    // as they get created. Serves no purpose other
    // than making debugging views of the stack
    // easier to read.

    private static int linkNumber;

    /// <summary>
    /// Accesses the unique small number that
    /// identifies which stack link or stack node this is.
    /// </summary>

    public int Number
    {
        get;
        private set;
    }

    /// <summary>
    /// Default constructor. Merely sets up
    /// the unique object number.
    /// </summary>

    public StackLink() => Number = ++linkNumber;

    /// <summary>
    /// The data associated with this link in the stack.
    /// This contains the token type and the value of
    /// the data associated with the node immediately
    /// above this link in the stack.
    /// </summary>

    public IToken Token
    {
        get;
        set;
    }

    /// <summary>
    /// The node to which we are setting a reference
    /// </summary>

    public StackNode TargetNode
    {
        get;
        set;
    }

    /// <summary>
    /// Stack links are built into a list
    /// </summary>

    public StackLink Next
    {
        get;
        set;
    }

    /// <summary>
    /// Attach this link to a node deeper in stack
    /// </summary>
    /// <param name="node">The node to attach to</param>

    public void ConnectToStackNode(StackNode node)
    {
        if (node != null)
            node.ReferenceCount++;
        TargetNode = node;
    }

    /// <summary>
    /// Disconnect this link from the node beneath
    /// </summary>
    /// <returns>A reference to the node
    /// beyond this link if that node's
    /// reference count has now dropped 
    /// to zero. This indicates that that
    /// node should also be freed as it
    /// no longer constitutes part of a
    /// valid stack. Null is returned if
    /// the node was still in use as part
    /// of some other stack.</returns>

    public StackNode DetachFromStackNode()
    {
        if (TargetNode != null
            && --TargetNode.ReferenceCount <= 0)
            return TargetNode;
        return null;
    }

    /// <summary>
    /// Recursive stack walking algorithm that extracts all the
    /// paths of a given depth.
    /// </summary>
    /// <param name="depth">The depth to search below the stack top</param>
    /// <param name="foundPaths">Where to put all the found paths</param>
    /// <param name="path">A current path that is being built up
    /// while performing a recursive walk of the stack.</param>
    /// <param name="linkRequiredInPath">For some of the tree
    /// walks, we should only include a path if it includes
    /// a particular stack link somewhere in the path. Set
    /// to null to include all eligible paths.</param>

    protected void WalkStack(int depth, List<StackLink[]> foundPaths,
        StackLink[] path, StackLink linkRequiredInPath)
    {
        if (path == null)
            throw new ArgumentException("Parmeter 'path' cannot be null in WalkStack");
        if (foundPaths == null)
            throw new ArgumentException("parameter 'foundPaths' cannot be null in WalkStack");

        // If a particular stack link is required somewhere
        // in the path, find out if this path has it.

        bool canIncludePath = linkRequiredInPath == null || this == linkRequiredInPath;

        // Fill in the current level in the
        // stack with the current found stack link

        if (depth >= 0)
            path[depth] = this;

        // When we have descended to the specified depth, 
        // output the path that has been found to the
        // list of found paths.

        if (depth <= 0)
        {
            if (canIncludePath)
                foundPaths.Add((StackLink[])path.Clone());
        }
        else
            TargetNode.WalkStack
            (
                depth - 1,
                foundPaths,
                path,
                canIncludePath ?
                    null : linkRequiredInPath
            );

        // Now try the sibling alternative paths

        Next?.WalkStack
                (depth, foundPaths, path, linkRequiredInPath);
    }

    /// <summary>
    /// Implementation of the free list for
    /// second hand stack links.
    /// </summary>

    StackLink IFreeable<StackLink>.NextFree
    {
        get => Next;
        set => Next = value;
    }

    public override string ToString() => string.Format("{0:X}: {1} Tgt={2:X}, Nxt={3:X}",
            GetHashCode(), Token.Type,
            TargetNode == null ? 0 : TargetNode.GetHashCode(),
            Next == null ? 0 : Next.GetHashCode());
}
