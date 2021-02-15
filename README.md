## The GLRParser Toolkit
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

The full documentation is available in the 
[Wiki for this project](https://github.com/RopleyIT/GLRParser/wiki).

### Licensing
This product is published under the [standard MIT License](https://opensource.org/licenses/MIT). The specific wording for this license is as follows:

Copyright 2018 [Ropley Information Technology Ltd.](http://www.ropley.com)
Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
