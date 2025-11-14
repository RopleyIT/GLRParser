using SentenceParser;

namespace SentenceParserTests;

public class SentenceTests
{
    [TestMethod]
    public void TestSentenceParser()
    {
        string sentence =
            "The hairy rabbit noisily licks a pink human.";
        InlineSentenceParser theParser
            = new();
        SentenceParser.SentenceParser parserInstance
            = theParser.ParseSentence(sentence);
        Assert.IsTrue(string.IsNullOrEmpty(parserInstance.Errors));
        Assert.AreEqual(0, parserInstance.PastCount);
        Assert.AreEqual(1, parserInstance.PresentCount);
        Assert.AreEqual(1, parserInstance.SingularCount);
        Assert.AreEqual(0, parserInstance.PluralCount);
        Assert.AreEqual(2, parserInstance.AdjectivesCount);
        Assert.AreEqual("hairy", parserInstance.AdjectiveList[0]);
        Assert.AreEqual("pink", parserInstance.AdjectiveList[1]);
    }
}
