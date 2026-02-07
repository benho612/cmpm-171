/*
1. Asset Creation (The "Data" Phase)
Before you can test, you need "stuff" in your folders.

[ ] Run Stats: Create 5-10 StatUpgrade assets (e.g., Glass Cannon, Tank's Resolve, Fleet Footed).

[ ] Meta Elements: Create your 3 ElementUnlock assets (Fire, Ice, Rock).

[ ] Meta Passives: Create assets for the "Conditional" boosts (e.g., Burning Flesh, Brittle Bones).

[ ] The Combo Library: Create the assets for your base combos (e.g., LLH, HHH, LHL).

2. The "Managers" (The "Wiring" Phase)
Your upgrades need to talk to a central "Brain" that stays active throughout the game.

[ ] MetaManager: A script that holds the list of all MetaUnlock assets. The ComboUpgrade will look here to see what elements are unlocked.

[X] RunManager / GameManager: * Needs the allPossibleUpgrades list.

Needs the PrepareUpgradeMenu() logic to shuffle and "Pre-Roll" elements for combos.

[X] The RunData SO: Ensure you have one physical asset of RunData that is shared by the Player and the Upgrades.

3. UI Implementation (The "Visual" Phase)
Even with great code, the player needs to see the choices.

[X] Upgrade Canvas: Create the UI with 3 Buttons.

[X] ButtonLogic: Attach your script to these buttons and link the OnClick events.

[ ] Skill Tree Menu: Create a separate scene or panel where the player can click those MetaUnlock assets to toggle isUnlocked = true.

4. Gameplay "Receivers" (The "Integration" Phase)
These are the scripts your teammates (or you) will eventually write. You need to make sure your upgrades have a place to plug in.

[X] CombatHandler: Needs the UnlockCombo(string) function and a list to store them.

[ ] Enemy/Health Script: Needs to understand Status Effects. It needs to store a string like "Burning" so your Passives know when to trigger.

[ ] The "Split" Logic: Inside the CombatHandler, implement the code to turn "LLH_Fire" into "Play LLH Animation" + "Spawn Fire Particles."

5. Saving & Persistence (The "Final Polish")
[ ] Meta-Reset: A button in your developer menu to set all isUnlocked bools back to false for testing.

[ ] Run-Reset: Ensure the GameManager calls runData.ResetStats() every time a new run starts.
*/