options
{
    using Parsing,
    namespace SentenceParser,
    parserclass SentenceParser
}
events
{
    UNKNOWN = 5,
    NOUN <string> = 10,
    VERB <string> = 15,
    ADJECTIVE <string> = 20,
    ADVERB <string> = 25,
    THE,
    A,
    PERIOD
}
guards
{
    PluralNoun,
    SingularVerb,
    PluralVerb,
    Past
}
grammar(para)
{
    para: 
        sentence
    |   para sentence
    ;

    sentence:
        presentSentence 
        {
            PresentCount++;
        }
    |   pastSentence 
        {
            PastCount++;
        }
    ;

    presentSentence:
        adverbs nounPhrase[!PluralNoun] verb[!Past & SingularVerb] nounPhrase adverbs PERIOD
        {
            SingularCount++;
        }
    |   adverbs nounPhrase[PluralNoun] verb[!Past & PluralVerb] nounPhrase adverbs PERIOD
        {
            PluralCount++;
        }
    ;

    pastSentence:
        adverbs nounPhrase[!PluralNoun] verb[Past & SingularVerb] nounPhrase adverbs PERIOD
        {
            SingularCount++;
        }
    |   adverbs nounPhrase[PluralNoun] verb[Past & PluralVerb] nounPhrase adverbs PERIOD
        {
            PluralCount++;
        }
    ;

    nounPhrase:
        THE adjectives NOUN
    |   A adjectives NOUN[!PluralNoun]
    |   adjectives NOUN[PluralNoun]
    ;

    adjectives:
        adjectives ADJECTIVE
        {
            AdjectivesCount++;
            if (AdjectiveList == null)
                AdjectiveList = new List<string>();
            AdjectiveList.Add($1);
        }
    |
    ;

    verb:
        adverbs VERB
    ;

    adverbs:
        ADVERB
    |
    ;
}
