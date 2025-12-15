# ============================================================================
# Entity-Based Discounts Demo
# ============================================================================
#
# DESCRIPTION:
#   Demonstrates fee calculations with entity-based discounts. Many patent
#   offices offer reduced fees for small entities, micro entities, and
#   natural persons. This example shows how to model percentage-based
#   discounts using CASE blocks.
#
# FEATURES DEMONSTRATED:
#   - LIST input with four entity types
#   - NUMBER input for claim count
#   - Percentage-based discounts (75%, 50%)
#   - Cross-product CASE conditions (entity type AND claim count)
#   - Complete coverage of all input combinations
#   - Monotonicity verification (fees increase with claims)
#
# DISCOUNT STRUCTURE:
#   - Large entity: 100% (no discount)
#   - Small entity: 75% of base fee
#   - Micro entity: 50% of base fee
#   - Natural person: 50% of base fee
#
# EXPECTED BEHAVIOR:
#   Run: dotnet run --project src/IPFLang.CLI -- run examples/03_entity_discounts.ipf
#   - Large entity with 20 claims: 500 + (50 x 10) = 1,000
#   - Micro entity with 20 claims: 250 + (25 x 10) = 500
#
# ============================================================================

VERSION '2024.1' EFFECTIVE 2024-01-01 DESCRIPTION 'Entity discounts'

DEFINE LIST EntitySize AS 'Applicant entity size'
CHOICE Entity_Large AS 'Large entity'
CHOICE Entity_Small AS 'Small entity'
CHOICE Entity_Micro AS 'Micro entity'
CHOICE Entity_Person AS 'Natural person'
DEFAULT Entity_Large
ENDDEFINE

DEFINE NUMBER ClaimCount AS 'Number of claims'
BETWEEN 1 AND 200
DEFAULT 10
ENDDEFINE

# Base filing fee with discounts
COMPUTE FEE FilingFee
LET BaseFee AS 500
CASE EntitySize EQ Entity_Large AS
  YIELD BaseFee
ENDCASE
CASE EntitySize EQ Entity_Small AS
  YIELD BaseFee * 0.75
ENDCASE
CASE EntitySize EQ Entity_Micro AS
  YIELD BaseFee * 0.50
ENDCASE
CASE EntitySize EQ Entity_Person AS
  YIELD BaseFee * 0.50
ENDCASE
ENDCOMPUTE

# Claim fees with discounts - using direct input references
COMPUTE FEE ClaimFee
LET BaseClaimFee AS 50
CASE ClaimCount LTE 10 AS
  YIELD 0
ENDCASE
CASE ClaimCount GT 10 AND EntitySize EQ Entity_Large AS
  YIELD BaseClaimFee * (ClaimCount - 10)
ENDCASE
CASE ClaimCount GT 10 AND EntitySize EQ Entity_Small AS
  YIELD BaseClaimFee * (ClaimCount - 10) * 0.75
ENDCASE
CASE ClaimCount GT 10 AND EntitySize EQ Entity_Micro AS
  YIELD BaseClaimFee * (ClaimCount - 10) * 0.50
ENDCASE
CASE ClaimCount GT 10 AND EntitySize EQ Entity_Person AS
  YIELD BaseClaimFee * (ClaimCount - 10) * 0.50
ENDCASE
ENDCOMPUTE

VERIFY COMPLETE FEE FilingFee
VERIFY COMPLETE FEE ClaimFee
VERIFY MONOTONIC FEE ClaimFee WITH RESPECT TO ClaimCount
