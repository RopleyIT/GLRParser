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

using Parsing;
using System.Collections.Generic;

namespace ParserParserTests
{
    /// <summary>
    /// Test parser action and guard implementation
    /// </summary>

    public class InlineTestParser : Parser
    {
        public int PresentCount
        {
            get;
            set;
        }

        public int PastCount
        {
            get;
            set;
        }

        public int PluralCount
        {
            get;
            set;
        }

        public int SingularCount
        {
            get;
            set;
        }

        public int AdjectivesCount
        {
            get;
            set;
        }

        public List<string> AdjectiveList
        {
            get;
            set;
        }

        // Default constructor

        public InlineTestParser() => AdjectiveList = new List<string>();

        // Actions

        public void BumpPresentTense() => PresentCount++;

        public void BumpPastTense() => PastCount++;

        public void BumpPlural() => PluralCount++;

        public void BumpSingular() => SingularCount++;

        public void BumpAdjectives() => AdjectivesCount++;

        public void BumpAdjectiveList(int count) => AdjectivesCount += count;

        public void AppendAdjective(string adjective)
        {
            if (AdjectiveList == null)
                AdjectiveList = new List<string>();
            AdjectiveList.Add(adjective);
        }

        public void AppendAdjectiveList(IList<string> adjectives)
        {
            foreach (string s in adjectives)
                AdjectiveList.Add(s);
        }

        // Guards

        public bool PluralNoun(object arg) => arg.ToString().EndsWith("s");

        public bool SingularVerb(object arg) => arg.ToString().EndsWith("s")
                || arg.ToString().EndsWith("ed");

        public bool PluralVerb(object arg) => !arg.ToString().EndsWith("s");

        public bool Past(object arg) => arg.ToString().EndsWith("ed");
    }
}
