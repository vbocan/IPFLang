# =============================================================================
# IPFLang Module: AutoRun
# =============================================================================
# Autorun Module with GROUP definitions
#
# This module defines reusable input definitions for IP fee calculations.
# Include in jurisdiction files using: COMPOSE modules/autorun.ipf
# =============================================================================

# Description texts for groups and their weights

DEFINE GROUP G_APP AS 'Application Details' WITH WEIGHT 10
DEFINE GROUP G_PCT AS 'PCT Details' WITH WEIGHT 20
DEFINE GROUP G_APL AS 'Applicant Details' WITH WEIGHT 30
DEFINE GROUP G_EPO AS 'EPO Details' WITH WEIGHT 40
DEFINE GROUP G_ARIPO AS 'ARIPO Details' WITH WEIGHT 50
