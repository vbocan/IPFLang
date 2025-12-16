# =============================================================================
# IPFLang Module: ARIPO-Specific Input Definitions
# =============================================================================
# Input definitions specific to African Regional IP Organization procedures.
#
# Usage: COMPOSE modules/aripo.ipf
# =============================================================================

DEFINE GROUP G_ARIPO AS 'ARIPO Options' WITH WEIGHT 40

# Number of designated countries
DEFINE NUMBER CountryCount AS 'Number of ARIPO designated countries'
GROUP G_ARIPO
BETWEEN 1 AND 22
DEFAULT 1
ENDDEFINE

# ARIPO member states for designation
DEFINE MULTILIST AP_Designations AS 'Designate ARIPO member states'
GROUP G_ARIPO
CHOICE AP_BW AS 'Botswana'
CHOICE AP_GH AS 'Ghana'
CHOICE AP_GM AS 'Gambia'
CHOICE AP_KE AS 'Kenya'
CHOICE AP_LR AS 'Liberia'
CHOICE AP_LS AS 'Lesotho'
CHOICE AP_MW AS 'Malawi'
CHOICE AP_MZ AS 'Mozambique'
CHOICE AP_NA AS 'Namibia'
CHOICE AP_RW AS 'Rwanda'
CHOICE AP_SC AS 'Seychelles'
CHOICE AP_SD AS 'Sudan'
CHOICE AP_SL AS 'Sierra Leone'
CHOICE AP_ST AS 'Sao Tome and Principe'
CHOICE AP_SZ AS 'Eswatini'
CHOICE AP_TZ AS 'Tanzania'
CHOICE AP_UG AS 'Uganda'
CHOICE AP_ZM AS 'Zambia'
CHOICE AP_ZW AS 'Zimbabwe'
DEFAULT AP_KE
ENDDEFINE
