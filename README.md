# IPFLang - Domain-Specific Language for Intellectual Property Fee Calculations

[![.NET](https://img.shields.io/badge/.NET-10.0-purple?logo=dotnet)](https://dotnet.microsoft.com)
[![Build Status](https://img.shields.io/badge/build-passing-brightgreen)](https://github.com/vbocan/ipflang)
[![License: GPL v3](https://img.shields.io/badge/License-GPLv3-blue.svg)](https://www.gnu.org/licenses/gpl-3.0)

## Overview

IPFLang is a domain-specific language (DSL) and execution engine for defining intellectual property fee calculations with advanced features for currency handling, completeness verification, provenance tracking, and version management. It enables legal professionals and developers to define complex fee structures in human-readable format without hardcoding business logic.

## Key Features

- **Currency-aware type system** with compile-time validation (supports all 161 ISO 4217 currencies)
- **Static completeness verification** to ensure all input combinations are covered
- **Monotonicity checking** to verify fee schedules behave as expected
- **Provenance tracking** with counterfactual analysis ("what-if" scenarios)
- **Version management** with effective dates and regulatory references
- **Temporal operations** for business days, deadlines, renewals, and late fees
- **Jurisdiction composition** for code reuse across related fee schedules

## Quick Start

### Installation

```bash
# Clone the repository
git clone https://github.com/vbocan/ipflang
cd ipflang/src

# Build the solution
dotnet build

# Run the CLI
dotnet run --project IPFLang.CLI -- --help
```

### CLI Commands

#### `parse` - Parse and validate a script

Parses the IPFLang script and reports any syntax or type errors.

```bash
dotnet run --project src/IPFLang.CLI -- parse <FILE>
```

**Options:**
- `-h, --help` - Show help information

**Example:**
```bash
dotnet run --project src/IPFLang.CLI -- parse examples/01_epo_filing.ipf
```

#### `run` - Execute a script

Executes the script with input values. By default, prompts interactively for each input.

```bash
dotnet run --project src/IPFLang.CLI -- run <FILE> [OPTIONS]
```

**Options:**
- `--inputs <FILE>` - JSON file containing input values (skips interactive mode)
- `-p, --provenance` - Show computation provenance (audit trail)
- `-c, --counterfactuals` - Show counterfactual analysis (what-if scenarios)
- `-h, --help` - Show help information

**Examples:**
```bash
# Interactive mode (default)
dotnet run --project src/IPFLang.CLI -- run examples/01_epo_filing.ipf

# With inputs from JSON file
dotnet run --project src/IPFLang.CLI -- run examples/01_epo_filing.ipf --inputs inputs.json

# Show computation audit trail
dotnet run --project src/IPFLang.CLI -- run examples/01_epo_filing.ipf --provenance

# Show what-if analysis
dotnet run --project src/IPFLang.CLI -- run examples/01_epo_filing.ipf --counterfactuals
```

#### `verify` - Run verification checks

Runs completeness and monotonicity verification on all fees with VERIFY directives.

```bash
dotnet run --project src/IPFLang.CLI -- verify <FILE>
```

**Options:**
- `-h, --help` - Show help information

**Example:**
```bash
dotnet run --project src/IPFLang.CLI -- verify examples/05_verification.ipf
```

#### `info` - Display script information

Shows detailed information about a script including inputs, fees, groups, and version metadata.

```bash
dotnet run --project src/IPFLang.CLI -- info <FILE>
```

**Options:**
- `-h, --help` - Show help information

**Example:**
```bash
dotnet run --project src/IPFLang.CLI -- info examples/01_epo_filing.ipf
```

#### `compose` - Compose multiple jurisdictions

Composes multiple IPFLang scripts with inheritance. Files are provided in order from root (parent) to leaf (child). Child jurisdictions inherit inputs and fees from parents and can add or override them.

```bash
dotnet run --project src/IPFLang.CLI -- compose <FILES> [OPTIONS]
```

**Options:**
- `--inputs <FILE>` - JSON file containing input values (skips interactive mode)
- `-p, --provenance` - Show computation provenance (audit trail)
- `-a, --analysis` - Show inheritance analysis (what is inherited vs. overridden)
- `-h, --help` - Show help information

**Examples:**
```bash
# Compose EPO base with German national phase
dotnet run --project src/IPFLang.CLI -- compose examples/jurisdiction_epo_base.ipf examples/jurisdiction_epo_de.ipf

# Compose EPO base with French national phase
dotnet run --project src/IPFLang.CLI -- compose examples/jurisdiction_epo_base.ipf examples/jurisdiction_epo_fr.ipf

# Compose EPO base with Romanian national phase
dotnet run --project src/IPFLang.CLI -- compose examples/jurisdiction_epo_base.ipf examples/jurisdiction_epo_ro.ipf

# Show inheritance analysis
dotnet run --project src/IPFLang.CLI -- compose examples/jurisdiction_epo_base.ipf examples/jurisdiction_epo_de.ipf --analysis

# With provenance tracking
dotnet run --project src/IPFLang.CLI -- compose examples/jurisdiction_epo_base.ipf examples/jurisdiction_epo_fr.ipf --provenance
```

**Output (with --analysis):**
```
Inheritance Analysis:

jurisdiction_epo_de:
  Inherited fees: FilingFee, SearchFee, ExaminationFee, ClaimsFee, PageFee, DesignationFee
  New fees: GermanValidationFee, GermanTranslationFee, GermanAgentFee, GermanPublicationFee
  Inherited inputs: ApplicationType, ApplicantType, ClaimCount, PageCount
  New inputs: NeedsTranslation, TranslationPages, UseGermanAgent, TranslationQuality
  Code reuse: 60.0%
```

### Example DSL Script

```
# European Patent Office Filing Fee Calculator
VERSION '2024.1' EFFECTIVE 2024-01-15 DESCRIPTION 'EPO fees 2024'

# Define groups for UI organization
DEFINE GROUP General AS 'General Information' WITH WEIGHT 1
DEFINE GROUP Claims AS 'Claims Information' WITH WEIGHT 2

# Define inputs
DEFINE LIST EntityType AS 'Applicant type'
GROUP General
CHOICE LargeEntity AS 'Large entity'
CHOICE SmallEntity AS 'SME'
CHOICE MicroEntity AS 'Micro entity'
DEFAULT LargeEntity
ENDDEFINE

DEFINE NUMBER ClaimCount AS 'Number of claims'
GROUP Claims
BETWEEN 1 AND 500
DEFAULT 10
ENDDEFINE

# Compute fees with conditional logic
COMPUTE FEE FilingFee
CASE EntityType EQ LargeEntity AS
  YIELD 1500
ENDCASE
CASE EntityType EQ SmallEntity AS
  YIELD 750
ENDCASE
CASE EntityType EQ MicroEntity AS
  YIELD 375
ENDCASE
ENDCOMPUTE

COMPUTE FEE ExcessClaimsFee
LET ExcessClaims AS ClaimCount - 15
YIELD 250 * ExcessClaims IF ExcessClaims GT 0
ENDCOMPUTE

# Verification directives
VERIFY COMPLETE FEE FilingFee
VERIFY MONOTONIC FEE ExcessClaimsFee WITH RESPECT TO ClaimCount

RETURN TotalFees AS 'Total Filing Fees'
```

### Running Tests

```bash
# Run all tests
dotnet test

# Run specific test project
dotnet test IPFLang.Engine.Tests
```

## Documentation

- **[Syntax Reference](docs/SYNTAX.md)**: Complete IPFLang syntax documentation
- **[Examples](examples/)**: Sample fee schedule scripts demonstrating all features
- **[Article](article/)**: Academic paper describing the DSL design and mathematical foundations

## Project Structure

```
src/
├── IPFLang.Engine/          # Core DSL parser and evaluator
│   ├── Analysis/            # Completeness & monotonicity checking
│   ├── Calculator/          # Fee computation engine
│   ├── Composition/         # Jurisdiction inheritance
│   ├── CurrencyConversion/  # Multi-currency support
│   ├── Evaluator/           # Expression evaluation
│   ├── Parser/              # DSL parsing
│   ├── Provenance/          # Audit trail & counterfactuals
│   ├── Temporal/            # Date/deadline calculations
│   ├── Types/               # Type system
│   ├── Validation/          # Input validation
│   └── Versioning/          # Version management
├── IPFLang.Engine.Tests/    # Unit tests
└── IPFLang.CLI/             # Command-line interface
examples/                    # Example DSL scripts
article/                     # Research paper and documentation
docs/                        # Documentation
```

## Examples

The `examples/` directory contains scripts demonstrating all IPFLang features:

| Example | Description |
|---------|-------------|
| `01_epo_filing.ipf` | EPO filing fees with VERSION, GROUPs, LIST inputs, CASE blocks |
| `02_currency_types.ipf` | Currency literals and type-safe multi-currency operations |
| `03_entity_discounts.ipf` | Complex discount logic based on entity type |
| `04_temporal_operations.ipf` | DATE inputs, renewal fees, temporal calculations |
| `05_verification.ipf` | VERIFY COMPLETE and VERIFY MONOTONIC directives |
| `06_multilevel_cases.ipf` | Nested conditions and complex decision trees |
| `07_multilist.ipf` | MULTILIST for multi-selection inputs (e.g., validation countries) |
| `08_optional_fees.ipf` | OPTIONAL keyword for fees that may or may not be charged |
| `09_versioning.ipf` | VERSION directive with effective dates and regulatory references |
| `10_uspto_complete.ipf` | Complete real-world USPTO fee calculator |

### Jurisdiction Composition Examples

The following examples demonstrate multi-jurisdiction composition with inheritance:

| Example | Description |
|---------|-------------|
| `jurisdiction_epo_base.ipf` | Base EPO fee schedule (parent jurisdiction) |
| `jurisdiction_epo_de.ipf` | German national phase (extends EPO base with translation/agent fees) |
| `jurisdiction_epo_fr.ipf` | French national phase (extends EPO base with French-specific fees) |
| `jurisdiction_epo_ro.ipf` | Romanian national phase (extends EPO base with OSIM fees, reduced rates for local applicants) |

**Usage:**
```bash
# Compose EPO base with German national phase
dotnet run --project src/IPFLang.CLI -- compose examples/jurisdiction_epo_base.ipf examples/jurisdiction_epo_de.ipf

# Compose EPO base with French national phase
dotnet run --project src/IPFLang.CLI -- compose examples/jurisdiction_epo_base.ipf examples/jurisdiction_epo_fr.ipf

# Compose EPO base with Romanian national phase
dotnet run --project src/IPFLang.CLI -- compose examples/jurisdiction_epo_base.ipf examples/jurisdiction_epo_ro.ipf

# Show what is inherited vs. overridden
dotnet run --project src/IPFLang.CLI -- compose examples/jurisdiction_epo_base.ipf examples/jurisdiction_epo_de.ipf --analysis
```

### Error Examples

The `examples/errors/` directory contains scripts that **intentionally fail** to demonstrate the DSL's validation capabilities:

| Example | Error Type | What It Demonstrates |
|---------|------------|---------------------|
| `err_01_mixed_currency.ipf` | Type Error | Cross-currency arithmetic (`100<EUR> + 50<USD>`) |
| `err_02_incomplete_coverage.ipf` | Verification Failure | Missing CASE for input value (incomplete coverage) |
| `err_03_non_monotonic.ipf` | Verification Failure | Fee decreases as input increases (violates monotonicity) |
| `err_04_invalid_currency.ipf` | Parse Error | Invalid ISO 4217 currency code (`XXX`) |
| `err_05_undefined_variable.ipf` | Type Error | Reference to undefined variable |
| `err_06_missing_currency.ipf` | Parse Error | AMOUNT input without CURRENCY declaration |

## Citation

If you use IPFLang in your research, please cite:

```bibtex
@software{ipflang2025,
  title = {IPFLang: Domain-Specific Language for IP Fee Calculations},
  author = {Bocan, Valer},
  year = {2025},
  version = {1.0.0},
  url = {https://github.com/vbocan/ipflang},
  license = {GPL-3.0}
}
```

See [CITATION.cff](CITATION.cff) for structured citation metadata.

## Contributing

We welcome contributions from the community:

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

For major changes, please open an issue first to discuss proposed modifications.

## License

This project is licensed under the GNU General Public License v3.0 - see the [LICENSE](LICENSE) file for details.

## Author

**Valer Bocan, Ph.D., CSSLP**

## Support & Contact

- **Issues**: Report bugs and request features via [GitHub Issues](https://github.com/vbocan/ipflang/issues)
- **Discussions**: Community support via [GitHub Discussions](https://github.com/vbocan/ipflang/discussions)
- **Email**: valer.bocan@upt.ro
