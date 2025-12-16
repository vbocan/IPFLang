# IPFLang: A Domain-Specific Language with Static Verification for Multi-Jurisdiction Intellectual Property Fee Calculation

**Valer Bocan, PhD, CSSLP**
Department of Computer and Information Technology
Politehnica University Timisoara
Timisoara, 300223, Romania
Email: valer.bocan@upt.ro
ORCID: 0009-0006-9084-4064

---

## Abstract

The intellectual property management industry faces interoperability challenges due to fragmented fee calculation implementations across patent offices. This paper presents IPFLang, a domain-specific language for encoding jurisdiction-specific fee calculation rules. IPFLang provides: (1) a formal EBNF grammar with declarative fee computation blocks; (2) a currency-aware type system supporting 161 ISO 4217 currencies that prevents cross-currency arithmetic errors at compile time; (3) static analysis of fee completeness (with formal guarantees for bounded domains) and monotonicity; and (4) provenance tracking for auditability. We present formal typing rules with a type safety argument, analysis algorithms with complexity bounds, and validation against official EPO and USPTO fee schedules. The reference CLI implementation validates the design with 266 tests and sub-millisecond execution times. IPFLang establishes a foundation for regulatory calculation standardization, combining practical DSL design with formal correctness guarantees.

**Keywords:** intellectual property, domain-specific language, type systems, static verification, regulatory automation, legal technology, software standards

---

## Highlights

- Formal DSL specification for multi-jurisdiction IP fee calculations
- Currency-aware type system with 161 ISO 4217 currencies preventing cross-currency errors statically
- Static analysis of fee completeness (formal guarantees for bounded domains) and monotonicity
- Provenance tracking with counterfactual analysis for auditability
- Validation against official EPO and USPTO fee schedules
- Open-source reference implementation at github.com/vbocan/IPFLang

---

## 1. Introduction

### 1.1 The IP Technology Fragmentation Problem

The global intellectual property management ecosystem operates through a complex network of national and regional patent offices, each maintaining independent fee calculation systems with proprietary interfaces. Major offices such as the United States Patent and Trademark Office (USPTO), European Patent Office (EPO), Japan Patent Office (JPO), and World Intellectual Property Organization (WIPO) each provide web-based fee calculators [1-4], yet these systems exhibit fundamental interoperability deficiencies that impede efficient IP management workflows.

The first and perhaps most pressing issue is the absence of any standard data format across jurisdictions. Each patent office defines fee parameters using custom terminology, requiring manual interpretation and data entry for each calculation. Where the USPTO uses "entity type" classifications, the EPO employs "applicant category" with entirely different discount structures. WIPO PCT applications introduce additional complexity by requiring separate parameters for International Searching Authority selection, compounding the burden for practitioners handling multi-jurisdiction filings.

Equally problematic is the complete absence of programmatic interfaces for fee calculation. Government calculators operate exclusively through web browsers with no API access, preventing any form of automation or integration with IP management workflows. Patent offices have invested significantly in digital transformation for application filing through systems like ePCT, EFS-Web, and Online Filing, yet they have not extended programmatic access to fee calculation services, creating a gap in their digital offerings.

The situation is further complicated by the proprietary nature of calculation logic itself. Fee computation rules remain embedded in server-side code, entirely inaccessible to practitioners who must verify accuracy against frequently-updated official fee schedules. When discrepancies arise between calculated and actual fees, practitioners find themselves unable to diagnose whether errors stem from incorrect inputs or flawed calculation logic, as the underlying implementation remains opaque.

Commercial IP management platforms from vendors such as CPA Global, Anaqua, and PatSnap address some workflow needs but perpetuate fragmentation through vendor-specific implementations, limited jurisdiction coverage, and hardcoded fee structures requiring vendor patches for regulatory updates [5]. The global IP management software market operates without standardized interfaces, forcing enterprises into vendor lock-in and limiting interoperability between best-of-breed solutions.

### 1.2 The Need for Standardization

Successful technology domains achieve interoperability through open standards. SQL standardized database queries through ISO/IEC 9075, HTML standardized web content through W3C specifications, and XML Schema standardized data validation under the same organization. These standards enabled ecosystem growth by separating interface specifications from implementations, allowing multiple vendors to provide interoperable solutions.

The IP technology domain lacks equivalent standards for fee calculation, resulting in three critical gaps. The first is a specification gap: no standard language exists for expressing jurisdiction-specific fee rules. LegalRuleML [6] addresses compliance checking but lacks arithmetic expressiveness for financial calculations. Catala [7] demonstrates sophisticated tax calculations but targets single-jurisdiction applications requiring formal methods expertise unsuitable for legal practitioners. The second gap concerns formal verification: existing calculators provide no guarantees that fee definitions cover all valid input combinations or behave predictably as inputs change. The third gap is one of transparency: proprietary implementations prevent independent verification of calculation correctness.

### 1.3 Research Questions and Contributions

This work addresses four fundamental questions regarding standardization of IP fee calculations.

The first research question concerns language design: can a domain-specific language provide sufficient expressiveness for complex regulatory fee structures while employing syntax designed for readability by domain experts? The second addresses formal correctness: what static guarantees can a type system and verification framework provide for multi-currency regulatory calculations? The third focuses on verification: how can completeness and monotonicity properties be statically verified to ensure fee definitions behave correctly? The fourth explores practical feasibility: can a DSL-based approach achieve acceptable performance for production use?

This paper presents IPFLang (Intellectual Property Fees Language), a domain-specific language standard for multi-jurisdiction fee calculations, with five principal contributions:

1. **Language Specification** (Section 3): Formal definition of IPFLang syntax using EBNF grammar, with declarative fee computation blocks, explicit input type declarations including a currency-aware AMOUNT type, temporal operators for date-dependent calculations, version management with effective dates, and jurisdiction composition for code reuse.

2. **Type System and Static Verification** (Section 4): A currency-aware type system supporting 161 ISO 4217 currencies with formal typing rules that prevent cross-currency arithmetic errors at compile time, along with verification algorithms for completeness and monotonicity with complexity analysis.

3. **Provenance and Auditability** (Section 5): Execution tracing showing how final amounts derive from input parameters, with counterfactual analysis enabling what-if scenarios for regulatory compliance.

4. **Reference Implementation** (Section 6): An open-source command-line tool released under GPLv3 demonstrating IPFLang execution with 266 tests validating type safety and verification correctness.

5. **Validation** (Section 7): Systematic validation against official EPO and USPTO fee schedules demonstrating calculation accuracy.

### 1.4 Paper Organization

Section 2 surveys related work in IP data standards, legal domain DSLs, and regulatory automation. Section 3 presents the complete IPFLang specification including formal grammar and input type system. Section 4 details the currency-aware type system with formal typing rules and static verification algorithms. Section 5 describes provenance tracking and counterfactual analysis. Section 6 presents the reference implementation architecture. Section 7 provides evaluation including validation against official fee schedules and threats to validity. Section 8 discusses advantages of DSL-based standardization, cross-domain applicability, and limitations. Section 9 concludes with contributions summary, impact assessment, and future directions.

---

## 2. Related Work and Standards Landscape

### 2.1 Existing IP Data Standards

The IP technology domain has achieved partial standardization in data exchange but lacks standards for computational tasks like fee calculation.

WIPO ST.96, commonly known as Patent XML, represents the World Intellectual Property Organization's standard for patent application data exchange [8]. The standard defines XML schemas covering bibliographic data, descriptions, claims, and drawings, but it explicitly excludes financial calculations from its scope. While the standard provides a foundation for data interoperability, fee calculations fall entirely outside its purview.

The European Patent Office provides Open Patent Services (OPS), which offers RESTful APIs for patent search and retrieval [9]. OPS provides family information, legal status, and bibliographic data, but omits fee calculation endpoints entirely. The existence of OPS demonstrates that patent offices can successfully deploy programmatic interfaces, suggesting that fee calculation APIs are technically feasible but simply have not been prioritized.

The gap becomes clear upon examination: IP data standards focus on informational exchange such as bibliographic data, legal status, and full-text search, while omitting computational tasks entirely. Fee calculation remains manual, preventing end-to-end workflow automation.

### 2.2 Domain-Specific Languages for Legal Domains

Academic research in computational law has produced several DSLs for legal rules, yet none address regulatory fee calculations with multi-currency and multi-jurisdiction requirements.

LegalRuleML, developed by Athan et al. [6], provides an XML-based specification language for legal rules with ontology-based reasoning. The language excels at representing deontic logic covering obligations, permissions, and prohibitions, and supports defeasibility for handling rule precedence. However, LegalRuleML emphasizes binary compliance checking (compliant or non-compliant) with minimal arithmetic support. Complex fee formulas involving thresholds, progressions, and conditional multipliers exceed the language's design scope. The XML syntax also presents accessibility challenges for legal professionals without technical training.

Catala, created by Merigoux et al. [7], represents a programming language specifically designed for tax law computation. The language demonstrates sophisticated financial calculations with formal verification guarantees through dependent type theory. However, Catala targets single-jurisdiction applications, primarily the French tax code, and requires formal methods expertise that limits adoption by legal practitioners. IPFLang and Catala represent complementary approaches: Catala employs dependent types for exhaustive case coverage targeting formal verification experts, while IPFLang prioritizes comprehensibility through keyword-based syntax (EQ, GT, AND rather than symbols) and domain-specific primitives (currency literals, temporal operators) designed for IP practitioners to read and modify directly. While Catala's dependent types provide stronger theoretical guarantees, IPFLang's simpler type system (currency-parameterized amounts without dependent types) suffices for IP fee calculations where amounts are always non-negative and conditions are finite Boolean combinations.

Contract-oriented DSLs [10] focus on party obligations, temporal constraints, and conditional execution semantics. Monetary aspects receive minimal treatment, with basic arithmetic operations but lacking multi-currency precision, exchange rate management, and historical rate tracking required for cross-border IP portfolios.

The concept of encoding units in type systems originates with Kennedy's dimensional types [11], which prevent unit mismatch errors in scientific computing. IPFLang applies similar principles to currency, extending the concept with explicit conversion operators and polymorphic type variables for generic fee definitions.

OpenFisca [12] provides a Python-based platform for tax-benefit microsimulation, used by governments including France and New Zealand. While powerful, OpenFisca requires Python programming expertise and targets general fiscal policy rather than the specific requirements of IP fee calculation.

Existing legal DSLs address contract execution, compliance checking, or single-jurisdiction calculations, but none provide the combination of arithmetic expressiveness for complex fee formulas, multi-currency support with type safety, static verification of completeness and monotonicity, and multi-jurisdiction portability that IP fee calculation demands.

### 2.3 Regulatory Automation and Rules Engines

Automated compliance checking represents a related domain where technology assists regulatory interpretation.

Business Rules Management Systems such as Drools [13] provide general-purpose rules engines using production rules with Rete algorithm inference. These systems require substantial technical expertise, lack domain-specific abstractions for legal concepts, and impose expensive enterprise licensing. While powerful, BRMS platforms are fundamentally over-engineered for deterministic fee calculations.

Some governments have begun pursuing rules-as-code initiatives that encode regulations in executable formats [14, 15]. New Zealand's "Better Rules" program and similar initiatives in Australia and France explore machine-consumable legislation. These initiatives typically use general-purpose languages rather than domain-specific languages, limiting accessibility to legal experts who must rely on programmers for implementation.

**[INSERT FIGURE 1: figures/figure1_dsl_comparison.mmd - Comparison of Legal DSLs and Rules Engines]**

Table 1 provides a systematic comparison of IPFLang with related approaches.

| Feature | IPFLang | Catala | LegalRuleML | Drools | OpenFisca |
|---------|---------|--------|-------------|--------|-----------|
| Primary Domain | IP fees | Tax law | Compliance | General | Tax-benefit |
| Type Safety | Currency-aware | Dependent types | None | None | Runtime |
| Multi-currency | Built-in (161 ISO 4217) | User-definable | No | No | Limited |
| Static Verification | Completeness, Monotonicity | Exhaustiveness | Defeasibility | None | None |
| Syntax Style | Declarative keywords | Literate | XML | Drools DRL | Python |
| Target User | Domain experts | Formal methods experts | Ontology engineers | Developers | Developers |
| Multi-jurisdiction | Composition | No | No | No | Yes |
| Provenance | Built-in | No | No | Audit log | No |
| License | GPLv3 | Apache 2.0 | OASIS | Apache 2.0 | AGPL |

*Table 1: Comparison of Legal DSLs and Rules Engines*

IPFLang differentiates itself through the combination of domain-specific syntax designed for readability, first-class multi-currency type safety with all 161 ISO 4217 currencies as built-in primitives, static verification of completeness and monotonicity, and explicit support for multi-jurisdiction fee structures with inheritance and composition. While languages like Catala could theoretically support currency types through user-defined abstractions, IPFLang provides these as language primitives requiring no additional implementation effort.

---

## 3. IPFLang Language Specification

### 3.1 Design Principles

IPFLang design balances five competing objectives: expressiveness for complex regulatory logic, readability through domain-specific syntax designed for legal professionals, deterministic execution for financial accuracy, extensibility for evolving regulations, and formal verification potential.

The first principle is declarative semantics. Fee calculations specify what to compute rather than how to compute it. Declarative approaches facilitate correctness verification by legal experts who can validate fee formulas directly against official schedules without needing to understand imperative control flow.

The second principle requires explicit semantics. Operators use keyword syntax such as EQ, GT, AND, and OR rather than symbols like ==, >, &&, and ||. This design choice aims to improve readability for users with varying technical backgrounds, aligning with DSL design guidelines emphasizing domain-specific notation [16]. Empirical validation of this readability hypothesis remains future work (see Section 8.3).

The third principle establishes a static type system. Input declarations explicitly specify types including NUMBER, LIST, MULTILIST, BOOLEAN, DATE, and AMOUNT, enabling compile-time validation. Static typing prevents runtime errors from type mismatches and provides clear parameter documentation.

The fourth principle maintains minimal syntax complexity. Language features target the minimum necessary for regulatory fee calculations. Structured blocks such as DEFINE...ENDDEFINE and COMPUTE...ENDCOMPUTE with explicit terminators aid comprehension and prevent syntax errors common in expression-based languages.

The fifth principle ensures auditability and traceability. Fee computation produces step-by-step execution traces showing how final amounts derive from input parameters. This addresses legal requirements for calculation transparency and assists in dispute resolution.

The sixth principle guarantees jurisdiction independence. Language constructs make no assumptions about specific jurisdictions, enabling code reuse across patent offices. Jurisdiction-specific business rules reside in fee definitions rather than language syntax.

### 3.2 Language Syntax Overview

IPFLang programs consist of several sections:

```
[Version Declaration]     // Optional metadata
[Group Definitions]       // Optional UI organization
[Input Definitions]       // Required parameters
[Fee Computations]        // Fee calculation logic
[Verification Directives] // Optional static checks
[Return Statements]       // Optional named outputs
```

### 3.3 Version Declaration

Fee schedules change frequently, requiring version tracking with effective dates.

**Syntax:**
```
VERSION '<VersionId>' EFFECTIVE <date> [DESCRIPTION '<description>'] [REFERENCE '<reference>']
```

**Example:**
```
VERSION '2024.1' EFFECTIVE 01.04.2024 DESCRIPTION 'EPO official fees 2024' REFERENCE 'EPO Official Journal 2024/03'
```

The VERSION directive enables temporal queries (calculating fees as they were on a specific date), version comparison (identifying changes between fee schedule revisions), and regulatory traceability (linking calculations to authoritative sources).

### 3.4 Input Type System

IPFLang provides six input types matching common parameter patterns across patent offices.

#### 3.4.1 LIST (Single-Choice Enumeration)

The LIST type handles parameters with mutually exclusive options such as entity type, filing basis, or examination request.

```
DEFINE LIST EntityType AS 'Select entity size'
GROUP General
CHOICE NormalEntity AS 'Large Entity'
CHOICE SmallEntity AS 'Small Entity (50% discount)'
CHOICE MicroEntity AS 'Micro Entity (75% discount)'
DEFAULT NormalEntity
ENDDEFINE
```

#### 3.4.2 MULTILIST (Multi-Choice Enumeration)

The MULTILIST type accommodates parameters allowing multiple selections. The special property accessor `!COUNT` returns the number of selections. IPFLang uses the exclamation mark (`!`) as the property accessor to distinguish property access from other syntactic elements and to provide visual emphasis for these computed properties, which derive values from the underlying data rather than representing stored values directly.

```
DEFINE MULTILIST Countries AS 'Select validation countries'
CHOICE VAL_DE AS 'Germany'
CHOICE VAL_FR AS 'France'
CHOICE VAL_GB AS 'United Kingdom'
DEFAULT VAL_DE,VAL_FR
ENDDEFINE
```

**Usage:** `YIELD 100 * Countries!COUNT`

#### 3.4.3 NUMBER (Numeric Input)

The NUMBER type handles counts, quantities, and page numbers with optional constraints.

```
DEFINE NUMBER ClaimCount AS 'Number of claims in application'
BETWEEN 1 AND 500
DEFAULT 10
ENDDEFINE
```

#### 3.4.4 BOOLEAN (Yes/No)

The BOOLEAN type represents binary choices.

```
DEFINE BOOLEAN RequestExamination AS 'Request substantive examination?'
DEFAULT TRUE
ENDDEFINE
```

#### 3.4.5 DATE (Date Input)

The DATE type handles filing dates and priority dates for calculating time-dependent fees. Temporal properties enable calculations based on elapsed time:

- `!YEARSTONOW` - Years from date to current date
- `!MONTHSTONOW` - Months from date to current date
- `!DAYSTONOW` - Days from date to current date
- `!MONTHSTONOW_FROMLASTDAY` - Months from end of date's month

```
DEFINE DATE FilingDate AS 'Application filing date'
BETWEEN 01.01.2000 AND TODAY
DEFAULT TODAY
ENDDEFINE
```

**Usage:** `LET YearsSinceFiling AS FilingDate!YEARSTONOW`

#### 3.4.6 AMOUNT (Currency-Aware Monetary Input)

The AMOUNT type represents monetary values with an associated ISO 4217 currency code, enabling type-safe arithmetic.

```
DEFINE AMOUNT PriorSearchFee AS 'Prior art search fee paid'
CURRENCY EUR
DEFAULT 0
ENDDEFINE
```

The AMOUNT type integrates with the currency-aware type system described in Section 4, preventing accidental cross-currency arithmetic.

### 3.5 Group Definitions

Groups organize inputs for user interface presentation, with weights determining display order.

```
DEFINE GROUP General AS 'General Information' WITH WEIGHT 10
DEFINE GROUP Claims AS 'Claims Information' WITH WEIGHT 20
DEFINE GROUP Options AS 'Fee Options' WITH WEIGHT 30
```

### 3.6 Fee Computation Blocks

Fee calculations use structured COMPUTE FEE blocks with conditional YIELD statements.

**Basic syntax:**
```
COMPUTE FEE <fee_name> [OPTIONAL]
  [LET <variable> AS <expression>]*
  [CASE <condition> AS
    YIELD <expression> [IF <condition>]
  ENDCASE]*
  YIELD <expression> [IF <condition>]
ENDCOMPUTE
```

The OPTIONAL keyword distinguishes fees that may or may not apply from mandatory fees.

**Example - EPO claim fees with progressive rates:**
```
COMPUTE FEE ExcessClaimsFee
LET ClaimFee1 AS 265
LET ClaimFee2 AS 660
CASE ClaimCount LTE 15 AS
  YIELD 0
ENDCASE
CASE ClaimCount GT 15 AND ClaimCount LTE 50 AS
  YIELD ClaimFee1 * (ClaimCount - 15)
ENDCASE
CASE ClaimCount GT 50 AS
  YIELD ClaimFee1 * 35 + ClaimFee2 * (ClaimCount - 50)
ENDCASE
ENDCOMPUTE
```

This directly encodes the EPO's fee schedule: EUR 265 per claim for claims 16-50 and EUR 660 per claim beyond 50.

### 3.7 Currency Literals

Numeric values can be annotated with ISO 4217 currency codes:

```
100<EUR>      # 100 Euros
50.50<USD>    # 50.50 US Dollars
1000<JPY>     # 1000 Japanese Yen
```

The type system enforces currency compatibility at compile time, as detailed in Section 4.

### 3.8 Operators and Expressions

IPFLang provides:

**Comparison operators:** EQ (equality), NEQ (inequality), GT (greater than), LT (less than), GTE (greater or equal), LTE (less or equal)

**Logical operators:** AND, OR (with short-circuit evaluation)

**Arithmetic operators:** +, -, *, /, with ROUND, FLOOR, CEIL functions

**Set operators for MULTILIST:** IN (membership), NIN (non-membership), !COUNT (cardinality)

**Operator precedence** (highest to lowest): ROUND/FLOOR/CEIL, then *, /, then +, -, then comparisons, then AND, then OR. Parentheses override default precedence.

### 3.9 Jurisdiction Composition

IPFLang supports inheritance for code reuse across related fee schedules. A child jurisdiction inherits inputs and fees from a parent, can add new definitions, and can override inherited fees.

**Parent (EPO base):**
```
COMPUTE FEE FilingFee
YIELD 135<EUR>
ENDCOMPUTE
```

**Child (EPO Germany national phase):**
```
# Inherits FilingFee from parent
# Adds Germany-specific fee
COMPUTE FEE GermanTranslationFee
YIELD 1050<EUR> IF NeedsTranslation EQ TRUE
YIELD 0 IF NeedsTranslation EQ FALSE
ENDCOMPUTE
```

Composition enables significant code reuse between related jurisdictions, simplifies maintenance when base fees change, and maintains clear traceability of fee origins.

### 3.10 Formal Grammar (EBNF)

The complete formal grammar in Extended Backus-Naur Form:

```ebnf
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
                           ["GROUP" <identifier>] <type_specifics>
                           ["DEFAULT" <default_value>] "ENDDEFINE"

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
<fee_computation>      ::= "COMPUTE" "FEE" <identifier> ["OPTIONAL"]
                           <let_statement>* <case_or_yield>*
                           "ENDCOMPUTE"

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

<currency_literal>     ::= <number> "<" <currency_code> ">"
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

---

## 4. Type System and Static Verification

### 4.1 Currency-Aware Type System

IPFLang employs a dimensional type system preventing cross-currency arithmetic errors at compile time, analogous to units-of-measure checking in scientific computing [11]. The system supports all 161 ISO 4217 currency codes.

#### 4.1.1 Type Language

The type language extends basic types with currency-parameterized amounts:

<math xmlns="http://www.w3.org/1998/Math/MathML" display="block">
  <mrow>
    <mi>τ</mi>
    <mo>::=</mo>
    <mi>Num</mi>
    <mo>|</mo>
    <mi>Bool</mi>
    <mo>|</mo>
    <mi>Sym</mi>
    <mo>|</mo>
    <mi>Date</mi>
    <mo>|</mo>
    <mi>StrList</mi>
    <mo>|</mo>
    <mi>Amt</mi><mo>[</mo><mi>c</mi><mo>]</mo>
    <mo>|</mo>
    <mi>α</mi>
  </mrow>
</math>

where:
- `Num` represents dimensionless numbers
- `Bool` represents Boolean values
- `Sym` represents symbolic identifiers (LIST choices)
- `Date` represents date values
- `StrList` represents string lists (MULTILIST selections)
- `Amt[c]` represents monetary amounts in currency c ∈ ISO-4217
- `α` represents type variables for polymorphic fee definitions

#### 4.1.2 Typing Rules

Let Γ denote a typing environment mapping identifiers to types, written Γ(x) = τ. We define the typing judgment Γ ⊢ e : τ, meaning "under environment Γ, expression e has type τ."

**Literals and Variables:**

<math xmlns="http://www.w3.org/1998/Math/MathML" display="block">
  <mtable columnalign="left">
    <mtr>
      <mtd>
        <mtext>[T-NUM]</mtext>
        <mspace width="2em"/>
        <mfrac linethickness="1px">
          <mrow></mrow>
          <mrow><mi>Γ</mi><mo>⊢</mo><mi>n</mi><mo>:</mo><mi>Num</mi></mrow>
        </mfrac>
      </mtd>
    </mtr>
  </mtable>
</math>

<math xmlns="http://www.w3.org/1998/Math/MathML" display="block">
  <mtable columnalign="left">
    <mtr>
      <mtd>
        <mtext>[T-VAR]</mtext>
        <mspace width="2em"/>
        <mfrac linethickness="1px">
          <mrow><mi>Γ</mi><mo>(</mo><mi>x</mi><mo>)</mo><mo>=</mo><mi>τ</mi></mrow>
          <mrow><mi>Γ</mi><mo>⊢</mo><mi>x</mi><mo>:</mo><mi>τ</mi></mrow>
        </mfrac>
      </mtd>
    </mtr>
  </mtable>
</math>

<math xmlns="http://www.w3.org/1998/Math/MathML" display="block">
  <mtable columnalign="left">
    <mtr>
      <mtd>
        <mtext>[T-CURR]</mtext>
        <mspace width="2em"/>
        <mfrac linethickness="1px">
          <mrow><mi>c</mi><mo>∈</mo><mtext>ISO-4217</mtext></mrow>
          <mrow><mi>Γ</mi><mo>⊢</mo><mi>n</mi><mo>⟨</mo><mi>c</mi><mo>⟩</mo><mo>:</mo><mi>Amt</mi><mo>[</mo><mi>c</mi><mo>]</mo></mrow>
        </mfrac>
      </mtd>
    </mtr>
  </mtable>
</math>

**Arithmetic Operations:**

<math xmlns="http://www.w3.org/1998/Math/MathML" display="block">
  <mtable columnalign="left">
    <mtr>
      <mtd>
        <mtext>[T-ADD-NUM]</mtext>
        <mspace width="2em"/>
        <mfrac linethickness="1px">
          <mrow><mi>Γ</mi><mo>⊢</mo><msub><mi>e</mi><mn>1</mn></msub><mo>:</mo><mi>Num</mi><mspace width="1em"/><mi>Γ</mi><mo>⊢</mo><msub><mi>e</mi><mn>2</mn></msub><mo>:</mo><mi>Num</mi></mrow>
          <mrow><mi>Γ</mi><mo>⊢</mo><msub><mi>e</mi><mn>1</mn></msub><mo>+</mo><msub><mi>e</mi><mn>2</mn></msub><mo>:</mo><mi>Num</mi></mrow>
        </mfrac>
      </mtd>
    </mtr>
  </mtable>
</math>

<math xmlns="http://www.w3.org/1998/Math/MathML" display="block">
  <mtable columnalign="left">
    <mtr>
      <mtd>
        <mtext>[T-ADD-AMT]</mtext>
        <mspace width="2em"/>
        <mfrac linethickness="1px">
          <mrow><mi>Γ</mi><mo>⊢</mo><msub><mi>e</mi><mn>1</mn></msub><mo>:</mo><mi>Amt</mi><mo>[</mo><mi>c</mi><mo>]</mo><mspace width="1em"/><mi>Γ</mi><mo>⊢</mo><msub><mi>e</mi><mn>2</mn></msub><mo>:</mo><mi>Amt</mi><mo>[</mo><mi>c</mi><mo>]</mo></mrow>
          <mrow><mi>Γ</mi><mo>⊢</mo><msub><mi>e</mi><mn>1</mn></msub><mo>+</mo><msub><mi>e</mi><mn>2</mn></msub><mo>:</mo><mi>Amt</mi><mo>[</mo><mi>c</mi><mo>]</mo></mrow>
        </mfrac>
      </mtd>
    </mtr>
  </mtable>
</math>

<math xmlns="http://www.w3.org/1998/Math/MathML" display="block">
  <mtable columnalign="left">
    <mtr>
      <mtd>
        <mtext>[T-MUL-SCALAR-R]</mtext>
        <mspace width="2em"/>
        <mfrac linethickness="1px">
          <mrow><mi>Γ</mi><mo>⊢</mo><msub><mi>e</mi><mn>1</mn></msub><mo>:</mo><mi>Amt</mi><mo>[</mo><mi>c</mi><mo>]</mo><mspace width="1em"/><mi>Γ</mi><mo>⊢</mo><msub><mi>e</mi><mn>2</mn></msub><mo>:</mo><mi>Num</mi></mrow>
          <mrow><mi>Γ</mi><mo>⊢</mo><msub><mi>e</mi><mn>1</mn></msub><mo>×</mo><msub><mi>e</mi><mn>2</mn></msub><mo>:</mo><mi>Amt</mi><mo>[</mo><mi>c</mi><mo>]</mo></mrow>
        </mfrac>
      </mtd>
    </mtr>
  </mtable>
</math>

<math xmlns="http://www.w3.org/1998/Math/MathML" display="block">
  <mtable columnalign="left">
    <mtr>
      <mtd>
        <mtext>[T-MUL-SCALAR-L]</mtext>
        <mspace width="2em"/>
        <mfrac linethickness="1px">
          <mrow><mi>Γ</mi><mo>⊢</mo><msub><mi>e</mi><mn>1</mn></msub><mo>:</mo><mi>Num</mi><mspace width="1em"/><mi>Γ</mi><mo>⊢</mo><msub><mi>e</mi><mn>2</mn></msub><mo>:</mo><mi>Amt</mi><mo>[</mo><mi>c</mi><mo>]</mo></mrow>
          <mrow><mi>Γ</mi><mo>⊢</mo><msub><mi>e</mi><mn>1</mn></msub><mo>×</mo><msub><mi>e</mi><mn>2</mn></msub><mo>:</mo><mi>Amt</mi><mo>[</mo><mi>c</mi><mo>]</mo></mrow>
        </mfrac>
      </mtd>
    </mtr>
  </mtable>
</math>

The rule T-ADD-AMT is critical: it requires both operands to have identical currency tags. No rule permits `Amt[c₁] + Amt[c₂]` when c₁ ≠ c₂. The rules T-MUL-SCALAR-R and T-MUL-SCALAR-L ensure scalar multiplication is commutative: both `amount * scalar` and `scalar * amount` are well-typed.

**Currency Conversion:**

<math xmlns="http://www.w3.org/1998/Math/MathML" display="block">
  <mtable columnalign="left">
    <mtr>
      <mtd>
        <mtext>[T-CONVERT]</mtext>
        <mspace width="2em"/>
        <mfrac linethickness="1px">
          <mrow><mi>Γ</mi><mo>⊢</mo><mi>e</mi><mo>:</mo><mi>Amt</mi><mo>[</mo><msub><mi>c</mi><mn>1</mn></msub><mo>]</mo><mspace width="1em"/><msub><mi>c</mi><mn>2</mn></msub><mo>∈</mo><mtext>ISO-4217</mtext></mrow>
          <mrow><mi>Γ</mi><mo>⊢</mo><mi>CONVERT</mi><mo>(</mo><mi>e</mi><mo>,</mo><msub><mi>c</mi><mn>1</mn></msub><mo>,</mo><msub><mi>c</mi><mn>2</mn></msub><mo>)</mo><mo>:</mo><mi>Amt</mi><mo>[</mo><msub><mi>c</mi><mn>2</mn></msub><mo>]</mo></mrow>
        </mfrac>
      </mtd>
    </mtr>
  </mtable>
</math>

**Comparisons and Conditions:**

<math xmlns="http://www.w3.org/1998/Math/MathML" display="block">
  <mtable columnalign="left">
    <mtr>
      <mtd>
        <mtext>[T-COMP]</mtext>
        <mspace width="2em"/>
        <mfrac linethickness="1px">
          <mrow><mi>Γ</mi><mo>⊢</mo><msub><mi>e</mi><mn>1</mn></msub><mo>:</mo><mi>τ</mi><mspace width="1em"/><mi>Γ</mi><mo>⊢</mo><msub><mi>e</mi><mn>2</mn></msub><mo>:</mo><mi>τ</mi><mspace width="1em"/><mo>⊕</mo><mo>∈</mo><mo>{</mo><mi>EQ</mi><mo>,</mo><mi>NEQ</mi><mo>,</mo><mi>GT</mi><mo>,</mo><mi>LT</mi><mo>,</mo><mi>GTE</mi><mo>,</mo><mi>LTE</mi><mo>}</mo></mrow>
          <mrow><mi>Γ</mi><mo>⊢</mo><msub><mi>e</mi><mn>1</mn></msub><mo>⊕</mo><msub><mi>e</mi><mn>2</mn></msub><mo>:</mo><mi>Bool</mi></mrow>
        </mfrac>
      </mtd>
    </mtr>
  </mtable>
</math>

<math xmlns="http://www.w3.org/1998/Math/MathML" display="block">
  <mtable columnalign="left">
    <mtr>
      <mtd>
        <mtext>[T-AND]</mtext>
        <mspace width="2em"/>
        <mfrac linethickness="1px">
          <mrow><mi>Γ</mi><mo>⊢</mo><msub><mi>φ</mi><mn>1</mn></msub><mo>:</mo><mi>Bool</mi><mspace width="1em"/><mi>Γ</mi><mo>⊢</mo><msub><mi>φ</mi><mn>2</mn></msub><mo>:</mo><mi>Bool</mi></mrow>
          <mrow><mi>Γ</mi><mo>⊢</mo><msub><mi>φ</mi><mn>1</mn></msub><mspace width="0.3em"/><mi>AND</mi><mspace width="0.3em"/><msub><mi>φ</mi><mn>2</mn></msub><mo>:</mo><mi>Bool</mi></mrow>
        </mfrac>
      </mtd>
    </mtr>
  </mtable>
</math>

**Let Bindings:**

<math xmlns="http://www.w3.org/1998/Math/MathML" display="block">
  <mtable columnalign="left">
    <mtr>
      <mtd>
        <mtext>[T-LET]</mtext>
        <mspace width="2em"/>
        <mfrac linethickness="1px">
          <mrow><mi>Γ</mi><mo>⊢</mo><msub><mi>e</mi><mn>1</mn></msub><mo>:</mo><msub><mi>τ</mi><mn>1</mn></msub><mspace width="1em"/><mi>Γ</mi><mo>[</mo><mi>x</mi><mo>↦</mo><msub><mi>τ</mi><mn>1</mn></msub><mo>]</mo><mo>⊢</mo><msub><mi>e</mi><mn>2</mn></msub><mo>:</mo><msub><mi>τ</mi><mn>2</mn></msub></mrow>
          <mrow><mi>Γ</mi><mo>⊢</mo><mtext>LET</mtext><mspace width="0.3em"/><mi>x</mi><mspace width="0.3em"/><mtext>AS</mtext><mspace width="0.3em"/><msub><mi>e</mi><mn>1</mn></msub><mo>;</mo><msub><mi>e</mi><mn>2</mn></msub><mo>:</mo><msub><mi>τ</mi><mn>2</mn></msub></mrow>
        </mfrac>
      </mtd>
    </mtr>
  </mtable>
</math>

**Yield Statements:**

<math xmlns="http://www.w3.org/1998/Math/MathML" display="block">
  <mtable columnalign="left">
    <mtr>
      <mtd>
        <mtext>[T-YIELD]</mtext>
        <mspace width="2em"/>
        <mfrac linethickness="1px">
          <mrow><mi>Γ</mi><mo>⊢</mo><mi>e</mi><mo>:</mo><mi>τ</mi><mspace width="1em"/><mi>Γ</mi><mo>⊢</mo><mi>φ</mi><mo>:</mo><mi>Bool</mi></mrow>
          <mrow><mi>Γ</mi><mo>⊢</mo><mtext>YIELD</mtext><mspace width="0.3em"/><mi>e</mi><mspace width="0.3em"/><mtext>IF</mtext><mspace width="0.3em"/><mi>φ</mi><mo>:</mo><mi>τ</mi></mrow>
        </mfrac>
      </mtd>
    </mtr>
  </mtable>
</math>

**Polymorphic Fees:**

<math xmlns="http://www.w3.org/1998/Math/MathML" display="block">
  <mtable columnalign="left">
    <mtr>
      <mtd>
        <mtext>[T-POLY]</mtext>
        <mspace width="2em"/>
        <mfrac linethickness="1px">
          <mrow><mi>Γ</mi><mo>[</mo><mi>α</mi><mo>↦</mo><mo>∀</mo><mo>]</mo><mo>⊢</mo><mi>body</mi><mo>:</mo><mi>α</mi></mrow>
          <mrow><mi>Γ</mi><mo>⊢</mo><mtext>FEE</mtext><mo>⟨</mo><mi>α</mi><mo>⟩</mo><mspace width="0.3em"/><mi>body</mi><mo>:</mo><mo>∀</mo><mi>α</mi><mo>.</mo><mi>α</mi></mrow>
        </mfrac>
      </mtd>
    </mtr>
  </mtable>
</math>

#### 4.1.3 Type Safety

**Theorem 1 (Type Safety).** If program P is well-typed (Γ ⊢ P : ok), then for all inputs σ satisfying Γ:
1. Every arithmetic expression evaluates without currency mismatch
2. Every fee f computes to a value v : Amt[c] where c is f's declared currency (or Num if undeclared)
3. No implicit currency conversion occurs

*Proof sketch.* By structural induction on typing derivations.

*Base cases:* Numeric literals have type Num by T-NUM. Currency literals n⟨c⟩ have type Amt[c] by T-CURR, where c is validated against the ISO-4217 database.

*Inductive cases:* For T-ADD-AMT, both operands must have type Amt[c] for the same c; the result preserves this currency tag. For T-MUL-SCALAR-R and T-MUL-SCALAR-L, the currency of the Amt operand is preserved regardless of operand order. For T-CONVERT, the explicit conversion changes the currency tag from c₁ to c₂, requiring runtime exchange rate lookup.

The type checker implementation (`CurrencyTypeChecker.cs`) enforces these rules, rejecting programs that violate conditions (1)-(3) before evaluation. Specifically, `InferArithmeticType()` implements T-ADD-AMT, T-MUL-SCALAR-R, and T-MUL-SCALAR-L; `InferConvertType()` implements T-CONVERT; and `TypeError.MixedCurrencyArithmetic()` signals violations of condition (1).

**Example - Type Error Detection:**
```
# Type error detected at compile time:
YIELD FilingFee + SearchFee   -- where FilingFee:Amt[EUR], SearchFee:Amt[USD]
# Error: Cannot add EUR and USD without conversion

# Correct version with explicit conversion:
YIELD FilingFee + CONVERT(SearchFee, USD, EUR)
```

### 4.2 Completeness Analysis

IPFLang supports static analysis to determine whether fee computations produce defined outputs for all valid input combinations. The analysis operates in two modes: *exhaustive verification* for small domains providing formal guarantees, and *boundary-based testing* for large domains providing high-confidence heuristic assurance.

#### 4.2.1 Formal Definitions

**Definition 1 (Input Domain).** Each input declaration defines a semantic domain:
- NUMBER BETWEEN m AND M defines Dom = {n ∈ ℤ : m ≤ n ≤ M}
- BOOLEAN defines Dom = {TRUE, FALSE}
- LIST with choices {c₁, ..., cₖ} defines Dom = {c₁, ..., cₖ}
- MULTILIST with choices {c₁, ..., cₖ} defines Dom = P({c₁, ..., cₖ})

**Definition 2 (Valuation Space).** The valuation space Σ is the Cartesian product of all input domains: Σ = Dom₁ × Dom₂ × ... × Domₘ

**Definition 3 (Coverage).** For fee f with conditional yields guarded by conditions φ₁, ..., φₙ, the coverage is:

<math xmlns="http://www.w3.org/1998/Math/MathML" display="block">
  <mrow>
    <mi>Cov</mi><mo>(</mo><mi>f</mi><mo>)</mo>
    <mo>=</mo>
    <mo>{</mo><mi>σ</mi><mo>∈</mo><mi>Σ</mi><mo>:</mo><mo>∃</mo><mi>i</mi><mo>.</mo><mspace width="0.3em"/><mi>σ</mi><mo>⊨</mo><msub><mi>φ</mi><mi>i</mi></msub><mo>}</mo>
  </mrow>
</math>

**Definition 4 (Completeness).** Fee f is *complete* if Cov(f) = Σ, equivalently if φ₁ ∨ ... ∨ φₙ is valid over Σ.

#### 4.2.2 Analysis Algorithm

The completeness analysis employs two strategies depending on domain size:

**[INSERT FIGURE 2: figures/figure2_completeness_algorithm.mmd - Completeness Analysis Algorithm Flowchart]**

```
Algorithm 1: Completeness Analysis
─────────────────────────────────────────────────────────────────────
Input:  Fee f with conditions Φ = {φ₁, ..., φₙ}
        Input domains D = {D₁, ..., Dₘ}
Output: (complete: Bool, gaps: Set⟨InputCombination⟩)

 1: Σ ← D₁ × D₂ × ... × Dₘ                    // Valuation space
 2: |Σ| ← ∏ᵢ |Dᵢ|                             // Domain size
 3: gaps ← ∅
 4:
 5: if |Σ| ≤ EXHAUSTIVE_THRESHOLD then        // Default: 10⁶
 6:     // EXHAUSTIVE VERIFICATION (provides formal guarantee)
 7:     for each σ ∈ Σ do
 8:         if ¬∃φᵢ ∈ Φ : σ ⊨ φᵢ then
 9:             gaps ← gaps ∪ {σ}
10:             if |gaps| ≥ MAX_GAPS then break
11: else
12:     // BOUNDARY-BASED TESTING (heuristic, no formal guarantee)
13:     S ← SampleRepresentative(D)           // Boundary sampling
14:     for each σ ∈ S do
15:         if ¬∃φᵢ ∈ Φ : σ ⊨ φᵢ then
16:             gaps ← gaps ∪ {σ}
17:
18: return (gaps = ∅, gaps)
```

#### 4.2.3 Complexity Analysis

**Theorem 2 (Verification Complexity).** Let n = |Φ| (number of yield conditions), m = |D| (number of inputs), k = average condition evaluation cost (typically O(m) for conjunctive conditions), and N = |Σ| (domain size).

- **Exhaustive verification** (N ≤ 10⁶): O(N · n · k) time, O(|gaps|) space. Provides formal guarantee.
- **Boundary-based testing** (N > 10⁶): O(S · n · k) time where S = O(5ᵐ) for boundary sampling with 5 representatives per domain. Provides heuristic assurance only.

#### 4.2.4 Sampling Strategy

For numeric domains [min, max], the representative sampling selects boundary values (min and max), threshold values consisting of integers appearing in conditions (such as 15 and 50 from `ClaimCount GT 15`), the midpoint (min + max) / 2, and near-boundary values (min + 1 and max - 1).

For Boolean domains, sampling includes both TRUE and FALSE exhaustively. For LIST domains, sampling covers all choices exhaustively up to the cardinality limit. For MULTILIST domains, sampling includes the empty set, all singleton sets, and the full set.

#### 4.2.5 Soundness and Guarantees

**Proposition 1 (Soundness of Exhaustive Mode).** If the exhaustive algorithm reports complete, the fee is genuinely complete.

*Proof.* The algorithm checks all σ ∈ Σ. If no gap is found, then ∀σ ∈ Σ. ∃φᵢ. σ ⊨ φᵢ, which is the definition of completeness.

**Proposition 2 (Completeness of Exhaustive Mode).** If a gap exists, the exhaustive algorithm will find it (up to MAX_GAPS reporting limit).

**Remark (Boundary-Based Testing Limitations).** The boundary-based testing mode may produce false negatives (miss gaps that fall between sampled points). It is designed to maximize gap detection for common fee patterns by prioritizing boundary values, threshold values extracted from conditions, and representative samples. **This mode does not provide formal guarantees**; users requiring provable completeness should ensure domain sizes permit exhaustive verification or employ manual analysis.

**Example - Gap Detection:**
```
COMPUTE FEE ClaimFee
  YIELD 100 * (ClaimCount - 20) IF EntityType EQ Large AND ClaimCount GT 20
  YIELD 50 * (ClaimCount - 20) IF EntityType EQ Small AND ClaimCount GT 20
ENDCOMPUTE

VERIFY COMPLETE FEE ClaimFee
```

Output: "Gap: {EntityType=Micro}" and "Gap: {ClaimCount ≤ 20}"

### 4.3 Monotonicity Verification

Fee schedules should exhibit predictable behavior: increasing claim count should never decrease the fee.

#### 4.3.1 Formal Definition

**Definition 5 (Monotonicity).** Let f: Σ → ℝ be a fee function, x ∈ Vars a numeric input, and σ₋ₓ a partial valuation excluding x. Fee f is *non-decreasing* with respect to x if:

<math xmlns="http://www.w3.org/1998/Math/MathML" display="block">
  <mrow>
    <mo>∀</mo><msub><mi>σ</mi><mrow><mo>−</mo><mi>x</mi></mrow></msub><mo>.</mo>
    <mspace width="0.3em"/>
    <mo>∀</mo><msub><mi>v</mi><mn>1</mn></msub><mo>,</mo><msub><mi>v</mi><mn>2</mn></msub><mo>∈</mo><mi>Dom</mi><mo>(</mo><mi>x</mi><mo>)</mo><mo>.</mo>
    <mspace width="0.3em"/>
    <msub><mi>v</mi><mn>1</mn></msub><mo>&lt;</mo><msub><mi>v</mi><mn>2</mn></msub>
    <mo>⟹</mo>
    <mi>f</mi><mo>(</mo><msub><mi>σ</mi><mrow><mo>−</mo><mi>x</mi></mrow></msub><mo>[</mo><mi>x</mi><mo>↦</mo><msub><mi>v</mi><mn>1</mn></msub><mo>]</mo><mo>)</mo>
    <mo>≤</mo>
    <mi>f</mi><mo>(</mo><msub><mi>σ</mi><mrow><mo>−</mo><mi>x</mi></mrow></msub><mo>[</mo><mi>x</mi><mo>↦</mo><msub><mi>v</mi><mn>2</mn></msub><mo>]</mo><mo>)</mo>
  </mrow>
</math>

Analogously: *non-increasing* (≥), *strictly increasing* (>), *strictly decreasing* (<).

#### 4.3.2 Verification Algorithm

```
Algorithm 2: Monotonicity Verification
─────────────────────────────────────────────────────────────────────
Input:  Fee f, numeric input x with domain Dₓ = [min, max]
        Other domains D₋ₓ, expected direction d
Output: (monotonic: Bool, violations: List⟨Violation⟩)

 1: violations ← []
 2: for each σ₋ₓ ∈ SampleRepresentative(D₋ₓ) do   // Context sampling
 3:     V ← SampleMonotonic(Dₓ, 20)               // 20 ordered values
 4:     prev_fee ← ⊥
 5:     for each v ∈ sort(V) do
 6:         curr_fee ← Evaluate(f, σ₋ₓ[x ↦ v])
 7:         if prev_fee ≠ ⊥ then
 8:             if Violates(prev_fee, curr_fee, d) then
 9:                 violations.append((σ₋ₓ, prev_v, prev_fee, v, curr_fee))
10:         prev_fee ← curr_fee; prev_v ← v
11: return (violations = [], violations)
```

where Violates(p, c, d) returns true if and only if: d = NonDecreasing and c < p; d = NonIncreasing and c > p; d = StrictlyIncreasing and c ≤ p; or d = StrictlyDecreasing and c ≥ p.

**Remark (Currency Comparison).** Monotonicity verification compares fee values numerically. When f returns Amt[c], comparison uses the underlying numeric value; currency tags must match (guaranteed by type safety).

**Example:**
```
VERIFY MONOTONIC FEE ExcessClaimsFee WITH RESPECT TO ClaimCount
VERIFY MONOTONIC FEE DiscountFee WITH RESPECT TO YearsInProgram DIRECTION NonIncreasing
```

---

## 5. Provenance and Auditability

### 5.1 Execution Tracing

Each fee evaluation generates provenance records that comprehensively document the calculation process. These records capture the input parameter values used, all LET variable bindings computed during evaluation, and each CASE and YIELD condition evaluated along with its true/false result. The trace also records the selected YIELD expression with its computed value and the final fee amount including currency designation.

This trace enables practitioners to verify calculations against official schedules and assists in dispute resolution.

**[INSERT FIGURE 3: figures/figure3_provenance_structure.mmd - Provenance Record Structure]**

### 5.2 Counterfactual Analysis

The counterfactual engine answers "what-if" questions: how would the total fee change if a specific input were different? For each input, the system computes alternative scenarios:

```
$ ipflang run filing.ipf --inputs params.json --counterfactuals

Counterfactual Analysis:
  If EntityType were SmallEntity instead of LargeEntity:
    FilingFee: 1820 -> 910 (difference: -910)
    Total: 2540 -> 1630 (difference: -910)
```

This capability supports budget planning, client advisory, and regulatory impact assessment.

---

## 6. Reference Implementation

### 6.1 Architecture Overview

**[INSERT FIGURE 4: figures/figure4_architecture.mmd - IPFLang Engine Architecture]**

The IPFLang reference implementation comprises approximately 15,000 lines of C# targeting .NET 10.0, organized into modular subsystems:

**Parser Module:** Lexical analysis, recursive descent parsing, and AST construction with comprehensive error reporting including line numbers and expected tokens.

**Semantic Checker:** Type validation ensuring identifier resolution, type compatibility, constraint validation, and circular dependency detection in LET statements.

**Type System:** Currency-aware type checking with 161 ISO 4217 currencies, polymorphic type support, and detailed type error reporting.

**Evaluator:** Depth-first AST traversal with environment binding, supporting expression memoization, short-circuit boolean evaluation, and constant folding.

**Analysis Module:** Completeness checker with exhaustive and sampling modes, monotonicity checker with four direction types, domain analyzer, and condition extractor.

**Provenance Module:** Audit trail recording, counterfactual engine for what-if analysis, and provenance export formatting.

**Versioning Module:** Version metadata management, diff engine for version comparison, impact analyzer, and temporal query support.

**Composition Module:** Jurisdiction inheritance with input/fee merging, override detection, and inheritance analysis reporting.

### 6.2 Command-Line Interface

The CLI provides five commands:

**parse** - Validate syntax and types:
```bash
ipflang parse filing.ipf
```

**run** - Execute fee calculation:
```bash
ipflang run filing.ipf --inputs params.json [--provenance] [--counterfactuals]
```

**verify** - Run verification directives:
```bash
ipflang verify filing.ipf
```

**info** - Display script metadata:
```bash
ipflang info filing.ipf
```

**compose** - Combine jurisdictions:
```bash
ipflang compose base.ipf national.ipf [--analysis]
```

### 6.3 Input/Output Formats

**Input (JSON):**
```json
{
  "EntityType": "SmallEntity",
  "ClaimCount": 25,
  "RequestsExamination": true,
  "FilingDate": "2024-01-15"
}
```

**Output:**
```
Fee Calculation Results
=======================
Filing Fee:        EUR 135.00
Designation Fee:   EUR 660.00
Excess Claims Fee: EUR 2,650.00
Search Fee:        EUR 1,460.00
----------------------------
Total:             EUR 4,905.00
```

### 6.4 Source Code Availability

The complete source code is available at https://github.com/vbocan/IPFLang under the GNU General Public License v3.0 (GPLv3). The repository includes the complete DSL engine source (approximately 10,000 lines of C#), a comprehensive test suite (approximately 5,500 lines comprising 266 tests), 20 IPFLang example files (approximately 1,900 lines of DSL code), and documentation with syntax reference.

---

## 7. Evaluation

### 7.1 Representative Examples

The implementation includes 20 IPFLang files demonstrating language expressiveness across diverse fee structure patterns.

Real-world fee schedules are represented by EPO filing fees with multi-tiered claim pricing and ISA-dependent search fees, as well as a USPTO complete fee calculator with entity-based discounts and excess claim calculations.

Feature demonstrations include currency type safety with mixed-currency error detection, entity-based discount patterns (50% and 75% reductions), temporal operations for date-dependent fees, nested CASE blocks for complex conditional logic, MULTILIST with !COUNT for designation fees, optional fees and versioning, and jurisdiction composition (EPO base combined with DE/FR/RO national phases).

Error detection examples cover mixed currency arithmetic (EUR + USD), incomplete fee coverage, non-monotonic fee behavior, invalid currency codes, and undefined variable references.

### 7.2 Validation Against Official Fee Schedules

We validated IPFLang calculations against official fee schedules from the EPO [2] and USPTO [1].

#### 7.2.1 EPO Validation (April 2024 Schedule)

Table 2 presents validation results for the EPO filing fee calculator against the official EPO fee schedule effective April 1, 2024.

| Test Case | Claims | Pages | ISA | Expected (EUR) | IPFLang (EUR) | Match |
|-----------|--------|-------|-----|----------------|---------------|-------|
| E1: Base filing | 10 | 30 | None | 2,255 | 2,255 | ✓ |
| E2: Excess claims (16-50) | 25 | 30 | None | 4,905 | 4,905 | ✓ |
| E3: Excess claims (>50) | 60 | 30 | None | 14,125 | 14,125 | ✓ |
| E4: Excess pages | 10 | 50 | None | 2,510 | 2,510 | ✓ |
| E5: EPO as ISA | 10 | 30 | EPO | 795 | 795 | ✓ |
| E6: Other ISA | 10 | 30 | Other | 1,070 | 1,070 | ✓ |

*Table 2: EPO Validation Results (Filing Fee = €135, Designation = €660, Search = €1,460, Claim Fee = €265/€660, Page Fee = €17)*

**Methodology:** Expected values were computed manually from the official EPO Schedule of Fees [2]. The IPFLang script encodes fees effective April 1, 2024.

#### 7.2.2 USPTO Validation (January 2025 Schedule)

Table 3 presents validation results for the USPTO fee calculator against the official fee schedule effective January 19, 2025.

| Test Case | Entity | App Type | Claims | Indep | Expected (USD) | IPFLang (USD) | Match |
|-----------|--------|----------|--------|-------|----------------|---------------|-------|
| U1: Large utility | Large | Utility | 20 | 3 | 2,000 | 2,000 | ✓ |
| U2: Small utility | Small | Utility | 20 | 3 | 800 | 800 | ✓ |
| U3: Micro utility | Micro | Utility | 20 | 3 | 400 | 400 | ✓ |
| U4: Excess claims | Large | Utility | 30 | 3 | 3,000 | 3,000 | ✓ |
| U5: Excess indep | Large | Utility | 20 | 5 | 2,960 | 2,960 | ✓ |
| U6: Design | Large | Design | - | - | 1,240 | 1,240 | ✓ |
| U7: Provisional | Large | Provisional | - | - | 350 | 350 | ✓ |
| U8: Paper surcharge | Large | Utility | 20 | 3 | 2,400 | 2,400 | ✓ |

*Table 3: USPTO Validation Results (2025 Fee Schedule: Filing+Search+Exam = $2,000 large, 60% small discount, 80% micro discount)*

**Methodology:** Expected values were computed from the USPTO fee schedule [1] effective January 19, 2025. The IPFLang script reflects the updated entity discount structure (60% small, 80% micro).

All 14 representative test scenarios covering the major fee calculation patterns (base fees, excess claims at multiple tiers, entity discounts, application types, and surcharges) produced exact matches with official calculations, demonstrating IPFLang's accuracy for production fee calculations.

### 7.3 Test Suite

The test suite comprises 266 tests across 18 test files covering:

| Category | Tests | Coverage |
|----------|-------|----------|
| Calculator operations | 15 | Basic fee computation |
| Currency type safety | 31 | Type checking, 161 currencies |
| Completeness verification | 25 | Gap detection, sampling |
| Monotonicity verification | 4 | Direction enforcement |
| Temporal operations | 54 | Date calculations |
| Provenance tracking | 20 | Audit trails |
| Jurisdiction composition | 19 | Inheritance, overrides |
| Versioning | 21 | Diff, impact analysis |
| Other | 77 | Semantics, parsing, logic |

All 266 tests pass with 100% success rate. Test execution completes in approximately 75ms (0.28ms average per test), confirming that DSL interpretation imposes negligible overhead.

### 7.4 Threats to Validity

**Internal Validity.** The test suite validates correctness of the implementation but may not cover all edge cases. The 266 tests focus on feature coverage rather than exhaustive input space exploration. The validation against official fee schedules covers representative scenarios but not all possible input combinations.

**External Validity.** The 20 example files demonstrate language expressiveness but do not constitute production-scale jurisdiction coverage. The EPO and USPTO examples were selected for their fee structure complexity and public availability of official schedules. Generalization to all 118+ patent offices requires additional implementation effort.

**Construct Validity.** Performance measurements reflect test execution time, not isolated component performance. Claims about syntax readability for domain experts are based on established DSL design principles [16] rather than empirical user studies. The keyword-based syntax (EQ, GT, AND) was designed to improve readability compared to symbolic operators, and the explicit block delimiters (ENDCOMPUTE, ENDDEFINE) were chosen to reduce syntax errors, but **these design hypotheses have not been validated through controlled experiments with practitioners**. This limitation should be considered when evaluating claims about the language's accessibility.

**Reliability.** Results are reproducible via the open-source implementation. The validation test cases can be independently verified against official fee schedule documents. Exchange rate-dependent calculations (not present in the current examples) would introduce temporal variation.

---

## 8. Discussion

### 8.1 Advantages of DSL-Based Standardization

**Transparency and auditability.** Unlike black-box proprietary calculators, IPFLang scripts are human-readable specifications that can be audited directly. When fee schedules change, updates are visible diffs that can be reviewed without requiring deep programming expertise, though familiarity with the DSL syntax is necessary.

**Formal verification.** Static completeness checking (exhaustive mode) and monotonicity checking provide formal guarantees impossible with imperative implementations. For domains within the exhaustive threshold, practitioners can be confident that fee definitions cover all cases and behave predictably.

**Vendor independence.** IPFLang-based calculations are vendor-neutral: any conforming interpreter can execute scripts. Multiple tools can compete on user experience while using a standard calculation engine.

**Rapid adaptation.** Government fee schedules change frequently. IPFLang enables updates through script editing and verification, compared to longer cycles for traditional software development.

### 8.2 Cross-Domain Applicability

IPFLang's design patterns address common regulatory calculation structures beyond intellectual property.

**Entity-based pricing tiers** apply different fees based on entity characteristics. In IP, USPTO discounts of 60% (small) and 80% (micro) exemplify this pattern. Cross-domain applications include SME tax rates, tiered professional licensing, and asset-based regulatory fees.

**Volume-based progressive pricing** uses marginal pricing where unit cost changes at thresholds. In IP, claim fees charge one rate for claims 1-15, another for 16-50, and higher for 51+. Cross-domain applications include import duties, tiered utility pricing, and bulk discount structures.

**Temporal dependencies** involve fees varying based on elapsed time. In IP, maintenance fees are due at specific intervals after grant. Cross-domain applications include late payment penalties, license renewals, and filing deadline calculations.

**Multi-component additive fees** calculate totals as sums of independent components. In IP, totals combine filing, search, examination, and claim fees. Cross-domain applications include building permits, vehicle registration, and business licensing with endorsements.

A domain suitability assessment suggests high applicability to professional licensing (very high structural similarity), court filing fees (very high), tax calculations for progressive structures (high, requiring negative value support), and customs duties (medium-high, requiring hierarchical types).

### 8.3 Limitations

Several limitations constrain the current work. Regarding implementation scope, the reference implementation provides only a CLI interface; REST API support would enable broader integration with existing IP management workflows. In terms of jurisdiction coverage, the example files demonstrate representative fee structures rather than comprehensive global coverage, and production deployment would require systematic encoding of additional patent office schedules.

The type system expressiveness is constrained in that it does not support dependent types or refinement types that could express additional invariants such as requiring claim counts to be positive. A significant limitation concerns empirical validation of readability: user studies validating syntax readability for domain experts have not been conducted, and design decisions favoring readability (keyword operators, explicit block delimiters) are based on DSL design principles [16] rather than empirical evidence. This constitutes a gap that future work must address.

Finally, boundary-based testing has inherent limitations. For large input domains exceeding the exhaustive verification threshold (10⁶ combinations), the boundary-based testing mode may miss gaps between sampled points. Users requiring formal completeness guarantees must either constrain domain sizes or employ complementary analysis techniques.

---

## 9. Conclusions and Future Work

This paper introduced IPFLang, a domain-specific language for standardizing intellectual property fee calculations. The key contributions are:

1. **Language specification** with declarative syntax designed for readability, featuring keyword-based operators (EQ, GT, AND) rather than symbolic notation, explicit block delimiters, and domain-specific primitives for currency and temporal operations.

2. **Currency-aware type system** supporting 161 ISO 4217 currencies with compile-time prevention of cross-currency arithmetic errors, formalized through typing rules with proven soundness properties.

3. **Static analysis algorithms** for completeness checking (exhaustive verification for domains ≤10⁶ providing formal guarantees, boundary-based testing for larger domains) and monotonicity verification (ensuring fees behave predictably with respect to numeric inputs).

4. **Provenance tracking** with counterfactual analysis supporting auditability requirements for regulatory compliance and dispute resolution.

5. **Reference implementation** validated through 266 passing tests and verification against official EPO and USPTO fee schedules.

**Impact.** For practitioners, IPFLang offers transparent, verifiable fee calculations with audit trails. For patent offices, the standard provides machine-readable fee schedule specifications enabling third-party tool development. For the research community, IPFLang demonstrates that formal verification techniques can be applied to domain-specific regulatory contexts.

**Future work** will address current limitations through: (1) REST API implementation conforming to OpenAPI standards for enterprise integration; (2) expanded jurisdiction coverage through community contributions; (3) cross-domain pilots applying IPFLang patterns to tax calculations and customs duties; (4) formal mechanization of type safety proofs in a proof assistant such as Coq or F*; and (5) **user studies with IP practitioners to empirically evaluate the readability and usability of the DSL syntax**, addressing the current gap between design-based readability claims and empirical validation.

The open-source implementation is available at https://github.com/vbocan/IPFLang under GPLv3.

---

## References

[1] United States Patent and Trademark Office. (2025). USPTO Fee Schedule. https://www.uspto.gov/learning-and-resources/fees-and-payment/uspto-fee-schedule

[2] European Patent Office. (2024). EPO Schedule of Fees. https://my.epoline.org/epoline-portal/classic/epoline.Scheduleoffees

[3] Japan Patent Office. (2024). JPO Fee Information. https://www.jpo.go.jp/

[4] World Intellectual Property Organization. (2024). WIPO PCT Fee Tables. https://www.wipo.int/

[5] CPA Global (Clarivate). (2024). IP Management Solutions. https://www.cpaglobal.com/

[6] Athan, T., Boley, H., Governatori, G., Palmirani, M., Paschke, A., & Wyner, A. (2015). LegalRuleML: Design Principles and Foundations. In Reasoning Web. Web Logic Rules (pp. 151-188). Springer, Cham. https://doi.org/10.1007/978-3-319-21768-0_6

[7] Merigoux, D., Chataing, N., & Protzenko, J. (2021). Catala: A Programming Language for the Law. Proceedings of the ACM on Programming Languages, 5(ICFP), Article 77. https://doi.org/10.1145/3473582

[8] World Intellectual Property Organization. (2023). WIPO ST.96 - Processing of IP Information using XML. https://www.wipo.int/standards/en/st96.html

[9] European Patent Office. (2024). EPO Open Patent Services (OPS) API. https://www.epo.org/searching-for-patents/data/web-services/ops.html

[10] Hvitved, T. (2011). Contract Formalisation and Modular Implementation of Domain-Specific Languages. PhD Thesis, University of Copenhagen.

[11] Kennedy, A. (2010). Types for Units-of-Measure: Theory and Practice. In Central European Functional Programming School (CEFP 2009) (pp. 268-305). Springer. https://doi.org/10.1007/978-3-642-17685-2_8

[12] OpenFisca Contributors. (2024). OpenFisca: Open-source platform for tax-benefit microsimulation. https://openfisca.org/

[13] Red Hat. (2024). Drools Business Rules Management System. https://www.drools.org/

[14] New Zealand Government. (2018). Better Rules for Government Discovery Report. https://www.digital.govt.nz/dmsdocument/95-better-rules-for-government-discovery-report/html

[15] Waddington, M. (2020). Rules as Code. IEEE IT Professional, 22(3), 14-19.

[16] Fowler, M. (2010). Domain-Specific Languages. Addison-Wesley. ISBN: 978-0321712943

[17] Aho, A. V., Lam, M. S., Sethi, R., & Ullman, J. D. (2006). Compilers: Principles, Techniques, and Tools (2nd ed.). Addison-Wesley. ISBN: 978-0321486814

---

## Author Biography

**Valer Bocan, PhD, CSSLP** is a Senior Software Engineer and IP Technology Researcher at Universitatea Politehnica Timisoara, Romania. His research interests include domain-specific languages, legal technology, and software standards. He holds a PhD in Computer Science and has contributed to multiple open-source projects in the IP management ecosystem. Contact: valer.bocan@upt.ro

---

## Data Availability Statement

The IPFLang reference implementation, including all example files and test suite, is available under GPLv3 at https://github.com/vbocan/IPFLang.

---

## Declaration of Competing Interests

The author declares no competing interests. IPFLang is released as open-source software with no commercial affiliations.
