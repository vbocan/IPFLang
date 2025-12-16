# =============================================================================
# IPFLang Example: EPO Germany (EPO-DE) - National Validation Phase
# =============================================================================
# This file EXTENDS the EPO base fee schedule for German national validation.
# It is a CHILD jurisdiction in the composition hierarchy.
#
# Jurisdiction Hierarchy:
#   EPO Base (parent - inherited)
#       └── [EPO-DE] (this file)
#
# What this file demonstrates:
#   1. INHERITING inputs and fees from parent (EPO base)
#   2. ADDING new inputs specific to Germany (translation requirements)
#   3. ADDING new fees specific to Germany (translation, validation, agent fees)
#   4. NOT overriding any parent fees (all EPO fees remain as-is)
#
# When composed with parent:
#   - All EPO base inputs are available
#   - All EPO base fees are calculated
#   - German-specific inputs and fees are ADDED
#   - Total = EPO fees + German national fees
#
# Usage:
#   dotnet run --project src/IPFLang.CLI -- compose examples/jurisdiction_epo_base.ipf examples/jurisdiction_epo_de.ipf
#   dotnet run --project src/IPFLang.CLI -- compose examples/jurisdiction_epo_base.ipf examples/jurisdiction_epo_de.ipf --analysis
#
# =============================================================================

VERSION '2024.2' EFFECTIVE 2024-01-01 DESCRIPTION 'EPO Germany Validation 2024'

# Additional inputs for Germany
DEFINE GROUP GermanGroup AS 'German Validation Options' WITH WEIGHT 3

DEFINE BOOLEAN NeedsTranslation AS 'Translation to German required'
GROUP GermanGroup
DEFAULT TRUE
ENDDEFINE

DEFINE NUMBER TranslationPages AS 'Number of pages to translate'
GROUP GermanGroup
BETWEEN 0 AND 500
DEFAULT 30
ENDDEFINE

DEFINE BOOLEAN UseGermanAgent AS 'Use German patent attorney'
GROUP GermanGroup
DEFAULT TRUE
ENDDEFINE

DEFINE LIST TranslationQuality AS 'Translation quality level'
GROUP GermanGroup
CHOICE STANDARD AS 'Standard translation'
CHOICE CERTIFIED AS 'Certified translation'
CHOICE PREMIUM AS 'Premium with review'
DEFAULT STANDARD
ENDDEFINE

# -----------------------------------------------------------------------------
# Germany-Specific Fees (NEW, not inherited)
# -----------------------------------------------------------------------------

COMPUTE FEE GermanValidationFee
YIELD 60<EUR>
ENDCOMPUTE

COMPUTE FEE GermanTranslationFee
CASE NeedsTranslation EQ TRUE AND TranslationQuality EQ STANDARD AS
  YIELD TranslationPages * 35
ENDCASE
CASE NeedsTranslation EQ TRUE AND TranslationQuality EQ CERTIFIED AS
  YIELD TranslationPages * 55
ENDCASE
CASE NeedsTranslation EQ TRUE AND TranslationQuality EQ PREMIUM AS
  YIELD TranslationPages * 85
ENDCASE
CASE NeedsTranslation EQ FALSE AS
  YIELD 0
ENDCASE
ENDCOMPUTE

COMPUTE FEE GermanAgentFee
CASE UseGermanAgent EQ TRUE AND ApplicationType EQ STANDARD AS
  YIELD 450<EUR>
ENDCASE
CASE UseGermanAgent EQ TRUE AND ApplicationType EQ DIVISIONAL AS
  YIELD 650<EUR>
ENDCASE
CASE UseGermanAgent EQ TRUE AND ApplicationType EQ PCT_ENTRY AS
  YIELD 600<EUR>
ENDCASE
CASE UseGermanAgent EQ FALSE AS
  YIELD 0
ENDCASE
ENDCOMPUTE

COMPUTE FEE GermanPublicationFee OPTIONAL
YIELD 150<EUR>
ENDCOMPUTE

# Verification directives for new fees
VERIFY COMPLETE FEE GermanValidationFee
VERIFY COMPLETE FEE GermanTranslationFee
VERIFY COMPLETE FEE GermanAgentFee
VERIFY COMPLETE FEE GermanPublicationFee
VERIFY MONOTONIC FEE GermanTranslationFee WITH RESPECT TO TranslationPages

# Updated Returns (override parent)
RETURN Currency AS 'EUR'
RETURN JurisdictionLevel AS 'EPO + Germany National Phase'
RETURN TotalFees AS 'Total EPO + German Validation Fees'
RETURN CompositionNote AS 'Fees include EPO regional + German national phase'
