# GLRParser
The GLRParser toolkit is an LR(1) and GLR parser and finite state machine generator 
for use with the C# language on the .NET Core platform. It can be used to convert
a formal grammar into C# source code that can parse data or event sequences conforming 
to that grammar. 

Alternatively it can be used as an inline parser with dynamically changing input grammars, 
to generate and replace parsers for a changing grammar at run-time. 

Another feature that differentiates this parser from the many other parser generators 
available is that it allows guard conditions to be associated with each input token. 
This enables the tool to be used for grammar-driven finite state machine construction.

Applications for this toolkit are broad, and include: language compilers; structured 
document scanners and interpreters; syntax highlighting and verification; workflow engines 
built from scenario descriptions; state machines described by a formal grammar.
