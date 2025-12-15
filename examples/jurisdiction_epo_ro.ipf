# ============================================================================
# JURISDICTION COMPOSITION EXAMPLE: Romanian National Phase (Child Jurisdiction)
# ============================================================================
#
# PURPOSE:
#   This file extends the EPO base jurisdiction (jurisdiction_epo_base.ipf) with
#   Romanian-specific fees for the national phase entry. It demonstrates:
#   - Inheriting inputs and fees from a parent jurisdiction
#   - Adding Romania-specific fees (translation, OSIM fees, agent)
#   - Romanian language requirements
#
# USAGE:
#   Compose with the base jurisdiction:
#   dotnet run --project src/IPFLang.CLI -- compose examples/jurisdiction_epo_base.ipf examples/jurisdiction_epo_ro.ipf
#
#   With inheritance analysis:
#   dotnet run --project src/IPFLang.CLI -- compose examples/jurisdiction_epo_base.ipf examples/jurisdiction_epo_ro.ipf --analysis
#
# INHERITANCE:
#   From jurisdiction_epo_base.ipf:
#   - Inputs: ApplicationType, ApplicantType, ClaimCount, PageCount
#   - Fees: FilingFee, SearchFee, ExaminationFee, ClaimsFee, PageFee, DesignationFee
#
# ROMANIAN-SPECIFIC ADDITIONS:
#   - Inputs: NeedsTranslation, TranslationPages, UseRomanianAgent, TranslationService
#   - Fees: RomanianValidationFee, RomanianTranslationFee, RomanianAgentFee, OSIMPublicationFee
#
# NOTES:
#   Romania uses RON (Romanian Leu) for OSIM fees but EUR for most other services.
#   OSIM = Oficiul de Stat pentru Inventii si Marci (State Office for Inventions and Trademarks)
#
# EXPECTED OUTPUT (with --analysis):
#   Inherited fees: FilingFee, SearchFee, ExaminationFee, ClaimsFee, PageFee, DesignationFee
#   New fees: RomanianValidationFee, RomanianTranslationFee, RomanianAgentFee, OSIMPublicationFee
#   Code reuse: ~60%
# ============================================================================

VERSION '2024.1' EFFECTIVE 2024-01-15 DESCRIPTION 'Romanian National Phase Fees'

# ============================================================================
# ROMANIAN-SPECIFIC INPUT DEFINITIONS
# ============================================================================
# These inputs are added to the inherited inputs from the EPO base

DEFINE GROUP RomanianPhase AS 'Romanian National Phase' WITH WEIGHT 10

# Does the application need Romanian translation?
DEFINE BOOLEAN NeedsTranslation AS 'Requires Romanian translation?'
GROUP RomanianPhase
DEFAULT TRUE
ENDDEFINE

# Number of pages requiring translation
DEFINE NUMBER TranslationPages AS 'Pages to translate'
GROUP RomanianPhase
BETWEEN 0 AND 500
DEFAULT 50
ENDDEFINE

# Use a Romanian patent agent?
DEFINE BOOLEAN UseRomanianAgent AS 'Use Romanian patent agent?'
GROUP RomanianPhase
DEFAULT TRUE
ENDDEFINE

# Translation service level
DEFINE LIST TranslationService AS 'Translation service type'
GROUP RomanianPhase
CHOICE Standard AS 'Standard (authorized translator)'
CHOICE Technical AS 'Technical (patent specialist)'
CHOICE Urgent AS 'Urgent (48h delivery)'
DEFAULT Standard
ENDDEFINE

# Applicant is Romanian entity? (reduced fees apply)
DEFINE BOOLEAN IsRomanianApplicant AS 'Romanian applicant (reduced fees)?'
GROUP RomanianPhase
DEFAULT FALSE
ENDDEFINE

# ============================================================================
# ROMANIAN-SPECIFIC FEE COMPUTATIONS
# ============================================================================

# Romanian national validation fee (OSIM fee)
# Romanian applicants get a 50% reduction
COMPUTE FEE RomanianValidationFee
CASE IsRomanianApplicant EQ TRUE AS
    # Reduced fee for Romanian entities: 250 RON ≈ 50 EUR
    YIELD 50<EUR>
ENDCASE
CASE IsRomanianApplicant EQ FALSE AS
    # Standard fee: 500 RON ≈ 100 EUR
    YIELD 100<EUR>
ENDCASE
ENDCOMPUTE

# Romanian translation fee
# Romania has lower translation costs compared to Western Europe
COMPUTE FEE RomanianTranslationFee
CASE NeedsTranslation EQ TRUE AND TranslationService EQ Standard AS
    # Standard rate: 20 EUR per page (authorized translator)
    YIELD TranslationPages * 20
ENDCASE
CASE NeedsTranslation EQ TRUE AND TranslationService EQ Technical AS
    # Technical rate: 35 EUR per page (patent specialist)
    YIELD TranslationPages * 35
ENDCASE
CASE NeedsTranslation EQ TRUE AND TranslationService EQ Urgent AS
    # Urgent rate: 50 EUR per page (48h delivery)
    YIELD TranslationPages * 50
ENDCASE
CASE NeedsTranslation EQ FALSE AS
    YIELD 0
ENDCASE
ENDCOMPUTE

# Romanian patent agent fee
# Generally more affordable than Western European agents
COMPUTE FEE RomanianAgentFee
CASE UseRomanianAgent EQ TRUE AND IsRomanianApplicant EQ TRUE AS
    # Discounted rate for Romanian entities
    YIELD 400<EUR>
ENDCASE
CASE UseRomanianAgent EQ TRUE AND IsRomanianApplicant EQ FALSE AS
    # Standard agent fee for foreign applicants
    YIELD 600<EUR>
ENDCASE
CASE UseRomanianAgent EQ FALSE AS
    YIELD 0<EUR>
ENDCASE
ENDCOMPUTE

# OSIM publication fee (for national phase publication in BOPI)
# BOPI = Buletinul Oficial de Proprietate Industriala
COMPUTE FEE OSIMPublicationFee OPTIONAL
CASE IsRomanianApplicant EQ TRUE AS
    # Reduced publication fee
    YIELD 40<EUR>
ENDCASE
CASE IsRomanianApplicant EQ FALSE AS
    # Standard publication fee
    YIELD 75<EUR>
ENDCASE
ENDCOMPUTE

# Annual maintenance fee (first year)
# Romanian maintenance fees are significantly lower than other EU countries
COMPUTE FEE RomanianMaintenanceFeeYear1 OPTIONAL
CASE IsRomanianApplicant EQ TRUE AS
    YIELD 25<EUR>
ENDCASE
CASE IsRomanianApplicant EQ FALSE AS
    YIELD 50<EUR>
ENDCASE
ENDCOMPUTE

# ============================================================================
# VERIFICATION
# ============================================================================

VERIFY COMPLETE FEE RomanianValidationFee
VERIFY COMPLETE FEE RomanianTranslationFee
VERIFY COMPLETE FEE RomanianAgentFee
VERIFY COMPLETE FEE OSIMPublicationFee
VERIFY COMPLETE FEE RomanianMaintenanceFeeYear1

# ============================================================================
# RETURN STATEMENT
# ============================================================================

RETURN TotalFees AS 'Total Romanian National Phase Fees'
