# Mathematical Foundations of IPFLang

This section presents the formal mathematical foundations underlying IPFLang's six core innovations. We adopt a semi-formal style balancing rigor with accessibility, using standard notation from programming language theory and mathematical logic where appropriate.

---

## 1. Preliminaries and Notation

### 1.1 Basic Definitions

Let $\mathcal{C}$ denote the set of ISO 4217 currency codes (|$\mathcal{C}$| = 161). Let $\mathbb{D}$ denote the set of calendar dates representable in ISO 8601 format. Let $\mathbb{Q}^+$ denote positive rational numbers used for monetary amounts.

**Definition 1.1 (IPFLang Script).** An IPFLang script $\mathcal{S}$ is a tuple:
$$\mathcal{S} = (\mathcal{I}, \mathcal{F}, \mathcal{G}, \mathcal{V}, v)$$

where:
- $\mathcal{I}$ is a finite set of input declarations
- $\mathcal{F}$ is a finite set of fee computations
- $\mathcal{G}$ is a finite set of group declarations
- $\mathcal{V}$ is a finite set of verification directives
- $v \in \mathcal{Version}$ is an optional version descriptor

**Definition 1.2 (Input Domain).** For each input $i \in \mathcal{I}$, the domain $\text{Dom}(i)$ is defined by its type:

| Type | Domain |
|------|--------|
| `NUMBER` with bounds $[m, M]$ | $\{n \in \mathbb{Z} : m \leq n \leq M\}$ |
| `BOOLEAN` | $\{\texttt{TRUE}, \texttt{FALSE}\}$ |
| `LIST` with choices $\{c_1, \ldots, c_k\}$ | $\{c_1, \ldots, c_k\}$ |
| `MULTILIST` with choices $\{c_1, \ldots, c_k\}$ | $\mathcal{P}(\{c_1, \ldots, c_k\})$ |
| `DATE` with bounds $[d_1, d_2]$ | $\{d \in \mathbb{D} : d_1 \leq d \leq d_2\}$ |
| `AMOUNT` with currency $c \in \mathcal{C}$ | $\mathbb{Q}^+ \times \{c\}$ |

**Definition 1.3 (Input Valuation).** An input valuation $\sigma$ is a function mapping each input name to a value in its domain:
$$\sigma : \text{names}(\mathcal{I}) \rightarrow \bigcup_{i \in \mathcal{I}} \text{Dom}(i)$$
such that $\sigma(i) \in \text{Dom}(i)$ for all $i \in \mathcal{I}$.

The set of all valid valuations for script $\mathcal{S}$ is denoted $\Sigma_\mathcal{S}$.

---

## 2. Currency-Aware Type System

### 2.1 Type Syntax

IPFLang employs a dimensional type system to prevent cross-currency arithmetic errors at compile time.

**Definition 2.1 (Types).** The set of types $\mathcal{T}$ is defined inductively:
$$\tau ::= \texttt{Num} \mid \texttt{Bool} \mid \texttt{Str} \mid \texttt{Date} \mid \texttt{StrList} \mid \texttt{Amt}[c] \mid \alpha$$

where:
- $\texttt{Num}$ is the type of dimensionless numbers
- $\texttt{Bool}$ is the type of boolean values
- $\texttt{Str}$ is the type of string/symbol values
- $\texttt{Date}$ is the type of calendar dates
- $\texttt{StrList}$ is the type of string lists (for `MULTILIST`)
- $\texttt{Amt}[c]$ is the type of monetary amounts in currency $c \in \mathcal{C}$
- $\alpha$ is a type variable (for polymorphic fees)

### 2.2 Typing Environment

**Definition 2.2 (Typing Environment).** A typing environment $\Gamma$ is a partial function from identifiers to types:
$$\Gamma : \text{Identifier} \rightharpoonup \mathcal{T}$$

We write $\Gamma, x : \tau$ for the environment extending $\Gamma$ with binding $x \mapsto \tau$.

**Definition 2.3 (Type Variable Environment).** A type variable environment $\Delta$ is a set of type variable names currently in scope:
$$\Delta \subseteq \{\alpha, \beta, \gamma, \ldots\}$$

### 2.3 Typing Rules

We present typing judgments of the form $\Delta; \Gamma \vdash e : \tau$, read as "under type variable environment $\Delta$ and typing environment $\Gamma$, expression $e$ has type $\tau$."

**Literals:**
$$\frac{n \in \mathbb{Q}}{\Delta; \Gamma \vdash n : \texttt{Num}} \quad (\text{T-Num})$$

$$\frac{n \in \mathbb{Q} \quad c \in \mathcal{C}}{\Delta; \Gamma \vdash n\langle c \rangle : \texttt{Amt}[c]} \quad (\text{T-CurrLit})$$

$$\frac{b \in \{\texttt{TRUE}, \texttt{FALSE}\}}{\Delta; \Gamma \vdash b : \texttt{Bool}} \quad (\text{T-Bool})$$

**Variables:**
$$\frac{\Gamma(x) = \tau}{\Delta; \Gamma \vdash x : \tau} \quad (\text{T-Var})$$

**Arithmetic (Same Currency):**
$$\frac{\Delta; \Gamma \vdash e_1 : \texttt{Amt}[c] \quad \Delta; \Gamma \vdash e_2 : \texttt{Amt}[c] \quad \oplus \in \{+, -\}}{\Delta; \Gamma \vdash e_1 \oplus e_2 : \texttt{Amt}[c]} \quad (\text{T-AmtAdd})$$

**Arithmetic (Scalar Multiplication):**
$$\frac{\Delta; \Gamma \vdash e_1 : \texttt{Amt}[c] \quad \Delta; \Gamma \vdash e_2 : \texttt{Num}}{\Delta; \Gamma \vdash e_1 * e_2 : \texttt{Amt}[c]} \quad (\text{T-AmtScale})$$

$$\frac{\Delta; \Gamma \vdash e_1 : \texttt{Num} \quad \Delta; \Gamma \vdash e_2 : \texttt{Amt}[c]}{\Delta; \Gamma \vdash e_1 * e_2 : \texttt{Amt}[c]} \quad (\text{T-ScaleAmt})$$

**Arithmetic (Dimensionless):**
$$\frac{\Delta; \Gamma \vdash e_1 : \texttt{Num} \quad \Delta; \Gamma \vdash e_2 : \texttt{Num} \quad \oplus \in \{+, -, *, /\}}{\Delta; \Gamma \vdash e_1 \oplus e_2 : \texttt{Num}} \quad (\text{T-NumArith})$$

**Currency Conversion:**
$$\frac{\Delta; \Gamma \vdash e : \texttt{Amt}[c_1] \quad c_2 \in \mathcal{C}}{\Delta; \Gamma \vdash \texttt{CONVERT}(e, c_1, c_2) : \texttt{Amt}[c_2]} \quad (\text{T-Convert})$$

**Polymorphic Fees:**
$$\frac{\alpha \in \Delta \quad \Delta; \Gamma \vdash e : \texttt{Amt}[\alpha]}{\Delta; \Gamma \vdash e : \texttt{Amt}[\alpha]} \quad (\text{T-Poly})$$

**Comparison (Homogeneous):**
$$\frac{\Delta; \Gamma \vdash e_1 : \tau \quad \Delta; \Gamma \vdash e_2 : \tau \quad \tau \in \{\texttt{Num}, \texttt{Str}, \texttt{Date}\}}{\Delta; \Gamma \vdash e_1 \bowtie e_2 : \texttt{Bool}} \quad (\text{T-Cmp})$$

where $\bowtie \in \{\texttt{EQ}, \texttt{NEQ}, \texttt{GT}, \texttt{GTE}, \texttt{LT}, \texttt{LTE}\}$.

### 2.4 Type Soundness

**Theorem 2.1 (Type Safety).** If $\Delta; \Gamma \vdash e : \tau$ and $e$ evaluates to value $v$, then $v$ has runtime type consistent with $\tau$. In particular:
1. If $\tau = \texttt{Amt}[c]$, then $v$ is a monetary value tagged with currency $c$.
2. No well-typed expression $e_1 + e_2$ combines values of currencies $c_1 \neq c_2$.

*Proof Sketch.* By structural induction on typing derivations. The key cases are:
- **T-AmtAdd**: Both operands must have type $\texttt{Amt}[c]$ for the same $c$, ensuring currency homogeneity.
- **T-Convert**: Explicit conversion changes the currency tag, preserving type safety.
- **T-Poly**: Type variables are instantiated consistently within a fee computation scope.

The type checker implementation (`CurrencyTypeChecker.cs`) enforces these rules statically, rejecting programs with mixed-currency arithmetic before evaluation. □

### 2.5 Compatibility Relation

**Definition 2.4 (Type Compatibility).** Types $\tau_1$ and $\tau_2$ are compatible, written $\tau_1 \sim \tau_2$, if:
1. $\tau_1 = \tau_2$, or
2. One of $\tau_1, \tau_2$ is a type variable $\alpha \in \Delta$, or
3. $\tau_1 = \texttt{Num}$ and $\tau_2 = \texttt{Amt}[c]$ (implicit zero-currency coercion)

This compatibility relation enables gradual typing where legacy scripts without currency annotations can interoperate with currency-aware code.

---

## 3. Completeness Verification

Completeness verification ensures that fee computations produce defined outputs for all valid input combinations, eliminating undefined behavior.

### 3.1 Condition Language

**Definition 3.1 (Logical Conditions).** The condition language $\mathcal{L}_C$ is defined:
$$\phi ::= \top \mid \bot \mid x \bowtie v \mid x \in S \mid x \notin S \mid \phi_1 \land \phi_2 \mid \phi_1 \lor \phi_2 \mid \neg \phi$$

where $x$ is an input variable, $v$ is a constant, $S$ is a set of constants, and $\bowtie$ is a comparison operator.

**Definition 3.2 (Condition Semantics).** For valuation $\sigma$, the satisfaction relation $\sigma \models \phi$ is defined:
- $\sigma \models \top$ always
- $\sigma \models \bot$ never
- $\sigma \models (x \bowtie v)$ iff $\sigma(x) \bowtie v$ holds
- $\sigma \models (x \in S)$ iff $\sigma(x) \in S$
- $\sigma \models (\phi_1 \land \phi_2)$ iff $\sigma \models \phi_1$ and $\sigma \models \phi_2$
- $\sigma \models (\phi_1 \lor \phi_2)$ iff $\sigma \models \phi_1$ or $\sigma \models \phi_2$
- $\sigma \models \neg\phi$ iff $\sigma \not\models \phi$

### 3.2 Fee Coverage

**Definition 3.3 (Fee Condition Set).** For fee computation $f \in \mathcal{F}$, let $\Phi_f = \{\phi_1, \ldots, \phi_n\}$ be the set of conditions guarding its YIELD statements. The coverage of $f$ is:
$$\text{Cov}(f) = \{\sigma \in \Sigma_\mathcal{S} : \exists \phi_i \in \Phi_f . \sigma \models \phi_i\}$$

**Definition 3.4 (Completeness).** Fee $f$ is *complete* with respect to input set $\mathcal{I}$ if:
$$\text{Cov}(f) = \Sigma_\mathcal{S}$$

Equivalently, $f$ is complete iff $\bigvee_{i=1}^n \phi_i$ is a tautology over the constrained input domains.

### 3.3 Completeness Checking Algorithm

**Algorithm 3.1 (Completeness Verification).**

```
function VerifyComplete(f: Fee, I: Inputs) → (bool, Set<Valuation>)
    Φ ← ExtractConditions(f)
    D ← ExtractDomains(I)
    
    if |∏_{i∈I} Dom(i)| ≤ THRESHOLD then
        // Exhaustive enumeration
        gaps ← ∅
        for σ in GenerateAll(D) do
            if ∀φ ∈ Φ : σ ⊭ φ then
                gaps ← gaps ∪ {σ}
        return (gaps = ∅, gaps)
    else
        // Representative sampling
        gaps ← ∅
        for σ in GenerateRepresentative(D) do
            if ∀φ ∈ Φ : σ ⊭ φ then
                gaps ← gaps ∪ {σ}
        return (gaps = ∅, gaps)
```

**Definition 3.5 (Representative Sampling).** For domain $D$ with bounds $[m, M]$, representative values include:
- Boundary values: $\{m, M\}$
- Near-boundary: $\{m+1, M-1\}$ (if within bounds)
- Threshold values: all constants appearing in conditions
- Midpoints between thresholds

This sampling strategy focuses on values where condition truth values change, providing high coverage with bounded enumeration.

**Theorem 3.1 (Soundness of Exhaustive Check).** For finite domains, Algorithm 3.1 with exhaustive enumeration is both sound and complete:
- If the algorithm reports "complete," then $\text{Cov}(f) = \Sigma_\mathcal{S}$.
- If the algorithm reports gaps $G$, then $G \subseteq \Sigma_\mathcal{S} \setminus \text{Cov}(f)$.

*Proof.* Exhaustive enumeration checks every valuation, so no false positives or negatives are possible. □

**Remark 3.1.** For large domains, representative sampling may produce false negatives (failing to detect some gaps), but produces no false positives. If sampling reports completeness, the fee may still have gaps; if it reports gaps, those gaps definitely exist.

---

## 4. Monotonicity Verification

Monotonicity ensures that fees behave predictably: increasing an input (e.g., claim count) should not decrease the fee.

### 4.1 Monotonicity Properties

**Definition 4.1 (Fee Function).** For fee $f$ and fixed context $\sigma_{-x}$ (valuation of all inputs except $x$), define:
$$f_{\sigma_{-x}} : \text{Dom}(x) \rightarrow \mathbb{Q}^+$$
as the function mapping input $x$ values to fee amounts, with other inputs fixed.

**Definition 4.2 (Monotonicity Directions).** Fee $f$ is:
1. *Non-decreasing* w.r.t. $x$ if: $\forall \sigma_{-x}, v_1 < v_2 \Rightarrow f_{\sigma_{-x}}(v_1) \leq f_{\sigma_{-x}}(v_2)$
2. *Non-increasing* w.r.t. $x$ if: $\forall \sigma_{-x}, v_1 < v_2 \Rightarrow f_{\sigma_{-x}}(v_1) \geq f_{\sigma_{-x}}(v_2)$
3. *Strictly increasing* w.r.t. $x$ if: $\forall \sigma_{-x}, v_1 < v_2 \Rightarrow f_{\sigma_{-x}}(v_1) < f_{\sigma_{-x}}(v_2)$
4. *Strictly decreasing* w.r.t. $x$ if: $\forall \sigma_{-x}, v_1 < v_2 \Rightarrow f_{\sigma_{-x}}(v_1) > f_{\sigma_{-x}}(v_2)$

### 4.2 Monotonicity Checking Algorithm

**Algorithm 4.1 (Monotonicity Verification).**

```
function VerifyMonotonic(f: Fee, I: Inputs, x: Input, dir: Direction) → (bool, List<Violation>)
    D_x ← ExtractDomain(x)
    D_rest ← ExtractDomains(I \ {x})
    violations ← []
    
    for σ_rest in GenerateRepresentative(D_rest) do
        values ← GetRepresentativeValues(D_x, 20)
        sort(values)
        
        prev_val ← ⊥
        prev_fee ← ⊥
        
        for v in values do
            σ ← σ_rest ∪ {x ↦ v}
            fee ← Evaluate(f, σ)
            
            if prev_val ≠ ⊥ then
                if ViolatesDirection(prev_fee, fee, dir) then
                    violations.append((σ_rest, prev_val, prev_fee, v, fee))
            
            prev_val ← v
            prev_fee ← fee
    
    return (violations = [], violations)
```

**Definition 4.3 (Violation Detection).** For direction $d$ and consecutive values $(v_1, f_1), (v_2, f_2)$ with $v_1 < v_2$:

| Direction | Violation Condition |
|-----------|---------------------|
| Non-decreasing | $f_2 < f_1$ |
| Non-increasing | $f_2 > f_1$ |
| Strictly increasing | $f_2 \leq f_1$ |
| Strictly decreasing | $f_2 \geq f_1$ |

### 4.3 Monotonicity Invariants for Fee Schedules

**Proposition 4.1 (Common Monotonicity Patterns).** The following fee patterns preserve non-decreasing monotonicity w.r.t. quantity $n$:
1. **Flat fee**: $f(n) = c$ (constant)
2. **Linear excess**: $f(n) = \max(0, n - k) \cdot r$ for threshold $k$ and rate $r > 0$
3. **Progressive tiers**: $f(n) = \sum_{i} r_i \cdot \max(0, \min(n, t_i) - t_{i-1})$ for increasing thresholds $t_i$ and non-negative rates $r_i$

*Proof.* Each formula is composed of monotonic operations (max, min, addition, multiplication by positive constants), and composition of monotonic functions preserves monotonicity. □

---

## 5. Provenance Semantics

Provenance tracking provides formal audit trails explaining how fee computations derive their results.

### 5.1 Provenance Model

**Definition 5.1 (Provenance Record).** A provenance record $\rho$ is a tuple:
$$\rho = (f, \phi_c, \phi_y, e, v, \delta, \sigma_{\text{ref}}, \lambda)$$

where:
- $f$ is the fee name
- $\phi_c$ is the CASE condition (or $\top$ if unconditional)
- $\phi_y$ is the YIELD condition (or $\top$ if unconditional)
- $e$ is the evaluated expression
- $v$ is the contribution amount
- $\delta \in \{\texttt{contributed}, \texttt{skipped}\}$ indicates whether this record contributed
- $\sigma_{\text{ref}}$ is the subset of input values referenced
- $\lambda$ is the LET variable bindings used

**Definition 5.2 (Fee Provenance).** The provenance of fee $f$ under valuation $\sigma$ is:
$$\text{Prov}(f, \sigma) = (\rho_1, \ldots, \rho_n, T)$$

where $\rho_i$ are individual yield evaluations and $T = \sum_{\rho_i.\delta = \texttt{contributed}} \rho_i.v$.

### 5.2 Provenance Collection Semantics

**Definition 5.3 (Instrumented Evaluation).** The instrumented evaluator $\llbracket \cdot \rrbracket_P$ extends standard evaluation to collect provenance:

$$\llbracket \texttt{YIELD } e \texttt{ IF } \phi \rrbracket_P(\sigma, \Gamma_L) = \begin{cases}
(\rho[\delta := \texttt{contributed}], \llbracket e \rrbracket(\sigma, \Gamma_L)) & \text{if } \sigma \models \phi \\
(\rho[\delta := \texttt{skipped}], 0) & \text{otherwise}
\end{cases}$$

where $\Gamma_L$ contains LET bindings and $\rho$ records the condition, expression, and referenced values.

### 5.3 Provenance Composition

**Definition 5.4 (Computation Provenance).** For script $\mathcal{S}$ with fees $\{f_1, \ldots, f_m\}$:
$$\text{Prov}(\mathcal{S}, \sigma) = (\text{Prov}(f_1, \sigma), \ldots, \text{Prov}(f_m, \sigma), T_M, T_O, T)$$

where:
- $T_M = \sum_{f_i \text{ mandatory}} T_i$ (total mandatory fees)
- $T_O = \sum_{f_i \text{ optional}} T_i$ (total optional fees)
- $T = T_M + T_O$ (grand total)

### 5.4 Counterfactual Generation

**Definition 5.5 (Counterfactual).** A counterfactual for input $x$ under valuation $\sigma$ is:
$$\text{CF}(x, v', \sigma) = (x, \sigma(x), v', T, T')$$

where $T = \text{Total}(\mathcal{S}, \sigma)$ and $T' = \text{Total}(\mathcal{S}, \sigma[x \mapsto v'])$.

**Algorithm 5.1 (Counterfactual Generation).**

For each input $x \in \mathcal{I}$, generate alternative values $V_x$ based on type:
- `BOOLEAN`: $V_x = \{\neg \sigma(x)\}$
- `LIST` with choices $C$: $V_x = C \setminus \{\sigma(x)\}$
- `NUMBER` with bounds $[m, M]$: $V_x = \{m, M, \lfloor(m+M)/2\rfloor\} \setminus \{\sigma(x)\}$
- `AMOUNT`: $V_x = \{2 \cdot \sigma(x), \sigma(x)/2\}$

**Proposition 5.1 (Counterfactual Consistency).** For all counterfactuals generated:
$$\text{CF}(x, v', \sigma).\text{Difference} = T' - T = \text{Total}(\mathcal{S}, \sigma[x \mapsto v']) - \text{Total}(\mathcal{S}, \sigma)$$

This enables "what-if" analysis showing how changing a single input affects the total fee.

---

## 6. Regulatory Change Semantics

Regulatory change semantics formalize how fee schedules evolve over time, enabling version comparison, impact analysis, and temporal queries.

### 6.1 Version Model

**Definition 6.1 (Version).** A version $v$ is a tuple:
$$v = (\text{id}, d_{\text{eff}}, \text{desc}, \text{ref})$$

where:
- $\text{id} \in \text{String}$ is a unique version identifier
- $d_{\text{eff}} \in \mathbb{D}$ is the effective date
- $\text{desc} \in \text{String}?$ is an optional description
- $\text{ref} \in \text{String}?$ is an optional regulatory reference

**Definition 6.2 (Versioned Script).** A versioned script $\mathcal{VS}$ is a sequence of version-script pairs:
$$\mathcal{VS} = \langle (v_1, \mathcal{S}_1), \ldots, (v_n, \mathcal{S}_n) \rangle$$

ordered by effective date: $v_1.d_{\text{eff}} < v_2.d_{\text{eff}} < \cdots < v_n.d_{\text{eff}}$.

### 6.2 Version Resolution

**Definition 6.3 (Version Resolution).** For date $d$, the applicable version is:
$$\text{Resolve}(\mathcal{VS}, d) = \begin{cases}
(v_i, \mathcal{S}_i) & \text{where } i = \max\{j : v_j.d_{\text{eff}} \leq d\} \\
\bot & \text{if no such } i \text{ exists}
\end{cases}$$

This implements the standard regulatory rule that the most recent version effective on or before date $d$ applies.

### 6.3 Change Classification

**Definition 6.4 (Change Types).** For scripts $\mathcal{S}_1, \mathcal{S}_2$, define change sets:

$$\text{Added}(\mathcal{S}_1, \mathcal{S}_2) = \text{names}(\mathcal{S}_2.F) \setminus \text{names}(\mathcal{S}_1.F)$$
$$\text{Removed}(\mathcal{S}_1, \mathcal{S}_2) = \text{names}(\mathcal{S}_1.F) \setminus \text{names}(\mathcal{S}_2.F)$$
$$\text{Modified}(\mathcal{S}_1, \mathcal{S}_2) = \{f : f \in \text{names}(\mathcal{S}_1.F) \cap \text{names}(\mathcal{S}_2.F) \land \mathcal{S}_1.F(f) \neq \mathcal{S}_2.F(f)\}$$

**Definition 6.5 (Breaking Changes).** A change is *breaking* if it may cause existing calculations to fail or produce different results:

| Change Type | Breaking Condition |
|-------------|-------------------|
| Fee removed | Fee was mandatory |
| Fee modified | Fee is mandatory |
| Input removed | Always breaking |
| Input added | New input is required |
| Input range narrowed | $[m', M'] \subsetneq [m, M]$ |
| List choice removed | Always breaking |

**Definition 6.6 (Change Report).** A change report $\mathcal{CR}$ between versions $v_1$ and $v_2$ is:
$$\mathcal{CR} = (v_1, v_2, \Delta_F, \Delta_I, \Delta_G, n_b)$$

where $\Delta_F, \Delta_I, \Delta_G$ are fee, input, and group changes respectively, and $n_b$ is the count of breaking changes.

### 6.4 Impact Analysis

**Definition 6.7 (Affected Scenarios).** The set of affected scenarios for a change is:
$$\text{Affected}(\mathcal{CR}, \Sigma) = \{\sigma \in \Sigma : \text{Total}(\mathcal{S}_1, \sigma) \neq \text{Total}(\mathcal{S}_2, \sigma)\}$$

**Algorithm 6.1 (Impact Estimation).**

```
function EstimateImpact(CR: ChangeReport, S1, S2: Script) → ImpactReport
    affected_inputs ← {}
    for change in CR.InputChanges do
        if change.type ∈ {Added, Removed, Modified} then
            affected_inputs ← affected_inputs ∪ {change.name}
    
    // Estimate affected scenario count
    domain_size ← ∏_{i ∈ S2.I} |Dom(i)|
    affected_ratio ← EstimateRatio(affected_inputs, S2.I)
    
    return ImpactReport(
        changes = CR,
        affected_scenarios = domain_size × affected_ratio,
        affected_fees = |CR.FeeChanges.Modified| + |CR.FeeChanges.Removed|
    )
```

### 6.5 Temporal Query Semantics

**Definition 6.8 (Temporal Query).** A temporal query computes fees using the version effective at a specified date:
$$\text{ComputeAt}(\mathcal{VS}, d, \sigma) = \begin{cases}
\text{Eval}(\mathcal{S}, \sigma) & \text{if } \text{Resolve}(\mathcal{VS}, d) = (v, \mathcal{S}) \\
\text{Error} & \text{otherwise}
\end{cases}$$

**Definition 6.9 (Temporal Comparison).** For dates $d_1 < d_2$:
$$\text{Compare}(\mathcal{VS}, d_1, d_2, \sigma) = (T_1, T_2, T_2 - T_1, \frac{T_2 - T_1}{T_1} \times 100\%)$$

where $T_i = \text{ComputeAt}(\mathcal{VS}, d_i, \sigma)$.

### 6.6 Preservation Verification

**Definition 6.10 (Completeness Preservation).** Versions $v_1, v_2$ preserve completeness if:
$$\text{Complete}(\mathcal{S}_1) \Rightarrow \text{Complete}(\mathcal{S}_2)$$

**Definition 6.11 (Monotonicity Preservation).** Versions preserve monotonicity for fee $f$ w.r.t. input $x$ if:
$$\text{Monotonic}(f_1, x, d) \Rightarrow \text{Monotonic}(f_2, x, d)$$

These preservation properties ensure that regulatory updates do not introduce coverage gaps or behavioral anomalies.

---

## 7. Temporal Logic for Regulatory Deadlines

IPFLang includes temporal operators for deadline calculations, late fees, and time-dependent regulatory rules.

### 7.1 Temporal Primitives

**Definition 7.1 (Date Operations).** Define temporal operators over dates:

| Operator | Signature | Semantics |
|----------|-----------|-----------|
| $\text{DaysBetween}$ | $\mathbb{D} \times \mathbb{D} \rightarrow \mathbb{Z}$ | Calendar days between dates |
| $\text{BDaysBetween}$ | $\mathbb{D} \times \mathbb{D} \rightarrow \mathbb{Z}$ | Business days (excl. weekends) |
| $\text{MonthsBetween}$ | $\mathbb{D} \times \mathbb{D} \rightarrow \mathbb{Z}$ | Complete months between dates |
| $\text{YearsBetween}$ | $\mathbb{D} \times \mathbb{D} \rightarrow \mathbb{Z}$ | Complete years between dates |
| $\text{AddDays}$ | $\mathbb{D} \times \mathbb{Z} \rightarrow \mathbb{D}$ | Add calendar days |
| $\text{AddBDays}$ | $\mathbb{D} \times \mathbb{Z} \rightarrow \mathbb{D}$ | Add business days |
| $\text{IsWeekend}$ | $\mathbb{D} \rightarrow \text{Bool}$ | Check if weekend |

### 7.2 Business Day Calculus

**Definition 7.2 (Business Days).** A day $d$ is a business day iff:
$$\text{IsBDay}(d) \equiv \text{DayOfWeek}(d) \notin \{\text{Saturday}, \text{Sunday}\}$$

**Definition 7.3 (Business Days Between).** The count of business days between $d_1$ and $d_2$:
$$\text{BDaysBetween}(d_1, d_2) = |\{d : d_1 \leq d < d_2 \land \text{IsBDay}(d)\}|$$

**Algorithm 7.1 (Add Business Days).**

```
function AddBDays(d: Date, n: Int) → Date
    direction ← sign(n)
    remaining ← |n|
    current ← d
    
    while remaining > 0 do
        current ← current + direction days
        if IsBDay(current) then
            remaining ← remaining - 1
    
    return current
```

### 7.3 Late Fee Calculus

**Definition 7.4 (Late Fee Multiplier).** For deadline $d_{\text{dead}}$, actual date $d_{\text{act}}$, and parameters $(b, r, m)$:
$$\text{LateMult}(d_{\text{dead}}, d_{\text{act}}, b, r, m) = \min(m, b + r \cdot \max(0, \text{DaysBetween}(d_{\text{dead}}, d_{\text{act}})))$$

where $b$ is the base multiplier, $r$ is the daily increase rate, and $m$ is the maximum multiplier.

**Definition 7.5 (Stepped Late Fee).** For tiers $\langle(t_1, f_1), \ldots, (t_n, f_n)\rangle$ where $t_i$ is the day threshold and $f_i$ is the fee:
$$\text{SteppedLate}(d_{\text{dead}}, d_{\text{act}}, \text{tiers}) = f_i \text{ where } i = \max\{j : \text{DaysLate} \geq t_j\}$$

and $\text{DaysLate} = \max(0, \text{DaysBetween}(d_{\text{dead}}, d_{\text{act}}))$.

### 7.4 Regulatory Time Windows

**Definition 7.6 (Grace Period).** A date $d$ is within grace period of deadline $d_{\text{dead}}$ with period $p$ months:
$$\text{InGrace}(d_{\text{dead}}, d, p) \equiv d_{\text{dead}} < d \leq \text{AddMonths}(d_{\text{dead}}, p)$$

**Definition 7.7 (Priority Period).** For Paris Convention priority, filing date $d_f$ has valid priority from date $d_p$ with period $p$ months:
$$\text{HasPriority}(d_p, d_f, p) \equiv d_p < d_f \leq \text{AddMonths}(d_p, p)$$

### 7.5 Renewal Calculus

**Definition 7.8 (Renewal Due).** A renewal is due if sufficient time has elapsed:
$$\text{RenewalDue}(d_{\text{file}}, d_{\text{check}}, y) \equiv \text{YearsBetween}(d_{\text{file}}, d_{\text{check}}) \geq y$$

**Definition 7.9 (Next Renewal Date).** The next renewal date after $d_{\text{check}}$:
$$\text{NextRenewal}(d_{\text{file}}, d_{\text{check}}, y) = d_{\text{file}} + \lceil\frac{\text{YearsBetween}(d_{\text{file}}, d_{\text{check}})}{y}\rceil \cdot y \text{ years}$$

---

## 8. Jurisdiction Composition Calculus

The composition calculus enables code reuse across jurisdictions through inheritance and override semantics.

### 8.1 Jurisdiction Model

**Definition 8.1 (Jurisdiction).** A jurisdiction $J$ is a tuple:
$$J = (\text{id}, \text{name}, \mathcal{S}, p, M)$$

where:
- $\text{id}$ is a unique identifier
- $\text{name}$ is the display name
- $\mathcal{S}$ is the associated script
- $p \in \text{JurisdictionId}?$ is an optional parent jurisdiction
- $M$ is metadata (region, currency, etc.)

**Definition 8.2 (Jurisdiction Registry).** A registry $\mathcal{R}$ is a set of jurisdictions with well-formed parent references:
$$\mathcal{R} = \{J_1, \ldots, J_n\} \text{ where } \forall J \in \mathcal{R}, J.p \neq \bot \Rightarrow \exists J' \in \mathcal{R}, J'.id = J.p$$

### 8.2 Inheritance Chain

**Definition 8.3 (Inheritance Chain).** The inheritance chain of jurisdiction $J$ is:
$$\text{Chain}(J) = \begin{cases}
\langle J \rangle & \text{if } J.p = \bot \\
\text{Chain}(\mathcal{R}[J.p]) \cdot \langle J \rangle & \text{otherwise}
\end{cases}$$

where $\cdot$ denotes sequence concatenation and $\mathcal{R}[id]$ looks up jurisdiction by id.

**Proposition 8.1 (Chain Properties).** For well-formed registry $\mathcal{R}$:
1. Every chain is finite (no cycles)
2. The first element is always a root (parentless) jurisdiction
3. The last element is always the queried jurisdiction

### 8.3 Composition Semantics

**Definition 8.4 (Script Composition).** For inheritance chain $\langle J_1, \ldots, J_n \rangle$, the composed script is:
$$\text{Compose}(\langle J_1, \ldots, J_n \rangle) = J_1.\mathcal{S} \triangleleft J_2.\mathcal{S} \triangleleft \cdots \triangleleft J_n.\mathcal{S}$$

where the override operator $\triangleleft$ is defined:
$$(\mathcal{S}_1 \triangleleft \mathcal{S}_2).F(f) = \begin{cases}
\mathcal{S}_2.F(f) & \text{if } f \in \text{names}(\mathcal{S}_2.F) \\
\mathcal{S}_1.F(f) & \text{otherwise}
\end{cases}$$

Similarly for inputs and groups: child definitions override parent definitions with the same name.

**Proposition 8.2 (Composition Associativity).** The override operator is associative:
$$(\mathcal{S}_1 \triangleleft \mathcal{S}_2) \triangleleft \mathcal{S}_3 = \mathcal{S}_1 \triangleleft (\mathcal{S}_2 \triangleleft \mathcal{S}_3)$$

### 8.4 Inheritance Analysis

**Definition 8.5 (Inheritance Analysis).** For jurisdiction $J$ with chain $\langle J_1, \ldots, J_n \rangle$:

$$\text{Inherited}(J) = \bigcup_{i=1}^{n-1} \text{names}(J_i.\mathcal{S}.F) \setminus \text{names}(J.\mathcal{S}.F)$$
$$\text{Defined}(J) = \text{names}(J.\mathcal{S}.F) \setminus \bigcup_{i=1}^{n-1} \text{names}(J_i.\mathcal{S}.F)$$
$$\text{Overridden}(J) = \text{names}(J.\mathcal{S}.F) \cap \bigcup_{i=1}^{n-1} \text{names}(J_i.\mathcal{S}.F)$$

### 8.5 Reuse Metrics

**Definition 8.6 (Reuse Percentage).** For jurisdiction $J$:
$$\text{Reuse}(J) = \frac{|\text{Inherited}(J)|}{|\text{Inherited}(J)| + |\text{Defined}(J)| + |\text{Overridden}(J)|} \times 100\%$$

**Definition 8.7 (Registry Metrics).** For registry $\mathcal{R}$:
$$\text{InheritanceUsage}(\mathcal{R}) = \frac{|\{J \in \mathcal{R} : J.p \neq \bot\}|}{|\mathcal{R}|} \times 100\%$$

$$\text{OverallReuse}(\mathcal{R}) = \frac{\sum_{J \in \mathcal{R}} |\text{Inherited}(J)|}{\sum_{J \in \mathcal{R}} |\text{ComposedFees}(J)|} \times 100\%$$

---

## 9. Correctness Properties

### 9.1 Type Safety

**Theorem 9.1 (Currency Safety).** For any well-typed IPFLang script $\mathcal{S}$:
1. No expression evaluates to a sum of values with different currencies.
2. All monetary results are tagged with their currency.
3. Currency conversion is explicit and traceable.

### 9.2 Determinism

**Theorem 9.2 (Deterministic Evaluation).** For any script $\mathcal{S}$ and valuation $\sigma$:
$$\forall \sigma \in \Sigma_\mathcal{S}, \text{Eval}(\mathcal{S}, \sigma) \text{ is unique}$$

*Proof Sketch.* IPFLang has no side effects, random operations, or external dependencies during evaluation. Expression evaluation is purely functional, and YIELD selection is deterministic (first matching condition). □

### 9.3 Verification Soundness

**Theorem 9.3 (Verification Soundness).**
1. If `VERIFY COMPLETE FEE f` passes, then $\text{Cov}(f) = \Sigma_\mathcal{S}$ (for exhaustive check).
2. If `VERIFY MONOTONIC FEE f WITH RESPECT TO x` passes with representative sampling, then no violations were found in sampled points.

### 9.4 Provenance Correctness

**Theorem 9.4 (Provenance Correctness).** For provenance $P = \text{Prov}(\mathcal{S}, \sigma)$:
$$P.T = \sum_{f \in \mathcal{F}} \sum_{\rho \in \text{Prov}(f, \sigma), \rho.\delta = \texttt{contributed}} \rho.v$$

The provenance total equals the computation total, ensuring audit trails are complete.

### 9.5 Composition Correctness

**Theorem 9.5 (Composition Idempotence).** For any jurisdiction $J$ with no parent:
$$\text{Compose}(\text{Chain}(J)) = J.\mathcal{S}$$

**Theorem 9.6 (Override Correctness).** For jurisdiction $J$ with parent $P$:
$$\forall f \in \text{names}(J.\mathcal{S}.F), \text{Compose}(\text{Chain}(J)).F(f) = J.\mathcal{S}.F(f)$$

Child definitions always take precedence over inherited definitions.

---

## 10. Complexity Analysis

### 10.1 Type Checking

**Proposition 10.1.** Type checking runs in $O(n)$ time where $n$ is the total number of AST nodes, since each node is visited once with constant-time type operations.

### 10.2 Completeness Verification

**Proposition 10.2.** Exhaustive completeness checking has complexity:
$$O\left(\prod_{i \in \mathcal{I}} |\text{Dom}(i)| \cdot |\Phi|\right)$$

where $|\Phi|$ is the number of conditions. For representative sampling with $k$ samples per input:
$$O(k^{|\mathcal{I}|} \cdot |\Phi|)$$

### 10.3 Monotonicity Verification

**Proposition 10.3.** Monotonicity checking has complexity:
$$O(m \cdot k \cdot T_{\text{eval}})$$

where $m$ is the number of context samples, $k$ is the number of target values checked, and $T_{\text{eval}}$ is fee evaluation time.

### 10.4 Composition

**Proposition 10.4.** Jurisdiction composition has complexity $O(d \cdot f)$ where $d$ is the inheritance depth and $f$ is the total number of fees in the chain.

---

## 11. Summary

This section presented formal mathematical foundations for IPFLang's six core innovations:

1. **Currency-Aware Type System** (§2): Dimensional types prevent cross-currency arithmetic errors statically, with typing rules ensuring amounts carry currency tags through all operations.

2. **Completeness Verification** (§3): Coverage analysis ensures fee computations produce defined outputs for all valid inputs, using exhaustive enumeration for small domains and representative sampling for large ones.

3. **Monotonicity Verification** (§4): Directional checks ensure fees behave predictably (e.g., more claims → higher fees), detecting violations through systematic value sampling.

4. **Provenance Semantics** (§5): Formal audit trails record which rules fired, which conditions matched, and how individual contributions compose into totals, with counterfactual generation for "what-if" analysis.

5. **Regulatory Change Semantics** (§6): Version management, change classification, impact analysis, and temporal queries enable tracking how fee schedules evolve while preserving correctness properties.

6. **Temporal Logic** (§7): Business day calculus, late fee formulas, grace period checking, and renewal calculations address time-dependent regulatory requirements.

7. **Jurisdiction Composition** (§8): Inheritance and override semantics enable code reuse across related jurisdictions, with formal metrics quantifying reuse effectiveness.

These foundations provide the theoretical underpinning for IPFLang's implementation while remaining accessible to practitioners through semi-formal presentation and practical algorithms.
