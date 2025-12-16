# =============================================================================
# IPFLang Module: SheetCount
# =============================================================================
# Define SheetCount as NUMBER w/ DEF=10
#
# This module defines reusable input definitions for IP fee calculations.
# Include in jurisdiction files using: COMPOSE modules/sheetcount.ipf
# =============================================================================

DEFINE NUMBER SheetCount AS 'Number of sheets?'
GROUP G_APP
BETWEEN 1 AND 10000
DEFAULT 10
ENDDEFINE