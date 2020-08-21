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

namespace Parsing
{
    /// <summary>
    /// Represents one path through the parser
    /// stack trellis that is a candidate for
    /// reduction
    /// </summary>

    public class GeneralisedPath
    {
        /// <summary>
        /// Factory. Given the production that will
        /// be used to reduce the stack, and the
        /// top node for the stack we are accumulating 
        /// paths, create the set of generalised paths
        /// that descend from this stack node and
        /// that have the reduction depth of the
        /// production used for reduction.
        /// </summary>
        /// <param name="targetList">The existing
        /// queue of reduction paths to which the
        /// additional paths will be accumulated.</param>
        /// <param name="prod">The production that will
        /// be used to reduce the stack.</param>
        /// <param name="topNode">The stacktop
        /// node from which paths will be found.</param>
        /// <param name="linkRequiredInPath">
        /// If set to a non-null value, indicates
        /// a stack link that must be present in
        /// a path for that path to be included
        /// in the set of added paths. If null,
        /// all suitable paths are included.</param>

        public static void AddGeneralisedPaths
        (
            Queue<GeneralisedPath> targetList,
            ParserProduction prod,
            StackNode topNode,
            StackLink linkRequiredInPath
        )
        {
            if (targetList == null)
                throw new ArgumentException("Null targetList parameter");
            if (prod == null)
                throw new ArgumentException("Null prod parameter");
            if (topNode == null)
                throw new ArgumentException("Null topNode parameter");

            List<StackLink[]> paths = topNode
                .FindPathsOfDepth(prod.TokenCount, linkRequiredInPath);
            foreach (StackLink[] path in paths)
                targetList.Enqueue(new GeneralisedPath
                {
                    TopNode = topNode,
                    Production = prod,
                    TrellisPath = path
                });
        }

        /// <summary>
        /// The production by which the reduction
        /// will take place
        /// </summary>

        public ParserProduction Production
        {
            get;
            private set;
        }

        /// <summary>
        /// The path through the trellis that
        /// the reduction will be applied to.
        /// Note that the first element in
        /// the array is actually a StackNode
        /// reference to the topmost node in
        /// the particular stack of interest.
        /// The remaining elements are the
        /// stack links between the nodes
        /// lower in the stack.
        /// </summary>

        public StackLink[] TrellisPath
        {
            get;
            private set;
        }

        public StackNode TopNode
        {
            get;
            private set;
        }
    }
}
