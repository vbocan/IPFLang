# =============================================================================
# IPFLang Module: AP_Designations
# =============================================================================
# Define CountryCount as NUMBER w/ DEF=22
#
# This module defines reusable input definitions for IP fee calculations.
# Include in jurisdiction files using: COMPOSE modules/ap_designations.ipf
# =============================================================================

DEFINE NUMBER CountryCount AS 'Number of designations?'
GROUP G_ARIPO
BETWEEN 1 AND 22
DEFAULT 22  # number of ARIPO member states
# Botswana, Eswatini, Gambia, Ghana, Kenya, Lesotho, Liberia, Malawi, Mozambique, Namibia, Rwanda, Sao Tome and Principe, Sierra Leone, Somalia (not member of the Harare Protocol), the Sudan, the United Republic of Tanzania, Uganda, Zambia and Zimbabwe
ENDDEFINE