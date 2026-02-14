/*
1. The Unity Editor Phase (The "Bulk" Work)
Since you have the ScriptableObject classes ready, you now need to create the actual assets they will use.

[ ] Create Combo Library: In your Project folder, create .asset files for every combo (e.g., L, LH, LLH).

[ ] Ensure IsFinisher is Checked for finishers (e.g., LLH) and Unchecked for middle-moves (e.g., L, LH).

[ ] Create Meta Progression Assets: Create .asset files for your Skill Tree (e.g., FireMastery, BurnDamage).

[ ] Wiring: In the Inspector, drag the "Prerequisite" assets into the slots to build the tree logic.

[ ] Populate AllUpgrades: In your GameManager inspector, drag all your ComboUnlock and CombatStatUpgrade assets into the AllUpgrades list.

2. The UI Phase (Visual Implementation)
[ ] Skill Tree Layout:

[ ] Place your SkillTreeNode prefabs onto your UI Canvas.

[ ] Assign the correct MetaUnlock asset to each button in the Inspector.

[ ] Run Upgrade Menu:

[ ] Ensure your UpgradeButtons array in the GameManager is filled with the buttons from your "In-Run" UI.

[ ] Currency Display: Create a small script or update UpdateUI() to show MetaData.MetaCurrency on screen so players know what they can afford.

3. The "Relay Race" Wiring (Integration Prep)
[ ] Enemy Status Placeholder: Create a simple script (or tell your teammate) to add public string currentStatus = "None"; to the Enemy objects.

[ ] Hit Detection Bridge:

[ ] Coordinate with Jayson: He must call _combatHandler.ProcessHit(enemyObject) whenever his hitbox trigger touches an enemy.

[ ] Status Application Logic: Update your ProcessHit in CombatHandler to apply the status to the enemy (e.g., if _activeElement is "Fire", change the enemy's currentStatus to "Burning").
*/