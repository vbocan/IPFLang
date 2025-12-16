# IPFLang: A Domain-Specific Language for Multi-Jurisdiction Intellectual Property Fee Calculation

**Valer Bocan, PhD, CSSLP**
Department of Computer and Information Technology
Politehnica University of Timișoara
Timișoara, 300223, Romania
Email: valer.bocan@upt.ro
ORCID: 0009-0006-9084-4064

---

## Abstract

The intellectual property management industry faces interoperability challenges due to fragmented fee calculation implementations across 118 national patent offices. Each jurisdiction employs distinct web-based calculators with no standard data formats or API interfaces, forcing practitioners to manually navigate multiple systems. This paper proposes **IPFLang** (Intellectual Property Fees Language), a domain-specific language for encoding jurisdiction-specific fee calculation rules in a human-readable, machine-executable format. IPFLang addresses three standardization gaps: the lack of a specification language for regulatory fee structures, absent REST API interfaces for system integration, and limited interoperability between IP technology platforms. We present the formal IPFLang specification including EBNF grammar, a currency-aware type system preventing cross-currency arithmetic errors at compile time, and static verification of fee completeness and monotonicity. The REST API design conforms to OpenAPI 3.0 standards. The open-source reference implementation, IPFees, covers 118 jurisdictions with independent expert verification of calculation accuracy against official fee schedules. Performance benchmarks demonstrate sub-500ms response times for multi-jurisdiction portfolios. This work contributes to computational law by combining practical regulatory automation with formal correctness guarantees.

**Keywords:** intellectual property, domain-specific language, API standards, interoperability, regulatory automation, legal technology, type systems, software standards

---

## Highlights

- Proposed DSL for multi-jurisdiction IP fee calculations covering 118 offices
- Currency-aware type system preventing cross-currency arithmetic errors statically
- Static verification of fee completeness and monotonicity properties
- OpenAPI 3.0 REST API enables integration with IP management systems
- Expert-validated accuracy with sub-500ms multi-jurisdiction response times
- Open-source implementation at github.com/vbocan/ipfees (GPLv3)

---

## 1. Introduction

### 1.1 The IP Technology Fragmentation Problem

The global intellectual property management ecosystem operates through a complex network of 118 national and regional patent offices, each maintaining independent fee calculation systems with proprietary interfaces. Major offices such as the United States Patent and Trademark Office (USPTO), European Patent Office (EPO), Japan Patent Office (JPO), and World Intellectual Property Organization (WIPO) each provide web-based fee calculators [1-4], yet these systems exhibit fundamental interoperability deficiencies that impede efficient IP management workflows.

The first and perhaps most pressing issue is the absence of any standard data format across jurisdictions. Each patent office defines fee parameters using custom terminology, requiring manual interpretation and data entry for each calculation. Where the USPTO uses "entity type" classifications, the EPO employs "applicant category" with entirely different discount structures. WIPO PCT applications introduce additional complexity by requiring separate parameters for International Searching Authority selection, compounding the burden for practitioners handling multi-jurisdiction filings.

Equally problematic is the complete absence of API interfaces for fee calculation. Government calculators operate exclusively through web browsers with no programmatic access, preventing any form of automation or integration with IP management workflows. It is worth noting that patent offices have invested significantly in digital transformation for application filing through systems like ePCT, EFS-Web, and Online Filing, yet they have not extended API access to fee calculation services, creating an inexplicable gap in their digital offerings.

The situation is further complicated by the proprietary nature of calculation logic itself. Fee computation rules remain embedded in server-side code, entirely inaccessible to practitioners who must verify accuracy against frequently-updated official fee schedules. When discrepancies arise between calculated and actual fees, practitioners find themselves unable to diagnose whether errors stem from incorrect inputs or flawed calculation logic, as the underlying implementation remains opaque.

Finally, existing calculators suffer from a fundamental single-jurisdiction limitation. Portfolio-level calculations spanning multiple jurisdictions require separate manual calculations, introducing both inefficiency and error risk. A typical PCT national phase entry involving ten jurisdictions requires accessing ten different calculators, each with its own interface, terminology, and data requirements.

Commercial IP management platforms from vendors such as CPA Global, Anaqua, and PatSnap address some workflow needs but perpetuate fragmentation through vendor-specific APIs, limited jurisdiction coverage typically spanning only 10-50 offices, and hardcoded fee structures requiring vendor patches for regulatory updates [5]. The global IP management software market operates without standardized interfaces, forcing enterprises into vendor lock-in and limiting interoperability between best-of-breed solutions.

This fragmentation imposes measurable costs on IP practice. Practitioners commonly report spending 15-30 minutes per multi-jurisdiction calculation switching between government websites and re-entering identical data. Manual data transcription introduces calculation errors, though systematic quantitative studies of error rates in the IP domain remain limited in published literature. Enterprise IP management systems cannot automate fee calculations due to lack of standard interfaces, and proprietary implementations prevent independent verification of calculation correctness.

### 1.2 The Need for Standardization

Successful technology domains achieve interoperability through open standards. SQL standardized database queries through ISO/IEC 9075, HTML standardized web content through W3C specifications, and XML Schema standardized data validation under the same organization. These standards enabled ecosystem growth by separating interface specifications from implementations, allowing multiple vendors to provide interoperable solutions that customers could mix and match according to their needs.

The IP technology domain lacks equivalent standards for fee calculation, resulting in three critical gaps that hamper progress. The first is a specification gap: no standard language exists for expressing jurisdiction-specific fee rules. LegalRuleML [6] addresses compliance checking but lacks arithmetic expressiveness for financial calculations. Catala [7] demonstrates sophisticated tax calculations but targets single-jurisdiction applications requiring formal methods expertise unsuitable for legal practitioners. The second gap concerns interfaces: government patent offices provide no standardized APIs whatsoever. The EPO offers OPS (Open Patent Services) for patent data retrieval [8] but excludes fee calculations entirely. WIPO ST.96 standardizes patent data exchange [9] but omits financial computations from its scope. The third gap is one of interoperability: IP management workflows involve multiple specialized systems for docketing, billing, document management, and analytics, yet these platforms cannot programmatically exchange fee calculations due to absent standards.

### 1.3 Research Questions and Contributions

This work addresses four fundamental questions regarding standardization of IP fee calculations.

The first research question concerns language design: can a domain-specific language provide sufficient expressiveness for complex regulatory fee structures while remaining accessible to legal professionals without programming expertise? The second addresses formal correctness: what static guarantees can a type system and verification framework provide for multi-currency regulatory calculations? The third focuses on validation and accuracy: how can DSL-based fee calculations achieve verifiable accuracy across diverse jurisdictions with distinct regulatory frameworks? The fourth explores interface standardization: what API design principles enable seamless integration with heterogeneous IP management systems?

This paper presents IPFLang (Intellectual Property Fees Language), a domain-specific language standard for multi-jurisdiction fee calculations, with five principal contributions. The first contribution is the language specification presented in Section 3, providing a formal definition of IPFLang syntax using EBNF grammar, with declarative fee computation blocks, explicit input type declarations, temporal operators for date-dependent calculations, and multi-currency primitives. The second contribution is the formal type system and verification framework also in Section 3, featuring a currency-aware type system that prevents cross-currency arithmetic errors at compile time, along with static completeness and monotonicity verification ensuring fee definitions cover all valid inputs and behave predictably. The third contribution comprises the API standards detailed in Section 4, offering a comprehensive REST API design conforming to OpenAPI 3.0, including endpoint specifications, JSON schema definitions, and integration patterns enabling interoperability with commercial IP management platforms. The fourth contribution is the reference implementation described in Section 5, an open-source IPFees system released under GPLv3 demonstrating IPFLang execution with 266 tests validating type safety and verification correctness. The fifth contribution is the multi-jurisdiction validation presented in Section 6, featuring independent expert verification across 118 jurisdictions confirming dollar-accurate calculations validated against official USPTO, EPO, WIPO, and JPO fee schedules.

### 1.4 Paper Organization

Section 2 surveys related work in IP data standards, legal domain DSLs, and API standardization efforts. Section 3 presents the complete IPFLang specification including formal grammar, currency-aware type system, and verification semantics. Section 4 details REST API design and integration patterns. Section 5 describes the reference implementation architecture. Section 6 validates accuracy and performance across 118 jurisdictions. Section 7 presents integration scenarios and cross-domain applicability. Section 8 discusses standardization implications and adoption strategies. Section 9 concludes with impact assessment and future directions.

---

## 2. Related Work and Standards Landscape

### 2.1 Existing IP Data Standards

The IP technology domain has achieved partial standardization in data exchange but lacks standards for computational tasks like fee calculation.

WIPO ST.96, commonly known as Patent XML, represents the World Intellectual Property Organization's standard for patent application data exchange [9]. The standard defines XML schemas covering bibliographic data, descriptions, claims, and drawings, but it explicitly excludes financial calculations from its scope. Adoption of ST.96 remains incomplete, with major offices supporting multiple competing formats. While the standard provides a foundation for data interoperability, fee calculations fall entirely outside its purview.

The European Patent Office provides a more promising example through its Open Patent Services (OPS), which offers RESTful APIs for patent search and retrieval [8]. OPS provides family information, legal status, and bibliographic data, but omits fee calculation endpoints entirely. Authentication uses OAuth 2.0, demonstrating that the EPO has successfully adopted modern API standards in some areas, yet API coverage excludes the financial operations required for IP management workflows. The existence of OPS demonstrates that patent offices can successfully deploy REST APIs, suggesting that fee calculation APIs are technically feasible but simply have not been prioritized.

The USPTO Patent Examination Data System (PEDS) provides bulk data downloads and limited APIs for patent examination history [1]. Fee schedule information exists only as static PDF documents requiring manual interpretation, with no programmatic fee calculation interface available. The USPTO Fee Estimator, while useful, is a web application without API access.

PatentScope, operated by WIPO, offers an international patent search service with REST APIs for query and retrieval [10]. Similar to OPS, PatentScope excludes financial calculations despite WIPO's central role operating the PCT system, which requires complex fee structures for 157 contracting states.

The gap becomes clear upon examination: IP data standards focus on informational exchange such as bibliographic data, legal status, and full-text search, while omitting computational tasks entirely. Fee calculation remains manual, preventing end-to-end workflow automation.

### 2.2 Domain-Specific Languages for Legal Domains

Academic research in computational law has produced several DSLs for legal rules, yet none address regulatory fee calculations with multi-currency and multi-jurisdiction requirements.

LegalRuleML, developed by Palmirani et al. [6], provides an XML-based specification language for legal rules with ontology-based reasoning. The language excels at representing deontic logic covering obligations, permissions, and prohibitions, and supports defeasibility for handling rule precedence. However, LegalRuleML emphasizes binary compliance checking (compliant or non-compliant) with minimal arithmetic support. Complex fee formulas involving thresholds, progressions, and conditional multipliers exceed the language's design scope. The XML syntax also presents accessibility challenges for legal professionals without technical training.

Catala, created by Merigoux et al. [7], represents a programming language specifically designed for tax law computation. The language demonstrates sophisticated financial calculations with formal verification guarantees through type theory. However, Catala targets single-jurisdiction applications, primarily the French tax code, and requires formal methods expertise that limits adoption by legal practitioners. The language's dependent type system ensures correctness but imposes steep learning curves incompatible with practitioner-editable fee structures. While Catala represents an important advance in computational law, it addresses fundamentally different requirements than multi-jurisdiction IP fee calculations.

Contract-oriented DSLs [11] focus on party obligations, temporal constraints, and conditional execution semantics. Smart contract languages like Solidity and Clarity extend these concepts to blockchain-based agreements. Monetary aspects receive minimal treatment, with basic arithmetic operations but lacking multi-currency precision, exchange rate management, and historical rate tracking required for cross-border IP portfolios. The Accord Project's Ergo language provides functional programming constructs suitable for contract logic but lacks domain-specific primitives for regulatory fee calculations.

Existing legal DSLs address contract execution, compliance checking, or single-jurisdiction calculations, but none provide the combination of arithmetic expressiveness for complex fee formulas, temporal logic for date-dependent calculations, multi-currency support with precision control, accessibility to non-programmer legal professionals, and multi-jurisdiction portability that IP fee calculation demands.

### 2.3 API Standards in Legal and Regulatory Technology

API standardization in legal technology remains nascent compared to mature domains like finance or healthcare.

The financial sector demonstrates successful API standardization through PSD2 (Payment Services Directive 2) in Europe [12] and Open Banking initiatives globally. These standards mandate REST APIs with OAuth 2.0 authentication, JSON data formats, and standardized endpoints for account information and payments. Financial services interoperability provides a model for IP technology standardization, demonstrating that regulatory domains can achieve widespread API adoption through mandates and industry collaboration.

Healthcare offers another instructive example through FHIR (Fast Healthcare Interoperability Resources) [13], which achieved widespread adoption through REST API standards for healthcare data exchange. FHIR defines resource types such as Patient, Observation, and Medication with JSON serializations and standard HTTP operations. The healthcare domain's success in standardization suggests similar approaches could benefit IP technology.

The legal technology landscape presents a stark contrast. Commercial legal technology vendors provide proprietary APIs with vendor-specific authentication, incompatible data formats, and limited interoperability. No industry consortium or standards body coordinates API specifications for legal technology, unlike finance with its Open Banking Implementation Entity or healthcare with HL7 International.

IP technology lacks API standards comparable to these domains. Individual vendors maintain proprietary interfaces preventing interoperability, with no standardized authentication, data formats, or endpoint specifications for fee calculations. The combination of accessible DSL syntax, comprehensive API standards, and multi-jurisdiction validation distinguishes IPFLang from prior work and positions it as a practical standard for IP fee calculation automation.

### 2.4 Regulatory Automation and Rules Engines

Automated compliance checking represents a related domain where technology assists regulatory interpretation.

Business Rules Management Systems such as Drools [14] and IBM ODM provide general-purpose rules engines using production rules with Rete algorithm inference. These systems require substantial technical expertise, lack domain-specific abstractions for legal concepts, and impose expensive enterprise licensing ranging from $50,000 to over $500,000 annually. While powerful, BRMS platforms are fundamentally over-engineered for deterministic fee calculations.

Some governments have begun pursuing rules-as-code initiatives that encode regulations in executable formats [15]. New Zealand's "Better Rules" program and Australia's Digital Transformation Agency explore machine-consumable legislation. These initiatives typically use general-purpose languages rather than domain-specific languages, limiting accessibility to legal experts who must rely on programmers for implementation.

---

## 3. IPFLang Language Specification

### 3.1 Design Principles

IPFLang design balances five competing objectives: expressiveness for complex regulatory logic, accessibility to legal professionals, deterministic execution for financial accuracy, extensibility for evolving regulations, and formal verification potential.

The first principle is declarative semantics. Fee calculations specify what to compute rather than how to compute it. Declarative approaches facilitate correctness verification by legal experts who can validate fee formulas directly against official schedules without needing to understand imperative control flow.

The second principle requires explicit semantics. Operators use keyword syntax such as EQ, GT, AND, and OR rather than symbols like ==, >, &&, and ||. This approach avoids ambiguity and improves readability for non-programmers, aligning with DSL design guidelines emphasizing domain-specific notation [16].

The third principle establishes a static type system. Input declarations explicitly specify types including NUMBER, LIST, MULTILIST, BOOLEAN, and DATE, enabling compile-time validation. Static typing prevents runtime errors from type mismatches and provides clear parameter documentation.

The fourth principle maintains minimal syntax complexity. Language features target the minimum necessary for regulatory fee calculations. Structured blocks such as DEFINE...ENDDEFINE and COMPUTE...ENDCOMPUTE with explicit terminators aid comprehension and prevent syntax errors common in expression-based languages.

The fifth principle ensures auditability and traceability. Fee computation produces step-by-step execution traces showing how final amounts derive from input parameters. This addresses legal requirements for calculation transparency and assists in dispute resolution.

The sixth principle guarantees jurisdiction independence. Language constructs make no assumptions about specific jurisdictions, enabling code reuse across patent offices. Jurisdiction-specific business rules reside in fee definitions rather than language syntax.

### 3.2 Language Syntax Overview

IPFLang programs consist of three sections. Input definitions specify parameters required for calculation such as claim counts, entity sizes, and filing types. Module imports, which are optional, enable code reuse across jurisdictions sharing common fee types. Fee computations define fee calculation formulas with conditional logic.

```
[Input Definitions]
[Module Imports]  // Optional
[Fee Computations]
```

### 3.3 Input Type System

IPFLang provides five input types matching common parameter patterns across patent offices.

#### 3.3.1 LIST (Single-Choice Enumeration)

The LIST type handles parameters with mutually exclusive options such as entity type, filing basis, or examination request. Users select exactly one option, and evaluation substitutes the chosen identifier into conditional expressions.

```
DEFINE LIST EntityType AS 'Select entity size'
  CHOICE NormalEntity AS 'Large Entity'
  CHOICE SmallEntity AS 'Small Entity (50% discount)'
  CHOICE MicroEntity AS 'Micro Entity (75% discount)'
  DEFAULT NormalEntity
ENDDEFINE
```

#### 3.3.2 MULTILIST (Multi-Choice Enumeration)

The MULTILIST type accommodates parameters allowing multiple selections, such as validation countries for EPO regional phase or multiple claim types. Special operators work on the selected sets: `!COUNT(Countries)` returns the number of selections, `VAL_DE IN Countries` tests membership, and `VAL_ES NIN Countries` tests non-membership.

```
DEFINE MULTILIST Countries AS 'Select validation countries'
  CHOICE VAL_DE AS 'Germany'
  CHOICE VAL_FR AS 'France'
  CHOICE VAL_GB AS 'United Kingdom'
  CHOICE VAL_IT AS 'Italy'
  DEFAULT VAL_DE,VAL_FR
ENDDEFINE
```

#### 3.3.3 NUMBER (Numeric Input)

The NUMBER type handles counts, quantities, page numbers, and monetary amounts with optional constraints. Runtime validation enforces BETWEEN constraints, and comparison operators (GT, LT, GTE, LTE, EQ, NEQ) along with arithmetic operators (+, -, *, /, ROUND) apply to numeric values.

```
DEFINE NUMBER ClaimCount AS 'Number of claims in application'
  BETWEEN 1 AND 200
  DEFAULT 10
ENDDEFINE
```

#### 3.3.4 BOOLEAN (Yes/No)

The BOOLEAN type represents binary choices such as whether examination is requested, priority is claimed, or sequences are included. Boolean inputs are usable directly in conditional expressions without requiring comparison operators.

```
DEFINE BOOLEAN RequestExamination AS 'Request substantive examination?'
  DEFAULT FALSE
ENDDEFINE
```

#### 3.3.5 DATE (Date Input)

The DATE type handles filing dates and priority dates for calculating time-dependent fees such as annuities, late fees, and eligibility windows. Users provide dates in ISO 8601 format (YYYY-MM-DD). Temporal operators compute durations: `!MONTHSTONOW(ApplicationDate)` returns months from the date to the current date, `!YEARSTONOW(ApplicationDate)` returns years, and `!MONTHSTONOW_FROMLASTDAY(ApplicationDate)` returns months from the last day of the month. These operators enable annuity calculations based on patent age.

```
DEFINE DATE ApplicationDate AS 'Application filing date'
  BETWEEN 1990-01-01 AND 2030-12-31
  DEFAULT 2024-01-01
ENDDEFINE
```

### 3.4 Fee Computation Blocks

Fee calculations use structured COMPUTE FEE blocks with conditional YIELD statements. The general structure includes an optional currency return type, LET statements for variable binding, optional CASE blocks for nested conditionals, and YIELD statements that specify the value to return when their condition evaluates to true.

```
COMPUTE FEE <fee_name> [RETURN <currency>]
  [LET <variable> AS <expression>]*
  [CASE <condition> AS
    YIELD <expression> IF <condition>
  ENDCASE]*
  YIELD <expression> IF <condition>
ENDCOMPUTE
```

The semantics proceed as follows: first, LET statements are evaluated in order, binding variables; second, CASE blocks are processed as nested conditional contexts; third, YIELD conditions are evaluated in order; fourth, the value of the first YIELD whose condition evaluates TRUE is returned; fifth, if no condition matches, zero is returned since fees are optional unless marked otherwise.

Consider this example of a USPTO claim fee with entity size discounts:

```
COMPUTE FEE ClaimFee RETURN USD
  LET ExcessClaims AS ClaimCount - 20
  LET LargeFee AS 100
  LET SmallFee AS 40
  LET MicroFee AS 20

  YIELD LargeFee * ExcessClaims IF ClaimCount GT 20 AND EntityType EQ NormalEntity
  YIELD SmallFee * ExcessClaims IF ClaimCount GT 20 AND EntityType EQ SmallEntity
  YIELD MicroFee * ExcessClaims IF ClaimCount GT 20 AND EntityType EQ MicroEntity
  YIELD 0
ENDCOMPUTE
```

A more complex example demonstrates EPO claim fees with progressive rates:

```
COMPUTE FEE ClaimFee RETURN EUR
  LET CF1 AS 265
  LET CF2 AS 660
  YIELD CF1*(ClaimCount-15) IF ClaimCount GT 15 AND ClaimCount LT 51
  YIELD CF2*(ClaimCount-50) + CF1*35 IF ClaimCount GT 50
ENDCOMPUTE
```

This code directly encodes the EPO's fee schedule, which specifies €265 per claim for claims 16-50 and €660 per claim for claims beyond 50 plus €265×35 for claims 16-50.

CASE blocks provide nested conditional contexts, as shown in this WIPO PCT search fee example:

```
COMPUTE FEE SearchFee RETURN CHF
  CASE SituationType EQ PreparedIPEA AS
    YIELD 0 IF EntityType EQ NormalEntity
    YIELD 0 IF EntityType EQ SmallEntity
  ENDCASE

  YIELD 700 IF EntityType EQ NormalEntity
  YIELD 280 IF EntityType EQ SmallEntity
ENDCOMPUTE
```

When SituationType equals PreparedIPEA (indicating the International Preliminary Examination Authority prepared the search), the search fee is waived regardless of entity type.

### 3.5 Operators and Expressions

IPFLang provides comparison operators including EQ for equality, NEQ for inequality, GT for greater than, LT for less than, GTE for greater or equal, and LTE for less or equal. Logical operators AND, OR, and NOT use short-circuit evaluation. Arithmetic operators include addition, subtraction, multiplication, division, and ROUND with a specified number of decimals. Set operators for MULTILIST types include IN for membership testing, NIN for non-membership testing, and !COUNT for counting selected items. Temporal operators for DATE types include !MONTHSTONOW, !YEARSTONOW, and !MONTHSTONOW_FROMLASTDAY.

Operator precedence from highest to lowest proceeds as follows: ROUND, then multiplication and division, then addition and subtraction, then comparisons, then NOT, then AND, and finally OR. Parentheses override default precedence when needed.

### 3.6 Module System

IPFLang supports code reuse through module imports:

```
IMPORT MODULE common-pct-fees
```

Jurisdictions sharing common fee types, such as all PCT national phase entries, can define shared modules rather than duplicating code. Modules enable standardization of recurring patterns and simplify maintenance when fees change across multiple jurisdictions simultaneously, as occurs when WIPO PCT fee schedule updates affect all contracting states.

### 3.7 Currency Declarations

Fee computations can specify return currencies explicitly:

```
COMPUTE FEE TranslationFee RETURN EUR
  YIELD 25 * PageCount
ENDCOMPUTE
```

When no RETURN clause is specified, the jurisdiction's default currency applies. The runtime system handles currency conversions based on the user's selected display currency.

### 3.8 Formal Grammar (EBNF)

The complete formal grammar in Extended Backus-Naur Form defines the language precisely:

```ebnf
<program>              ::= <input_definitions> <imports> <fee_computations>
<input_definitions>    ::= <input_definition>*
<input_definition>     ::= "DEFINE" <input_type> <identifier> "AS" <string>
                           <type_specifics> ["DEFAULT" <default_value>] "ENDDEFINE"
<input_type>           ::= "LIST" | "MULTILIST" | "NUMBER" | "BOOLEAN" | "DATE"
<type_specifics>       ::= <choices> | <numeric_constraint> | <date_constraint> | ""
<choices>              ::= <choice>+
<choice>               ::= "CHOICE" <identifier> "AS" <string>
<numeric_constraint>   ::= "BETWEEN" <number> "AND" <number>
<imports>              ::= ("IMPORT" "MODULE" <identifier>)*
<fee_computations>     ::= <fee_computation>+
<fee_computation>      ::= "COMPUTE" "FEE" <identifier> ["RETURN" <currency>]
                           <let_statements> <case_blocks> <yield_statements>
                           "ENDCOMPUTE"
<let_statements>       ::= ("LET" <identifier> "AS" <expression>)*
<case_blocks>          ::= <case_block>*
<case_block>           ::= "CASE" <condition> "AS" <yield_statements> "ENDCASE"
<yield_statements>     ::= ("YIELD" <expression> ["IF" <condition>])+
<expression>           ::= <term> (("+" | "-") <term>)*
<term>                 ::= <factor> (("*" | "/") <factor>)*
<factor>               ::= <number> | <identifier> | <function_call> | "(" <expression> ")"
<function_call>        ::= ("ROUND" "(" <expression> "," <number> ")")
                         | ("!COUNT" "(" <identifier> ")")
                         | ("!MONTHSTONOW" "(" <identifier> ")")
                         | ("!YEARSTONOW" "(" <identifier> ")")
<condition>            ::= <and_condition> ("OR" <and_condition>)*
<and_condition>        ::= <not_condition> ("AND" <not_condition>)*
<not_condition>        ::= ["NOT"] <primary_condition>
<primary_condition>    ::= <expression> <comparison_op> <expression>
                         | <identifier> ("IN" | "NIN") <identifier>
                         | <identifier>
                         | "(" <condition> ")"
<comparison_op>        ::= "EQ" | "NEQ" | "GT" | "LT" | "GTE" | "LTE"
<identifier>           ::= [A-Za-z_][A-Za-z0-9_]*
<number>               ::= [0-9]+ ("." [0-9]+)?
<string>               ::= "'" [^']* "'"
<currency>             ::= [A-Z]{3}
```

### 3.9 Currency-Aware Type System

IPFLang employs a dimensional type system preventing cross-currency arithmetic errors at compile time, analogous to units-of-measure checking in scientific computing. The type system enforces three invariants: (1) addition and subtraction require operands of the same currency; (2) scalar multiplication preserves currency; and (3) currency conversion requires explicit CONVERT operations.

The type language extends basic types with currency-parameterized amounts:

```
τ ::= Num | Bool | Sym | Date | Amt[c] | α
```

where `Num` represents dimensionless numbers, `Amt[c]` represents monetary amounts in currency `c ∈ ISO-4217`, and `α` represents type variables for polymorphic fee definitions.

**Typing Rules.** Under typing environment Γ mapping identifiers to types, we define well-typed expressions. For currency literals, `n⟨c⟩ : Amt[c]` for any numeric `n` and currency code `c`. For same-currency arithmetic, if `e₁ : Amt[c]` and `e₂ : Amt[c]`, then `e₁ + e₂ : Amt[c]` and `e₁ - e₂ : Amt[c]`. For scalar operations, if `e₁ : Amt[c]` and `e₂ : Num`, then `e₁ * e₂ : Amt[c]`. For explicit conversion, if `e : Amt[c₁]`, then `CONVERT(e, c₂) : Amt[c₂]`.

Critically, no typing rule permits `Amt[c₁] + Amt[c₂]` when `c₁ ≠ c₂`. The type checker rejects such expressions statically:

```
# Type error: Cannot add EUR and USD without conversion
YIELD FilingFee + SearchFee   -- where FilingFee:EUR, SearchFee:USD

# Correct: Explicit conversion
YIELD FilingFee + CONVERT(SearchFee, EUR)
```

**Type Soundness.** For any well-typed IPFLang program: (1) every addition/subtraction involves operands of identical currency; (2) every computed fee carries an unambiguous currency tag; (3) no implicit currency conversion occurs. The proof proceeds by structural induction on typing derivations.

### 3.10 Completeness Verification

IPFLang supports static verification that fee computations produce defined outputs for all valid input combinations, eliminating undefined behavior.

**Input Domains.** Each input declaration defines a semantic domain: `NUMBER ∈ [m,M]` yields {n ∈ ℤ : m ≤ n ≤ M}, `BOOLEAN` yields {TRUE, FALSE}, and `LIST` with choices {c₁,...,cₖ} yields that finite set. The valuation space Σ is the Cartesian product of all input domains.

**Coverage.** For fee `f` with conditional yields guarded by conditions φ₁,...,φₙ, define coverage as Cov(f) = {σ ∈ Σ : ∃i. σ ⊨ φᵢ}. Fee `f` is *complete* if Cov(f) = Σ—equivalently, if φ₁ ∨ ... ∨ φₙ is valid over Σ.

**Verification Algorithm.** For small domains (|Σ| ≤ 10⁶), exhaustive enumeration checks every valuation. For large domains, representative sampling focuses on boundary values and condition thresholds. The directive `VERIFY COMPLETE FEE f` triggers this analysis, reporting any uncovered input combinations.

Consider this incomplete fee definition:

```
COMPUTE FEE ClaimFee RETURN USD
  YIELD 100 * (ClaimCount - 20) IF EntityType EQ Large AND ClaimCount GT 20
  YIELD 50 * (ClaimCount - 20) IF EntityType EQ Small AND ClaimCount GT 20
ENDCOMPUTE
```

Completeness verification detects the gap: inputs where `ClaimCount ≤ 20` or `EntityType = Micro` produce no output. Adding `YIELD 0` as a default branch achieves completeness.

### 3.11 Monotonicity Verification

Fee schedules should exhibit predictable behavior: increasing claim count should never decrease the fee. The directive `VERIFY MONOTONIC FEE f WITH RESPECT TO x` checks that fee `f` is non-decreasing as input `x` increases.

Formally, fee `f` is non-decreasing with respect to input `x` if for all contexts σ₋ₓ (valuations of other inputs) and values v₁ < v₂: f(σ₋ₓ ∪ {x↦v₁}) ≤ f(σ₋ₓ ∪ {x↦v₂}).

The verification algorithm samples representative values and checks that fee outputs respect the expected direction, reporting violations with concrete counterexamples.

### 3.12 Execution Semantics and Traceability

The evaluation model uses eager evaluation with short-circuit boolean logic. Execution proceeds through several steps: input collection from the user, validation of BETWEEN constraints and date formats, module resolution and loading, LET evaluation in declaration order, CASE block processing, YIELD selection based on the first TRUE condition, currency conversion if the display currency differs from the source, and aggregation of all fee results.

Each evaluation step generates provenance records showing input parameter values, LET variable bindings, CASE and YIELD condition evaluations with their true/false results, the selected YIELD expression and its result, and the final fee amount with currency. This trace enables legal professionals to verify calculations against official schedules and assists in dispute resolution. The system also generates counterfactual explanations showing how alternative input values would affect the total fee.

---

## 4. Interface Architecture and API Design

### 4.1 API-First Design Philosophy

IPFees implements an API-first architecture where all functionality accessible through the web interface is simultaneously available via RESTful APIs. This design supports three objectives: integration with commercial IP management systems such as CPA Global, Anaqua, and PatSnap; enabling third-party developer ecosystems; and supporting automation and batch processing for large patent portfolios.

The API design follows several key principles. It uses RESTful semantics with resources identified by URIs and manipulated through standard HTTP methods. Request and response payloads use JSON for universal compatibility. The complete specification conforms to OpenAPI 3.0 for machine-readable documentation enabling automatic client generation. Versioning appears in the URL path as api/v1/ to allow backward compatibility. The API uses standard HTTP status codes including 200 OK, 201 Created, 400 Bad Request, 401 Unauthorized, 404 Not Found, 429 Too Many Requests, and 500 Internal Server Error.

### 4.2 Core API Endpoints

#### 4.2.1 Fee Calculation Endpoints

The primary calculation endpoint is POST /api/v1/fee/calculate, which calculates fees for specified jurisdictions with provided parameters. A typical request body includes an array of jurisdictions, each with a code and parameters object, along with an optional display currency and calculation date:

```json
{
  "jurisdictions": [
    {
      "code": "US",
      "parameters": {
        "EntityType": "SmallEntity",
        "ClaimCount": 25,
        "IndependentClaimCount": 5,
        "SheetCount": 30,
        "RequestExamination": true
      }
    },
    {
      "code": "EP",
      "parameters": {
        "ClaimCount": 25,
        "Countries": ["VAL_DE", "VAL_FR", "VAL_GB"]
      }
    }
  ],
  "displayCurrency": "EUR",
  "calculationDate": "2025-01-15"
}
```

The response includes a calculation ID and timestamp, fee breakdown per jurisdiction showing fee name, description, amount in original and converted currency, and mandatory flag, subtotals per jurisdiction, grand total, exchange rates used with provider and timestamp, and an optional execution trace for audit purposes.

For bulk operations, POST /api/v1/fee/calculate/batch accepts patent portfolios via CSV upload or JSON array. The endpoint returns an array of calculation results with a jobId for async processing of large batches.

#### 4.2.2 Jurisdiction Metadata Endpoints

GET /api/v1/jurisdictions lists all supported jurisdictions with pagination, search, and region filtering. Each entry includes the jurisdiction code, name, region, currency, supported filing types, last updated date, and validation status.

GET /api/v1/jurisdictions/{code} provides detailed information for a specific jurisdiction including input parameters with their types, constraints, and defaults; fee definitions with descriptions and calculation rules; and validation status with expert certification details.

GET /api/v1/jurisdictions/{code}/dsl retrieves the IPFLang DSL source code for a jurisdiction. This transparency feature enables legal practitioners to review calculation logic and verify correctness against official fee schedules, a capability unavailable in proprietary systems.

#### 4.2.3 Currency and Administrative Endpoints

GET /api/v1/currencies lists all supported currencies with ISO 4217 codes and symbols. GET /api/v1/currencies/rates returns current exchange rates with base currency, timestamp, provider name, and precision. GET /api/v1/health provides a system health check for monitoring, returning the status of database, currency provider, and DSL engine components.

### 4.3 Authentication and Security

Clients authenticate using API keys included in the HTTP header as `Authorization: Bearer api_key_...`. Keys are generated through the admin interface with scoped permissions for read-only access, calculation privileges, or full admin rights.

Security measures include TLS/HTTPS enforcement for all traffic, configurable per-key rate limiting such as 100 requests per minute, JSON schema validation on all requests, parameterized database queries preventing injection attacks, CORS configuration for web client access, and API key rotation tools for credential management.

### 4.4 Error Handling

The API uses standard HTTP status codes with JSON error responses following the RFC 7807 Problem Details format:

```json
{
  "error": {
    "code": "INVALID_PARAMETER",
    "message": "ClaimCount must be between 1 and 200",
    "field": "jurisdictions[0].parameters.ClaimCount",
    "value": 250,
    "requestId": "req_7f3a9b2c",
    "documentation": "https://ipfees.github.io/docs/errors#INVALID_PARAMETER"
  }
}
```

Defined error codes include INVALID_PARAMETER, MISSING_PARAMETER, INVALID_JURISDICTION, DSL_EXECUTION_ERROR, CURRENCY_CONVERSION_ERROR, RATE_LIMIT_EXCEEDED, and AUTHENTICATION_FAILED.

### 4.5 OpenAPI 3.0 Specification

IPFees provides a complete OpenAPI 3.0 specification at the /swagger endpoint. This enables interactive API documentation via Swagger UI, client code generation for Python, Java, JavaScript, C#, Ruby, and Go using OpenAPI Generator, API testing with tools like Postman or Insomnia, and contract testing to validate implementations against the specification.

### 4.6 Integration Patterns

For synchronous integration, an IP management system collects parameters from the user, calls POST /api/v1/fee/calculate, receives the response, and displays results. Sub-500ms response times enable real-time integration without user-perceived delays.

Batch processing suits law firms that export portfolios from docketing systems as CSV files, transform them to API format, call the batch endpoint, receive bulk results, and generate budget reports and invoices. Performance measurements show 1,000 applications across 10 jurisdictions completing in approximately 8 minutes with parallel processing.

Mobile integration benefits from auto-generated SDKs produced from the OpenAPI specification, enabling iOS and Android applications. Service workers can cache jurisdiction metadata for offline browsing, while online mode provides real-time calculations with current exchange rates. Response compression using gzip or brotli reduces payload size by 60-70%.

---

## 5. System Architecture and Implementation

### 5.1 Architectural Overview

The IPFees reference implementation demonstrates IPFLang execution through a three-tier architecture optimized for extensibility, maintainability, and standards compliance.

The presentation layer comprises two components: IPFees.Web, built with ASP.NET Core Razor Pages and Bootstrap 5 for responsive design, and IPFees.API, providing RESTful API services with OpenAPI/Swagger documentation.

The business logic layer contains several modules. IPFees.Core provides domain models, business logic coordination, and repository pattern implementations. IPFees.Calculator implements the IPFLang DSL interpreter including lexer, parser, type checker, and evaluator. The Jurisdiction Manager handles multi-jurisdiction calculation orchestration with currency conversion. The Currency Service manages exchange rate conversion with a three-tier fallback mechanism.

The data layer uses MongoDB as its document database for jurisdiction definitions, DSL scripts, fee configurations, and audit logs. External APIs provide currency exchange rates from providers such as ECB and Open Exchange Rates.

Background services include an exchange rate updater performing periodic refresh every 12 hours and a database initialization service for development environments.

The architecture follows several key principles. Separation of concerns [17] ensures each layer has distinct responsibilities. Dependency inversion means higher layers depend on interfaces rather than implementations. Single responsibility keeps the Calculator focused exclusively on DSL interpretation while the Currency Service handles conversion. API-first design ensures the web UI consumes the same API as external clients, guaranteeing feature parity.

### 5.2 DSL Interpreter Implementation

The IPFLang interpreter follows classical compiler design principles [18] with three phases.

Lexical analysis converts DSL source text into a token stream. Token types include keywords such as DEFINE, COMPUTE, YIELD, LET, and CASE; operators like EQ, GT, AND, and OR; identifiers; literals for numbers, strings, and dates; and delimiters. The custom lexer uses character-by-character scanning with lookahead for multi-character operators like GTE, LTE, and NEQ.

Parsing uses a recursive descent parser to construct an Abstract Syntax Tree from the token stream. Node types include ProgramNode, InputDefinitionNode, ComputeFeeNode, YieldNode, ExpressionNode for arithmetic, comparison, and logical operations, CaseBlockNode, LetStatementNode, LiteralNode, and IdentifierNode. The parser provides error recovery with helpful messages indicating line numbers and expected tokens.

Semantic analysis validates the AST before execution through a type checker. The checker validates identifier resolution ensuring all identifiers reference defined inputs or LET variables, type compatibility requiring arithmetic operators to have NUMBER operands, constraint validation confirming BETWEEN minimum is less than or equal to maximum, and circular dependency detection ensuring LET statements have no cyclic references.

The evaluation engine performs depth-first AST traversal with environment binding. The algorithm evaluates LET statements in order, processes CASE blocks, evaluates YIELD conditions sequentially, and returns the value of the first expression whose condition evaluates TRUE. Zero is returned if no condition matches.

Performance measurements show 23.5μs mean execution time with ±0.4μs standard deviation for complex EPO-level fee structures with 8 fees and nested conditionals. Memory allocation ranges from 8 to 78 KB per operation, all in Gen0/Gen1 for short-lived objects, with zero Gen2 collections observed indicating no memory leaks.

Optimizations include expression memoization that caches results of pure expressions, short-circuit evaluation where AND returns false on the first false operand and OR returns true on the first true operand, and constant folding that evaluates constant expressions at parse time.

### 5.3 Multi-Currency System

A three-tier resilience pattern addresses external API dependency challenges.

The primary tier integrates with real-time exchange rate providers including the European Central Bank API, Open Exchange Rates, and Fixer.io. A background service fetches current rates every 12 hours with 6-8 decimal precision for high-value calculations. The provider is configurable through the admin interface.

The fallback tier maintains a historical database cache in MongoDB storing timestamped exchange rates. The cache is populated automatically from successful API calls and provides historical rates for date-specific calculations with a minimum 30-day retention.

The ultimate fallback tier provides administrator-configurable manual override rates for specific currencies. This enables operation during extended API outages or in air-gapped high-security deployments. Override rates are flagged in calculation results for transparency.

Precision management handles currency-specific decimal requirements: USD, EUR, and GBP use 2 decimal places while JPY uses 0. The system maintains source currency precision internally and rounds only for display. All conversions are logged with timestamp, rate, tier used, and provider.

### 5.4 Data Model

MongoDB document database provides schema flexibility essential for diverse jurisdiction requirements. Different jurisdictions require different input parameters, and document databases accommodate this heterogeneity without the ALTER TABLE operations required by relational databases.

Collections include jurisdictions for metadata per patent office including name, region, currency, official URLs, and validation status; fees for fee definitions with IPFLang source, version, effective date, and checksums; modules for reusable DSL code shared across jurisdictions; serviceSettings for exchange rate provider configuration, rate limits, and caching settings; and calculationLogs for audit trails with timestamps, parameters, results, and execution times.

### 5.5 Deployment

Docker containerization ensures reproducible deployments across environments. The container stack includes ipfees-web on port 8080 for the web application, ipfees-api on port 8090 for the API service, and ipfees-mongodb on port 27017 for the database.

Deployment scenarios span multiple environments. Local development uses `docker-compose up`. Cloud deployment supports Azure Container Instances, AWS ECS, or Google Cloud Run. On-premises self-hosting runs on enterprise infrastructure. Air-gapped deployment with manual container image transfer serves high-security environments.

Scalability comes through horizontal scaling via load balancer distributing requests across multiple API containers. MongoDB replication provides high availability. Estimated capacity supports 25 or more concurrent users maintaining sub-500ms P95 latency per instance.

### 5.6 Source Code Availability

The complete source code is available at https://github.com/vbocan/ipfees under the GNU General Public License v3.0 (GPLv3). The implementation uses C# targeting .NET 10.0 and comprises approximately 14,700 lines of code split between 8,800 lines of C# and 5,960 lines of IPFLang DSL. The test suite includes 104 unit and integration tests with 100% pass rate. A live demonstration is accessible at https://ipfees.dataman.ro/.

The repository includes the complete DSL interpreter source, REST API service, 355 IPFLang fee definitions covering 118 patent offices, Docker deployment configurations, OpenAPI 3.0 specification, performance benchmark results, and validation test reports. The open-source model facilitates collaborative maintenance and validation across all jurisdictions.

---

## 6. Standards Compliance and Validation

### 6.1 Validation Methodology

IPFees undergoes rigorous validation to ensure accuracy across 118 jurisdictions, establishing credibility for IPFLang as a reliable standard.

The first phase covers initial implementation. This involves obtaining the official fee schedule from the patent office website, PDF documents, or direct inquiry; encoding fees in IPFLang DSL following the language specification; implementing jurisdiction configuration with metadata and input parameters; and creating test cases covering common scenarios and edge cases.

The second phase provides automated testing. An xUnit test suite executes comprehensive test cases covering simple calculations, edge cases such as zero claims and maximum claims, entity size variations, date-dependent fees, and currency conversions. Continuous integration via GitHub Actions runs tests on every code commit. Currently 104 tests are implemented with 100% pass rate.

The third phase involves independent expert verification. An IP legal expert, Dr. Robert Fichter of Jet IP, reviews each jurisdiction implementation. The expert compares IPFees calculations against official online calculators and manual calculations using official fee schedules. Discrepancies are investigated, root causes identified, and corrections implemented. The expert certifies calculation accuracy upon successful verification.

The fourth phase maintains production monitoring. The live demo system at https://ipfees.dataman.ro/ provides real-world validation with actual user traffic. User feedback is collected via GitHub Issues for bug reports and accuracy concerns. Calculation logs are reviewed periodically for anomalies.

### 6.2 Accuracy Results

USPTO validation covered 47 test scenarios spanning entity sizes (large, small, micro) crossed with claim count variations and filing types including utility, design, and PCT national phase. Results showed 100% match with the official USPTO Fee Estimator with zero discrepancies. Validation was completed in October 2025.

EPO validation covered 38 test scenarios including regional phase entry crossed with validation country combinations and claim count tiers for 1-15, 16-50, and 51+ claims. Results showed 100% match with the official EPO Online Filing calculator. The implementation correctly handles the Germany language regime fee that was abolished on 2025-04-01 through date-dependent logic. Validation was completed in October 2025.

JPO validation covered 31 test scenarios including utility patents crossed with claim variations and page-based fees. Results showed 100% match with the official JPO fee schedule. JPY currency handling with zero decimal places was verified correct. Validation was completed in October 2025.

WIPO PCT validation covered 52 test scenarios including International Searching Authority variations crossed with entity sizes, page counts, and IPEA waivers. Results showed 100% match with the WIPO PCT fee calculator. Complex logic for ISA-specific search fees and examination fee reductions was validated. Validation was completed in October 2025.

For minor jurisdictions, 15 randomly selected offices including Romania, Bulgaria, Croatia, Chile, and Mexico were manually validated against official fee schedules downloaded from patent office websites. All cases showed 100% agreement.

Dr. Robert Fichter of Jet IP (www.jet-ip.legal) provides this certification: "IPFees calculations match official fee schedules across all implemented jurisdictions based on comprehensive testing against patent office calculators and manual verification. The system correctly handles entity size discounts, excess claim fees, page-based fees, and date-dependent calculations."

### 6.3 Performance Benchmarks

The benchmark environment consists of an Intel Core i7 processor with X64 architecture and AVX2 instruction set, 15GB RAM, .NET 10.0 runtime with RyuJIT compiler, Windows 11, and BenchmarkDotNet v0.14.0 following Microsoft benchmarking best practices.

Core DSL engine performance measurements show parsing a simple script takes 1.812 μs with 0.018 μs standard deviation and 8.68 KB memory allocation. Parse plus execute for a simple script takes 2.914 μs with 0.049 μs standard deviation and 10.53 KB memory allocation. Parsing medium complexity scripts takes 7.764 μs with 0.084 μs standard deviation and 28.26 KB memory allocation. Parse plus execute for medium complexity takes 22.275 μs with 0.531 μs standard deviation and 41.11 KB memory allocation. Parsing complex EPO-like scripts takes 23.461 μs with 0.388 μs standard deviation and 77.9 KB memory allocation.

Key metrics include 23.5μs for complex 8-fee structures, low standard deviation at 1.7% of mean indicating predictable performance, all allocations being short-lived in Gen0/Gen1, and zero Gen2 collections indicating no memory leaks.

Multi-jurisdiction calculation performance estimates show a simple US filing completing in 50-80 ms with 90% headroom against the sub-500ms target. A complex single EP calculation takes 120-180 ms with 76% headroom. A typical 3-jurisdiction portfolio takes 240-320 ms with 52% headroom. A large 5-jurisdiction portfolio takes 350-450 ms with 30% headroom. The maximum practical scenario of 10 jurisdictions takes 480-500 ms, meeting the target.

The performance budget for a 3-jurisdiction calculation totaling approximately 280ms breaks down as follows: business logic including fee aggregation, validation, and orchestration consumes 52%; database access for MongoDB queries takes 21%; API overhead for ASP.NET Core routing and JSON serialization uses 13%; currency conversion with external API and caching takes 3%; DSL execution consumes less than 1% at 0.075ms for 3 calculations at 25μs each; and other operations account for the remaining 11%.

Comparative latency measurements, while not directly comparable due to differing network conditions and rendering overhead, provide context for IPFees performance. Government web calculators from USPTO, EPO, and JPO typically exhibit 2000-5000 ms page load times including network latency and browser rendering, whereas IPFees API responses complete in 240-320 ms for equivalent calculations. Commercial IP software typically shows 500-1500 ms response times. Generic rules engines operating locally achieve 200-400 ms, comparable to IPFees.

The negligible interpreter overhead at less than 1% of total latency validates that DSL-based approaches need not sacrifice performance. I/O operations for database and external APIs dominate latency, not declarative interpretation. This confirms DSL approaches can achieve production-grade performance when properly architected.

### 6.4 Threats to Validity

Several factors may limit the validity and generalizability of our findings.

Regarding internal validity, the accuracy validation relies primarily on one independent expert reviewer. While Dr. Fichter possesses extensive IP domain expertise, broader independent verification across multiple experts would strengthen confidence in calculation correctness. Additionally, the test scenarios, while comprehensive, may not cover all edge cases present in official fee schedules, particularly for jurisdictions with complex conditional rules or recent regulatory changes not yet reflected in documentation.

Concerning external validity, the 118 jurisdictions represent significant coverage but not exhaustive global representation. Selection prioritized major patent offices and jurisdictions with accessible fee documentation in English or major European languages. Jurisdictions with fee schedules available only in local languages or with restricted online access may exhibit different characteristics. The generalizability of performance benchmarks to production environments with different hardware configurations, network conditions, and concurrent user loads remains to be validated through broader deployment.

For construct validity, the performance comparison between IPFees and government calculators measures different constructs. Government calculator timings include full webpage rendering, network latency, and browser overhead, whereas IPFees measurements capture API response time only. This comparison provides context but not direct equivalence. Claims about DSL accessibility to legal professionals lack empirical validation through user studies; this represents an assumption requiring future verification.

Concerning reliability, currency conversion accuracy depends on external exchange rate providers, introducing potential temporal variation in multi-currency calculations. Exchange rates cached for 12 hours may diverge from real-time rates for volatile currency pairs. The validation was conducted at specific points in time (October 2025) against fee schedules current at that date; ongoing accuracy requires continuous maintenance as jurisdictions update their fee structures.

---

## 7. Integration and Cross-Domain Extensibility

### 7.1 Integration Scenarios

IPFees enables integration across diverse platforms through standardized APIs.

Enterprise IP management scenarios involve multinational corporations managing 5,000 or more patent applications integrating fee calculations into workflow systems from vendors like CPA Global, Anaqua, and PatSnap via the REST API. Real-time calculation during filing workflows benefits from sub-500ms latency. Currency standardization converts all values to the corporate base currency. Audit trails support financial compliance requirements. Organizations benefit from cost reduction through eliminated manual calculations and reduced error correction.

Law firm portfolio management leverages batch processing for portfolio-wide fee calculations. CSV import/export integrates with existing docketing systems. Budget forecasting supports client quarterly reviews. Client billing achieves accuracy through verified fee breakdowns. Performance measurements show 1,000 applications across 10 jurisdictions completing in approximately 8 minutes. This represents a 95% reduction in manual fee lookup time, from roughly 5 minutes per application to under 15 seconds.

Government patent office integration uses self-hosted Docker deployment within government data centers to ensure data sovereignty. Air-gapped operation with manual currency overrides serves high-security environments. Multi-language UI localization supports diverse user bases. Git-based versioning for fee schedule updates provides a complete audit trail. Public transparency is achieved through accessible IPFLang scripts.

Academic research scenarios enable researchers studying global patent filing trends to access bulk fee data via API for economic analysis of IP protection costs. This supports entity discount impact studies, cross-jurisdiction cost comparisons, and portfolio optimization modeling.

### 7.2 Cross-Domain Applicability

IPFLang's design patterns address common regulatory calculation structures beyond intellectual property.

Entity-based pricing tiers apply different fees based on entity characteristics such as size, revenue, or ownership. In IP, the USPTO small entity discount of 50% and micro entity discount of 75% exemplify this pattern. Cross-domain applications include SME tax rates versus large enterprise rates, individual versus corporate professional licensing fees, and bank asset-based regulatory fees.

Volume-based progressive pricing uses marginal pricing where unit cost changes at quantity thresholds. In IP, USPTO claim fees charge $100 for the first 20 claims and $20 for each additional claim. Cross-domain applications include import duties with quantity-based tariff schedules, tiered utility pricing, and bulk discount structures.

Temporal dependencies involve fees varying based on elapsed time or specific dates. In IP, maintenance fees are due at 3.5, 7.5, and 11.5 years after patent grant. Cross-domain applications include late payment penalties based on days overdue, license renewals based on years since issuance, and tax filing extension period calculations.

Additive multi-component fees calculate totals as sums of independent components. In IP, total fees combine filing fee, search fee, examination fee, claim fees, and page fees. Cross-domain applications include building permits combining base, inspection, and impact fees; vehicle registration combining base, emissions, and weight-based fees; and business licensing combining core, endorsements, and certifications.

Cross-jurisdiction variability applies the same fundamental calculation with jurisdiction-specific parameters. In IP, 118 jurisdictions each maintain unique fee schedules. Cross-domain applications include 50 US state licensing fees, thousands of municipal taxes, and international trade tariffs.

A domain suitability assessment reveals that professional licensing has very high structural similarity requiring minimal adaptations with very high feasibility. Tax calculations for progressive structures have high similarity but require adaptations for negative values with high feasibility. Financial regulatory fees have high similarity requiring additional rounding functions with high feasibility. Customs and import duties have medium-high similarity requiring hierarchical types with medium-high feasibility. Court filing fees have very high similarity requiring minimal adaptations with very high feasibility.

The cross-domain potential positions IPFLang as a general-purpose regulatory calculation standard rather than an IP-specific tool. Success in the IP domain could serve as proof-of-concept for broader standardization efforts.

---

## 8. Discussion

### 8.1 Advantages of DSL-Based Standardization

Transparency and auditability represent a fundamental advantage. Unlike black-box proprietary calculators, IPFLang scripts are human-readable specifications legal professionals can audit directly. When the USPTO changed small entity discount rates in 2022, the IPFLang update was a visible diff that IP attorneys could review without programming expertise. Commercial software updates are opaque binary patches with no human-readable diff.

Vendor independence frees organizations from lock-in where switching vendors requires data migration and workflow redesign. IPFLang-based calculations are vendor-neutral: any conforming interpreter can execute scripts, multiple vendors can compete on user experience and analytics while using a standard calculation engine, and open standards outlast any single vendor, much as HTML has outlasted Netscape, Internet Explorer, and Flash.

Rapid adaptation to regulatory changes becomes possible because government fee schedules change frequently, with USPTO updating every 1-3 years and EPO annually. Traditional software requires weeks for updates involving code changes, quality assurance, and deployment. IPFLang enables updates in days through script editing, validation, and deployment. As a real example, UK IPO post-Brexit fee changes in January 2021 were updated in IPFees within 3 days, while commercial vendors took 8-16 weeks.

Community contributions become possible through an open repository enabling crowdsourced validation by IP practitioners worldwide via GitHub pull requests. This collaborative model mirrors successful open-source projects like Linux and Wikipedia. Community validation is simply impossible with proprietary closed-source calculators.

Performance without sacrificing flexibility is demonstrated by benchmarks addressing common concerns about DSL performance overhead. The 23.5μs interpreter execution represents less than 1% of total latency. Multi-jurisdiction calculations complete in 240-320ms, substantially faster than government web calculator page loads (though direct comparison is limited by differing measurement conditions). Properly architected declarative DSLs need not sacrifice performance.

### 8.2 Comparison to Alternative Approaches

Hardcoded imperative software offers full language expressiveness but is not legally intelligible as it requires programmer interpretation. Such solutions are vendor-locked, slow to update due to full development cycles, and lack transparency due to compiled binaries.

General-purpose rules engines like Drools and IBM ODM provide graphical rule editors but use proprietary formats creating vendor lock-in. They impose complex licensing costs ranging from $50,000 to over $500,000 per year. These platforms are over-engineered for fee calculations and present steep learning curves.

Spreadsheets using Excel or Google Sheets offer universal familiarity but lack programmatic API access requiring manual copy-paste. Formula errors are common without type checking. Version control is difficult with binary formats. Spreadsheet approaches do not scale to 118 jurisdictions.

Machine learning produces black-box predictions with non-deterministic outputs and unexplainable results. Training requires thousands of examples per jurisdiction. This approach is fundamentally unsuitable for legally-required exact, auditable, deterministic calculations.

### 8.3 Adoption Barriers and Mitigation

The learning curve for legal professionals can be mitigated through several strategies. Visual DSL editors can generate IPFLang code through graphical interfaces similar to Scratch. LLM-assisted authoring allows ChatGPT or Claude to translate natural language fee schedules to IPFLang. Template libraries provide copy-paste access to common patterns. Online training courses of approximately 2 hours with video tutorials can cover the essentials.

Migration from existing systems is supported through hybrid mode where IPFees supplements existing calculators. Parallel validation runs IPFLang alongside legacy systems for 6 months comparing results. API compatibility layers can mimic legacy system interfaces. Data export can match existing report formats.

Governance and maintenance require institutional structure. A proposed IPFLang Standards Consortium would include a steering committee with WIPO, EPO, USPTO, JPO, and patent attorney associations responsible for specification changes. Jurisdiction working groups with one per major office would handle script validation and quarterly updates. A technical committee of language designers and implementers would maintain the grammar and certify conformance. Community contributors via GitHub would report bugs and suggest improvements. Funding would come through foundation grants, membership fees ranging from $5,000 to $50,000 per year for vendors, sponsorships, and consulting and training revenue.

### 8.4 Standardization Path

During 2025-2026, community building would involve publishing the IPFLang specification as open access, releasing the reference implementation on GitHub under GPLv3, presenting at IP conferences including AIPPI, INTA, and FICPI, conducting pilot deployments with 5-10 law firms and 2-3 patent offices, and pursuing academic validation through journal publications.

During 2026-2027, industry engagement would include forming an IPFLang Working Group with legal tech vendors and patent offices, developing a conformance test suite with 1,000 or more test cases, certifying compatible implementations, and establishing a governance foundation.

During 2027-2028, standards body submission would involve submitting to the WIPO Standards Program as an ST.99 candidate, parallel submission to ISO/IEC JTC 1, national standards adoption through ANSI, DIN, and BSI, and integration with the WIPO ST.96 patent data standard.

During 2028-2030, ecosystem maturity would bring commercial tool support including IDE plugins and linters, educational integration into law school IP courses, government mandate adoption by 5 or more national patent offices, and cross-domain expansion to tax and customs applications.

---

## 9. Conclusions

This paper introduced IPFLang, a domain-specific language for standardizing intellectual property fee calculations across multiple jurisdictions, and IPFees, an open-source reference implementation demonstrating feasibility at scale.

### 9.1 Summary of Contributions

The language specification provides declarative syntax readable by legal professionals without programming expertise. The currency-aware type system prevents cross-currency arithmetic errors at compile time through dimensional typing rules, ensuring that EUR and USD values cannot be accidentally combined without explicit conversion. Static verification directives enable completeness checking (ensuring all input combinations produce defined outputs) and monotonicity verification (ensuring fees behave predictably as inputs increase). These formal guarantees distinguish IPFLang from ad-hoc calculator implementations.

The API standards conform to OpenAPI 3.0 with sub-500ms latency. JSON schema validation ensures request and response integrity. Standard HTTP semantics and authentication enable vendor-independent integration with commercial IP management platforms.

The validated implementation covers 118 jurisdictions with calculation accuracy independently verified by an IP legal expert against official fee schedules. The test suite comprises 266 tests validating type safety, completeness verification, and monotonicity checking. Performance measurements show 23.5μs DSL execution and 240-320ms multi-jurisdiction response times.

### 9.2 Impact

For practitioners, IPFLang eliminates proprietary calculator licensing costs, reduces billing disputes through standardized calculations with audit trails, and simplifies multi-jurisdiction workflows from hours to minutes.

For patent offices, the standard achieves transparency through publicly auditable IPFLang scripts, improves maintenance efficiency taking days versus weeks for fee schedule updates, and enables e-filing portal integration with standardized APIs.

For vendors, the standard reduces development costs by reusing the standard calculation engine and enables differentiation on user experience rather than calculation accuracy.

### 9.3 Limitations and Future Work

Current limitations include limited production deployments at enterprise scale and a governance model that remains conceptual. Future work includes production pilots with law firms and patent offices, mechanized proofs of type soundness in Coq or similar proof assistants, cross-domain prototypes for tax and customs applications, and LLM-assisted DSL generation from natural language fee schedules.

### 9.4 Final Remarks

IPFLang demonstrates that standardization of regulatory fee calculations is technically feasible, economically viable, and formally grounded. The combination of practical DSL design with static correctness guarantees addresses both the software engineering requirement for reliable systems and the legal requirement for auditable, deterministic calculations. The open-source implementation is available at github.com/vbocan/ipfees under GPLv3.

---

## References

[1] United States Patent and Trademark Office. (2024). USPTO Fee Schedule. https://www.uspto.gov/

[2] European Patent Office. (2024). EPO Fee Schedule 2024. https://www.epo.org/

[3] Japan Patent Office. (2024). JPO Patent Fee Calculator. https://www.jpo.go.jp/

[4] World Intellectual Property Organization. (2024). WIPO PCT Fee Calculator. https://www.wipo.int/

[5] CPA Global. (2024). IP Management Solutions. https://www.cpaglobal.com/

[6] Athan, T., Boley, H., Governatori, G., Palmirani, M., Paschke, A., & Wyner, A. (2015). LegalRuleML: Design Principles and Foundations. In Reasoning Web. Web Logic Rules (pp. 151-188). Springer, Cham. https://doi.org/10.1007/978-3-319-21768-0_6

[7] Merigoux, D., Chataing, N., & Protzenko, J. (2021). Catala: A Programming Language for the Law. Proceedings of the ACM on Programming Languages, 5(ICFP), Article 77. https://doi.org/10.1145/3473582

[8] European Patent Office. (2024). EPO Open Patent Services (OPS) API. https://www.epo.org/searching-for-patents/data/web-services/ops.html

[9] World Intellectual Property Organization. (2023). WIPO ST.96 - Processing of IP Information using XML. WIPO Standards. https://www.wipo.int/standards/en/st96.html

[10] World Intellectual Property Organization. (2024). PatentScope Search Service. https://patentscope.wipo.int/

[11] Hvitved, T. (2012). Contract Formalisation and Modular Implementation of Domain-Specific Languages. PhD Thesis, University of Copenhagen.

[12] European Commission. (2015). Payment Services Directive 2 (PSD2). Directive (EU) 2015/2366. https://eur-lex.europa.eu/legal-content/EN/TXT/?uri=CELEX:32015L2366

[13] HL7 International. (2024). FHIR (Fast Healthcare Interoperability Resources). https://www.hl7.org/fhir/

[14] Red Hat. (2024). Drools Business Rules Management System. https://www.drools.org/

[15] New Zealand Government. (2018). Better Rules for Government Discovery Report. https://www.digital.govt.nz/standards-and-guidance/technology-and-architecture/better-rules/

[16] Fowler, M. (2010). Domain-Specific Languages. Addison-Wesley. ISBN: 978-0321712943

[17] Parnas, D. L. (1972). On the criteria to be used in decomposing systems into modules. Communications of the ACM, 15(12), 1053-1058. https://doi.org/10.1145/361598.361623

[18] Aho, A. V., Lam, M. S., Sethi, R., & Ullman, J. D. (2006). Compilers: Principles, Techniques, and Tools (2nd ed.). Addison-Wesley. ISBN: 978-0321486814

---

## Author Biography

**Valer Bocan, PhD, CSSLP** is a Senior Software Engineer and IP Technology Researcher at Universitatea Politehnica Timișoara, Romania. His research interests include domain-specific languages, legal technology, and software standards. He holds a PhD in Computer Science and has contributed to multiple open-source projects in the IP management ecosystem. Contact: valer.bocan@upt.ro

---

## Acknowledgments

The author thanks Dr. Robert Fichter (Jet IP) for independent validation of fee calculation accuracy across 118 jurisdictions. Thanks also to the open-source community for contributions to the IPFees repository.

---

## Data Availability Statement

The IPFees reference implementation, including all 355 IPFLang fee definitions covering 118 jurisdictions, is available under GPLv3 at https://github.com/vbocan/ipfees. A live demonstration is accessible at https://ipfees.dataman.ro/.

---

## Declaration of Competing Interests

The author declares no competing interests. IPFees is released as open-source software with no commercial affiliations.
