/*
1. The Player-System Handshake

[ ] Input-to-Logic Bridge: Ensure your CombatCoordinator is receiving events from the New Input System (e.g., OnLightAttack) and passing those strings ("L" or "H") into the RecordInput function.

[ ] Animation Event Wiring: Work with your teammate to place Animation Events on the attack clips. These should trigger the hitbox and call CombatHandler.ProcessHit(enemy) at the exact moment of impact.

[ ] Active Element Sync: Double-check that CombatHandler.ExecuteMove correctly parses the ElementType from the moveID string so ProcessHit knows which multipliers to pull.

2. UI & Meta-Progression Verification

[ ] Currency Real-Time Update: Confirm your MetaCurrencyDisplay updates immediately after a SkillTreeNode.OnClick event so the player sees the "Gems" deducted.

[ ] Prerequisite Visual Chain: Verify that unlocking a "Basic" node immediately makes its "Child" nodes in the Skill Tree interactable and changes their color.

[ ] Runtime Combo Injection: Test if buying a combo in the UI successfully adds it to the CombatHandler.UnlockedCombos list so the CombatCoordinator can actually find and play the move.

3. The "Relay Race" (Data Flow Testing)

[ ] Dummy Stagger Test: Attach your TempStatusScript to a cube and verify that hitting it with a "Stone" finisher correctly sets currentStatus to Concussed.

[ ] Multiplier Validation: Use Debug.Log in ProcessHit to print out finalDamage. Ensure that hitting a "Burning" enemy with a "Weak Flesh" upgrade active actually results in a higher number.

[ ] IsFinisher Safety: Confirm that hitting an enemy with a "mid-combo" move (like the first "L") does not reset the combo or apply an elemental status.
*/