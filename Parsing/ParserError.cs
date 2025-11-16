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


namespace Parsing;

/// <summary>
/// Container type used to return action errors
/// from action functions into the parser
/// </summary>

public class ParserError
{
    /// <summary>
    /// Set true to terminate the parse.
    /// Set false to allow the parse to
    /// continue with a warning.
    /// </summary>

    public bool Fatal
    {
        get;
        set;
    }

    /// <summary>
    /// The error message to
    /// report back to the parser
    /// </summary>

    public string Message
    {
        get;
        set;
    }
}
