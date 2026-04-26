# IPFLang Formal Grammar (EBNF)

This document presents the complete formal grammar of IPFLang in Extended Backus-Naur Form (EBNF). It is the authoritative reference for the language's syntax: every IPFLang program accepted by the reference parser conforms to the productions below.

For a tutorial walkthrough of the language with examples, see [IPFLang_syntax.md](IPFLang_syntax.md). For the formal type system, see [type_system_manual.md](type_system_manual.md).

---

## Grammar

```
(* Program Structure *)
<program>              ::= <comment>* <version>? <group>* <input_definition>*
                           <fee_computation>+ <verification>* <return>*

(* Comments *)
<comment>              ::= "#" {<any_char> - <newline>} <newline>

(* Version Declaration *)
<version>              ::= "VERSION" <string> "EFFECTIVE" <date>
                           ["DESCRIPTION" <string>] ["REFERENCE" <string>]

(* Group Definition *)
<group>                ::= "DEFINE" "GROUP" <identifier> "AS" <string>
                           "WITH" "WEIGHT" <number>

(* Input Definitions *)
<input_definition>     ::= "DEFINE" <input_type> <identifier> "AS" <string>
                           <type_specifics> ["DEFAULT" <default_value>]
                           ["GROUP" <identifier>] "ENDDEFINE"
<input_type>           ::= "LIST" | "MULTILIST" | "NUMBER" | "BOOLEAN" | "DATE" | "AMOUNT"
<type_specifics>       ::= <choices> | <numeric_constraint> | <date_constraint>
                         | <currency_spec> | ε
<choices>              ::= <choice>+
<choice>               ::= "CHOICE" <identifier> "AS" <string>
<numeric_constraint>   ::= "BETWEEN" <number> "AND" <number>
<date_constraint>      ::= "BETWEEN" <date> "AND" (<date> | "TODAY")
<currency_spec>        ::= "CURRENCY" <currency_code>
<default_value>        ::= <number> | <identifier> | <boolean_literal> | <date>
                         | <currency_literal> | <identifier_list>
<identifier_list>      ::= <identifier> ("," <identifier>)*
<boolean_literal>      ::= "TRUE" | "FALSE"

(* Fee Computation *)
<fee_computation>      ::= "COMPUTE" "FEE" <identifier> [<type_params>]
                           ["RETURN" <currency_code>] ["OPTIONAL"]
                           <let_statement>* <case_or_yield>*
                           "ENDCOMPUTE"
<type_params>          ::= "<" <type_variable> ">"
<type_variable>        ::= <upper_letter>
<let_statement>        ::= "LET" <identifier> "AS" <expression>
<case_or_yield>        ::= <case_block> | <yield_statement>
<case_block>           ::= "CASE" <condition> "AS" <yield_statement>+ "ENDCASE"
<yield_statement>      ::= "YIELD" <expression> ["IF" <condition>]

(* Verification Directives *)
<verification>         ::= <verify_complete> | <verify_monotonic>
<verify_complete>      ::= "VERIFY" "COMPLETE" "FEE" <identifier>
<verify_monotonic>     ::= "VERIFY" "MONOTONIC" "FEE" <identifier>
                           "WITH" "RESPECT" "TO" <identifier>
                           ["DIRECTION" <direction>]
<direction>            ::= "NonDecreasing" | "NonIncreasing"
                         | "StrictlyIncreasing" | "StrictlyDecreasing"

(* Return Statement *)
<return>               ::= "RETURN" <identifier> "AS" <string>

(* Expressions *)
<expression>           ::= <term> (("+" | "-") <term>)*
<term>                 ::= <factor> (("*" | "/") <factor>)*
<factor>               ::= <number> | <currency_literal> | <identifier>
                         | <property_access> | <function_call> | "(" <expression> ")"
<currency_literal>     ::= <number> "<" (<currency_code> | <type_variable>) ">"
<property_access>      ::= <identifier> "!" <property_name>
<property_name>        ::= "COUNT" | "YEARSTONOW" | "MONTHSTONOW" | "DAYSTONOW"
                         | "MONTHSTONOW_FROMLASTDAY"
<function_call>        ::= ("ROUND" | "FLOOR" | "CEIL") "(" <expression> ")"
                         | "CONVERT" "(" <expression> "," <currency_code> "," <currency_code> ")"

(* Conditions *)
<condition>            ::= <or_condition>
<or_condition>         ::= <and_condition> ("OR" <and_condition>)*
<and_condition>        ::= <primary_condition> ("AND" <primary_condition>)*
<primary_condition>    ::= <expression> <comparison_op> <expression>
                         | <identifier> ("IN" | "NIN") <identifier>
                         | <identifier>   (* Boolean variable reference *)
                         | "(" <condition> ")"

<comparison_op>        ::= "EQ" | "NEQ" | "GT" | "LT" | "GTE" | "LTE"

(* Lexical Elements *)
<identifier>           ::= <letter> (<letter> | <digit> | "_")*
<number>               ::= <digit>+ ("." <digit>+)?
<string>               ::= "'" {<any_char> - "'"} "'"
<currency_code>        ::= <upper_letter> <upper_letter> <upper_letter>
<date>                 ::= <day> "." <month> "." <year> | "TODAY"
<day>                  ::= <digit> <digit>?
<month>                ::= <digit> <digit>?
<year>                 ::= <digit> <digit> <digit> <digit>
<letter>               ::= "A" | ... | "Z" | "a" | ... | "z"
<upper_letter>         ::= "A" | ... | "Z"
<digit>                ::= "0" | ... | "9"
```

A bare `<identifier>` in a condition context must reference a BOOLEAN input variable; it evaluates to that variable's Boolean value.
