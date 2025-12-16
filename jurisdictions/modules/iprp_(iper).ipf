# =============================================================================
# IPFLang Module: IPRP (IPER)
# =============================================================================
# Define IPRP as LIST w/ DEF=IPRP_NONE
#
# This module defines reusable input definitions for IP fee calculations.
# Include in jurisdiction files using: COMPOSE modules/iprp (iper).ipf
# =============================================================================

# International preliminary report on patentability (Chapter II of the PCT; IPER)
DEFINE LIST IPRP AS 'Which office provided an IPRP (Chapter II)/IPER?'
GROUP G_PCT
CHOICE IPRP_NONE AS 'NONE'
CHOICE IPRP_AT AS 'AT - Austrian Patent Office'
CHOICE IPRP_AU AS 'AU - IP Australia'
CHOICE IPRP_BR AS 'BR - National Institute of Industrial Property (Brazil)'
CHOICE IPRP_CA AS 'CA - Canadian Intellectual Property Office'
CHOICE IPRP_CL AS 'CL - National Institute of Industrial Property of Chile'
CHOICE IPRP_CN AS 'CN - China National Intellectual Property Administration (CNIPA)'
CHOICE IPRP_EG AS 'EG - Egyptian Patent Office'
CHOICE IPRP_EPO AS 'EP - European Patent Office (EPO)'
CHOICE IPRP_ES AS 'ES - Spanish Patent and Trademark Office'
CHOICE IPRP_FI AS 'FI - Finnish Patent and Registration Office (PRH)'
CHOICE IPRP_IL AS 'IL - Israel Patent Office'
CHOICE IPRP_IN AS 'IN - Indian Patent Office'
CHOICE IPRP_JP AS 'JP - Japan Patent Office'
CHOICE IPRP_KR AS 'KR - Korean Intellectual Property Office'
CHOICE IPRP_PH AS 'PH - Intellectual Property Office of the Philippines'
CHOICE IPRP_RU AS 'RU - Federal Service for Intellectual Property, Patents and Trademarks (Russian Federation)'
CHOICE IPRP_SE AS 'SE - Swedish Intellectual Property Office (PRV)'
CHOICE IPRP_SG AS 'SG - Intellectual Property Office of Singapore'
CHOICE IPRP_TR AS 'TR - Turkish Patent and Trademark Office'
CHOICE IPRP_UA AS 'UA - Ministry for Development of Economy, Trade and Agriculture of Ukraine, Department for Development of Intellectual Property'
CHOICE IPRP_USPTO AS 'US - United States Patent and Trademark Office (USPTO)'
CHOICE IPRP_XN AS 'XN - Nordic Patent Institute'
CHOICE IPRP_XV AS 'XV - Visegrad Patent Institute'
DEFAULT IPRP_NONE
ENDDEFINE