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

namespace Parsing;

/// <summary>
/// The interface expected by the parser
/// generator in order to output the
/// executable code for a parser.
/// </summary>

public interface IGrammarOutput
{
    /// <summary>
    /// Given the list of item sets in the grammar
    /// and the list of productions, generate the
    /// necessary output code that will become the
    /// parser for the grammar.
    /// </summary>
    /// <param name="gram">The data structures
    /// needed to construct an output parser</param>
    /// <param name="verbose">True to include more debugging
    /// information in the output state strings</param>
    /// <param name="errRecovery">True to enable
    /// stack-unwinding error recovery in
    /// output parser.</param>
    /// <param name="isGLR">Set true to generate
    /// output code for a Generalised LR(1) parser
    /// that can support ambiguous grammars. Set
    /// false for traditional LR(1) parsers.</param>

    void RenderStateTables(Grammar gram, bool verbose, bool errRecovery, bool isGLR);
}
