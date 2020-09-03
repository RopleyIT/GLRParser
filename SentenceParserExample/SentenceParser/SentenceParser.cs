using System.Collections.Generic;
using System.IO;
using System.Text;
using Parsing;

namespace SentenceParser
{
    /// <summary>
    /// Application-specific parts of the sentence parsing
    /// parser class. Mostly this contains the properties
    /// that describe the results of analysing a correctly
    /// constructed sentence. Note that the sentences used
    /// in this demo must be made up from a limited
    /// vocabulary, as described in SentenceTokeniser.cs.
    /// </summary>

    public class SentenceParser : Parser
    {
        private readonly StringBuilder errMessages;

        /// <summary>
        /// Error messages from parser
        /// </summary>

        public string Errors
        {
            get
            {
                if (errMessages == null)
                    return string.Empty;
                else
                    return errMessages.ToString();
            }
        }

        /// <summary>
        /// Constructor sets up the new error stream, since
        /// on cloning, nothing is shared from the
        /// prototype parser other than the parser tables.
        /// </summary>

        public SentenceParser()
        {
            errMessages = new StringBuilder();
            base.ErrStream = new StringWriter(errMessages);
        }

        /// <summary>
        /// The number of verbs/sentences in the present tense
        /// </summary>

        public int PresentCount
        {
            get;
            set;
        }

        /// <summary>
        /// The number of verbs/sentences in the past tense
        /// </summary>

        public int PastCount
        {
            get;
            set;
        }

        /// <summary>
        /// The number of sentences for which the subject
        /// of the sentence is plural
        /// </summary>

        public int PluralCount
        {
            get;
            set;
        }

        /// <summary>
        /// The number of sentences for which the subject
        /// of the sentence is singular
        /// </summary>

        public int SingularCount
        {
            get;
            set;
        }

        /// <summary>
        /// The number of adjectives used
        /// </summary>

        public int AdjectivesCount
        {
            get;
            set;
        }

        /// <summary>
        /// The list of adjectives found
        /// </summary>

        public List<string> AdjectiveList
        {
            get;
            set;
        }

        // Guard conditions used on tokens within
        // the SentenceParser.g grammar

        public bool PluralNoun(object arg) => arg.ToString().EndsWith("s");

        public bool SingularVerb(object arg) => arg.ToString().EndsWith("s")
                || arg.ToString().EndsWith("ed");

        public bool PluralVerb(object arg) => !arg.ToString().EndsWith("s");

        public bool Past(object arg) => arg.ToString().EndsWith("ed");
    }
}
