using System;
using System.IO;
using System.Text;
using Parsing;

namespace SentenceParser
{
    /// <summary>
    /// A manager class for parsing sentences.
    /// </summary>

    public class InlineSentenceParser
    {
        private readonly bool parserBuilt = false;

        /// <summary>
        /// Any errors encountered while building the
        /// parser object itself, i.e. errors in the
        /// input grammar for the sentence parser.
        /// </summary>

        public string ParsingErrors
        {
            get;
            private set;
        }

        /// <summary>
        /// The state machine description for the
        /// sentence parser.
        /// </summary>

        public string ParserStateTables
        {
            get;
            private set;
        }

        /// <summary>
        /// Optional output stream for verbose parsing output from the parser
        /// </summary>
        
        public TextWriter DebugStream
        {
            get;
            set;
        }

        /// <summary>
        /// Parse a sentence and analyze it.
        /// </summary>
        /// <param name="sentence">The sentence to be parsed.</param>
        /// <returns>The parser object, whose properties
        /// contain the analysis results.</returns>

        public SentenceParser ParseSentence(string sentence)
        {
            // The first time ParseSentence is called, 
            // read the input grammar from a data
            // file in the executable folder

            if (!parserBuilt)
            {
                // Find the path to the grammar file so that it can be parsed into
                // a dynamic assembly at runtime. The grammar file is copied to the
                // executable folder on build as if it were a resource.

                string grammarFileLocation = typeof(SentenceParser).Assembly.Location;
                grammarFileLocation = Path.GetDirectoryName(grammarFileLocation);
                grammarFileLocation = Path.Combine(grammarFileLocation, "SentenceParser.g");

                using StreamReader grammarStream = new(grammarFileLocation);
                StringBuilder tableString = new();
                TextWriter tabOut = new StringWriter(tableString);
                try
                {
                    ParserFactory<SentenceParser>.InitializeFromGrammar
                    (
                        grammarStream,
                        tabOut,
                        false,
                        false,
                        false
                    );

                    ParserStateTables = tableString.ToString();
                }
                catch (ArgumentException x)
                {
                    // If the input grammar was faulty, just escape from
                    // the attempted parse before trying to parse a sentence.
                    // By returning null here we are telling the caller that
                    // the sentence parsing grammar is at fault. Later, when
                    // we run the sentence parser, even if there is a fault
                    // in the input sentence, we still return a reference to
                    // the parser.

                    ParsingErrors = x.Message;
                    return null;
                }
            }

            // Now demonstrate the creation of a parser
            // for parsing the actual sentence passed in

            SentenceParser sentenceParser
                = ParserFactory<SentenceParser>.CreateInstance();
            sentenceParser.DebugStream = DebugStream;

            // Create the input tokeniser, connecting it to the
            // list of terminal token values from the parser

            SentenceTokeniser tt = new(new StringReader(sentence), sentenceParser.Tokens);

            // Now execute the parse. The return value is not
            // used, as the caller looks at the Errors property
            // in the SentenceParser clone object to find if
            // there were any faults in the grammar.

            sentenceParser.Parse(tt);
            return sentenceParser;
        }
    }
}
