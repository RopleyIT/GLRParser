using Xunit;
using SentenceParser;

namespace SentenceParserTests
{
    public class SentenceTests
    {
        [Fact]
        public void TestSentenceParser()
        {
            string sentence =
                "The hairy rabbit noisily licks a pink human.";
            InlineSentenceParser theParser
                = new ();
            SentenceParser.SentenceParser parserInstance
                = theParser.ParseSentence(sentence);
            Assert.True(string.IsNullOrEmpty(parserInstance.Errors));
            Assert.Equal(0, parserInstance.PastCount);
            Assert.Equal(1, parserInstance.PresentCount);
            Assert.Equal(1, parserInstance.SingularCount);
            Assert.Equal(0, parserInstance.PluralCount);
            Assert.Equal(2, parserInstance.AdjectivesCount);
            Assert.Equal("hairy", parserInstance.AdjectiveList[0]);
            Assert.Equal("pink", parserInstance.AdjectiveList[1]);
        }
    }
}
