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

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Parsing;
using System.Collections.Generic;

namespace GLRParserTests;

[TestClass]
public class StackTests
{
    /// <summary>
    /// Prove that the algorithm used to find all the paths
    /// through the trellis based GLR stack of depth N are found.
    /// </summary>

    [TestMethod]
    public void TestStackPathExtraction()
    {
        // Set up a depth 4 stack with several paths

        StackNode node1 = new()
        {
            State = 100,
            ReferenceCount = 2,
            Token = new ParserToken(200)
        };
        StackNode node2 = new()
        {
            State = 101,
            ReferenceCount = 1,
            Token = new ParserToken(201)
        };
        node1 = new StackNode
        {
            State = 110,
            ReferenceCount = 1,
            Token = new ParserToken(210),
            TargetNode = node1
        };
        node2 = new StackNode
        {
            State = 111,
            ReferenceCount = 2,
            Token = new ParserToken(211),
            TargetNode = node2,
            Next = new StackLink
            {
                TargetNode = node1.TargetNode,
                Token = new ParserToken(777)
            }
        };
        node1 = new StackNode
        {
            State = 120,
            ReferenceCount = 1,
            Token = new ParserToken(220),
            TargetNode = node1,
            Next = new StackLink
            {
                TargetNode = node2,
                Token = new ParserToken(787)
            }
        };
        node2 = new StackNode
        {
            State = 121,
            ReferenceCount = 1,
            Token = new ParserToken(221),
            TargetNode = node2
        };
        node1 = new StackNode
        {
            State = 130,
            ReferenceCount = 1,
            Token = new ParserToken(230),
            TargetNode = node1,
            Next = new StackLink
            {
                TargetNode = node2,
                Token = new ParserToken(797)
            }
        };

        List<StackLink[]> foundPaths = node1.FindPathsOfDepth(3, null);
        Assert.HasCount(5, foundPaths);
        Assert.AreEqual(787, foundPaths[1][1].Token.Type);
        Assert.AreEqual(797, foundPaths[3][2].Token.Type);
        Assert.AreEqual(777, foundPaths[4][0].Token.Type);
    }
}
