# =============================================================================
# IPFLang Module: EP_VAL_CTY
# =============================================================================
# Define VAL_CTY as MULTILIST w/ DEF=VAL_NONE
#
# This module defines reusable input definitions for IP fee calculations.
# Include in jurisdiction files using: COMPOSE modules/ep_val_cty.ipf
# =============================================================================

DEFINE MULTILIST VAL_CTY AS 'EP: Select validation countries'
GROUP G_EPO
CHOICE VAL_NONE AS 'NONE'
CHOICE VAL_CD AS 'Cambodia'
CHOICE VAL_MA AS 'Morocco'
CHOICE VAL_MD AS 'Republic of Moldova'
CHOICE VAL_TN AS 'Tunisia'
DEFAULT VAL_NONE
ENDDEFINE