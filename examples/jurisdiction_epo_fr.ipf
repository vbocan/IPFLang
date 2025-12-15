# ============================================================================
# JURISDICTION COMPOSITION EXAMPLE: French National Phase (Child Jurisdiction)
# ============================================================================
#
# PURPOSE:
#   This file extends the EPO base jurisdiction (jurisdiction_epo_base.ipf) with
#   French-specific fees for the national phase entry. It demonstrates:
#   - Inheriting inputs and fees from a parent jurisdiction
#   - Adding France-specific fees (translation, publication, agent)
#   - French language requirements
#
# USAGE:
#   Compose with the base jurisdiction:
#   dotnet run --project src/IPFLang.CLI -- compose examples/jurisdiction_epo_base.ipf examples/jurisdiction_epo_fr.ipf
#
#   With inheritance analysis:
#   dotnet run --project src/IPFLang.CLI -- compose examples/jurisdiction_epo_base.ipf examples/jurisdiction_epo_fr.ipf --analysis
#
# INHERITANCE:
#   From jurisdiction_epo_base.ipf:
#   - Inputs: ApplicationType, ApplicantType, ClaimCount, PageCount
#   - Fees: FilingFee, SearchFee, ExaminationFee, ClaimsFee, PageFee, DesignationFee
#
# FRENCH-SPECIFIC ADDITIONS:
#   - Inputs: NeedsTranslation, TranslationPages, UseFrenchAgent, TranslationQuality
#   - Fees: FrenchValidationFee, FrenchTranslationFee, FrenchAgentFee, FrenchPublicationFee
#
# EXPECTED OUTPUT (with --analysis):
#   Inherited fees: FilingFee, SearchFee, ExaminationFee, ClaimsFee, PageFee, DesignationFee
#   New fees: FrenchValidationFee, FrenchTranslationFee, FrenchAgentFee, FrenchPublicationFee
#   Code reuse: ~60%
# ============================================================================

VERSION '2024.1' EFFECTIVE 2024-01-15 DESCRIPTION 'French National Phase Fees'

# ============================================================================
# FRENCH-SPECIFIC INPUT DEFINITIONS
# ============================================================================
# These inputs are added to the inherited inputs from the EPO base

DEFINE GROUP FrenchPhase AS 'French National Phase' WITH WEIGHT 10

# Does the application need French translation?
DEFINE BOOLEAN NeedsTranslation AS 'Requires French translation?'
GROUP FrenchPhase
DEFAULT TRUE
ENDDEFINE

# Number of pages requiring translation
DEFINE NUMBER TranslationPages AS 'Pages to translate'
GROUP FrenchPhase
BETWEEN 0 AND 500
DEFAULT 50
ENDDEFINE

# Use a French patent agent?
DEFINE BOOLEAN UseFrenchAgent AS 'Use French patent agent?'
GROUP FrenchPhase
DEFAULT TRUE
ENDDEFINE

# Translation quality level
DEFINE LIST TranslationQuality AS 'Translation service level'
GROUP FrenchPhase
CHOICE Standard AS 'Standard (certified translator)'
CHOICE Premium AS 'Premium (IP specialist)'
CHOICE Express AS 'Express (24h delivery)'
DEFAULT Standard
ENDDEFINE

# ============================================================================
# FRENCH-SPECIFIC FEE COMPUTATIONS
# ============================================================================

# French national validation fee (INPI fee)
COMPUTE FEE FrenchValidationFee
YIELD 105<EUR>
ENDCOMPUTE

# French translation fee (required unless originally filed in French)
COMPUTE FEE FrenchTranslationFee
CASE NeedsTranslation EQ TRUE AND TranslationQuality EQ Standard AS
    # Standard rate: 35 EUR per page
    YIELD TranslationPages * 35
ENDCASE
CASE NeedsTranslation EQ TRUE AND TranslationQuality EQ Premium AS
    # Premium rate: 55 EUR per page (IP specialist)
    YIELD TranslationPages * 55
ENDCASE
CASE NeedsTranslation EQ TRUE AND TranslationQuality EQ Express AS
    # Express rate: 75 EUR per page (24h delivery)
    YIELD TranslationPages * 75
ENDCASE
CASE NeedsTranslation EQ FALSE AS
    YIELD 0
ENDCASE
ENDCOMPUTE

# French patent agent fee
COMPUTE FEE FrenchAgentFee
CASE UseFrenchAgent EQ TRUE AS
    # Base agent fee for French representation
    YIELD 850<EUR>
ENDCASE
CASE UseFrenchAgent EQ FALSE AS
    YIELD 0<EUR>
ENDCASE
ENDCOMPUTE

# French publication fee (for national phase publication)
COMPUTE FEE FrenchPublicationFee OPTIONAL
YIELD 90<EUR>
ENDCOMPUTE

# ============================================================================
# VERIFICATION
# ============================================================================

VERIFY COMPLETE FEE FrenchValidationFee
VERIFY COMPLETE FEE FrenchTranslationFee
VERIFY COMPLETE FEE FrenchAgentFee
VERIFY COMPLETE FEE FrenchPublicationFee

# ============================================================================
# RETURN STATEMENT
# ============================================================================

RETURN TotalFees AS 'Total French National Phase Fees'
