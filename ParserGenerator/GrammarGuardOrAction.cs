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

using System.Text.RegularExpressions;

namespace ParserGenerator;

/// <summary>
/// Represents the name of a guard or action function,
/// and any attached code fragment that will become
/// the body of the guard function.
/// </summary>
/// <remarks>
/// Constructor
/// </remarks>
/// <param name="name">The name the function will have</param>
/// <param name="code">The code for the guard function</param>
/// <param name="guardType">The data type for the argument passed
/// to a guard function</param>

public class GrammarGuardOrAction(string name, string code = null, string guardType = null)
{
    /// <summary>
    /// The name of the guard or action function
    /// </summary>

    public string Name
    {
        get;
        set;
    } = name;

    /// <summary>
    /// The code fragment that is the implementation
    /// of the guard or action function
    /// </summary>

    public string Code
    {
        get;
        private set;
    } = code;

    private static readonly Regex RxDollarArg 
        = new(@"\$([0-9]+)", RegexOptions.Compiled);

    /// <summary>
    /// Validate the $ arguments on the grammar rule to ensure
    /// they are in range for the grammar item the code follows
    /// </summary>
    /// <param name="argCount">The number of different values
    /// the argument can take. A value of 4 would permit $0 to $3
    /// and disallow any others.</param>
    /// <param name="location">String that identifies which grammar
    /// item the code fragment follows.</param>
    /// <returns>An error message if outside range. The empty
    /// string if the validation succeeds.</returns>

    public string ValidateCodeArguments(int argCount, string location)
    {
        if (Code == null)
            return string.Empty;

        MatchCollection mList = RxDollarArg.Matches(Code);
        if (mList != null)
        {
            foreach (Match m in mList)
            {
                if (!int.TryParse(m.Groups[1].Value, out int argIndex) || argIndex >= argCount)
                    return $"{m.Groups[0].Value} outside valid range " +
                        $"($0 to ${argCount - 1}) in code for {location}";
            }
        }
        return string.Empty;
    }

    /// <summary>
    /// The data type used for the single argument passed to
    /// a guard function. This is the expected type for the
    /// argument given the type of input token/event that
    /// was received from the tokeniser or the type of the
    /// token value for a non-terminal token being reduced.
    /// </summary>

    public string GuardType
    {
        get;
        private set;
    } = guardType;
}
