# =============================================================================
# IPFLang Module: EPO-Specific Input Definitions
# =============================================================================
# Input definitions specific to European Patent Office procedures.
#
# Usage: COMPOSE modules/epo.ipf
# =============================================================================

DEFINE GROUP G_EPO AS 'EPO Options' WITH WEIGHT 40

# Validation countries (states where EP patent can be validated)
DEFINE MULTILIST VAL_CTY AS 'EP: Select validation countries'
GROUP G_EPO
CHOICE VAL_NONE AS 'None'
CHOICE VAL_KH AS 'Cambodia'
CHOICE VAL_MA AS 'Morocco'
CHOICE VAL_MD AS 'Republic of Moldova'
CHOICE VAL_TN AS 'Tunisia'
DEFAULT VAL_NONE
ENDDEFINE

# Extension state (Bosnia and Herzegovina)
DEFINE BOOLEAN Extension AS 'EP: Designate Bosnia and Herzegovina (extension)?'
GROUP G_EPO
DEFAULT FALSE
ENDDEFINE

# Request early processing
DEFINE BOOLEAN EarlyProcessing AS 'EP: Request early processing?'
GROUP G_EPO
DEFAULT FALSE
ENDDEFINE
