# =============================================================================
# IPFLang Module: EntitySize
# =============================================================================
# Define EntitySize as LIST w/ DEF=Entity_Large
#
# This module defines reusable input definitions for IP fee calculations.
# Include in jurisdiction files using: COMPOSE modules/entitysize.ipf
# =============================================================================

DEFINE LIST EntitySize AS 'Entity size of applicant?'
GROUP G_APL
CHOICE Entity_Person AS 'Natural person'
CHOICE Entity_Less25 AS 'Less than 25 employees'
CHOICE Entity_Less100 AS 'Less than 100, more than 25 employees'
CHOICE Entity_Large AS 'Large entity (default)'
CHOICE Entity_Small AS 'Small entity'
CHOICE Entity_Micro AS 'Micro entity'
DEFAULT Entity_Large
ENDDEFINE