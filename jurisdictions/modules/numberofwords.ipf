# =============================================================================
# IPFLang Module: NumberOfWords
# =============================================================================
# Define NumberOfWords as NUMBER w/ DEF=1
#
# This module defines reusable input definitions for IP fee calculations.
# Include in jurisdiction files using: COMPOSE modules/numberofwords.ipf
# =============================================================================

#asks user the number of words of the patent application

DEFINE NUMBER NumberOfWords AS 'Number of words?'
GROUP G_APP
BETWEEN 1 AND 10000000
DEFAULT 1
ENDDEFINE
