/*
1. Asset & Data Phase (The "Content" Phase)
[ ] Populate Meta Library: Create the actual .asset files for your Elemental Features (e.g., Fire Spread, Ice Freeze, Rock Armor).

[ ] Set Prerequisites: In the Inspector, drag the "Mastery" assets into the Prerequisite slots of the "Feature" assets to build your tree's logic.

[ ] The Combo Library: Ensure your base combos (LLH, etc.) are created and ready to receive randomized elements.

2. The "Managers" (The "Wiring" Phase)
[x] MetaManager: Logic complete. (Search by Feature ID, search by Stat Multiplier, Singleton access).

[x] SkillTreeManager: Logic complete. (Registry of nodes, Global Refresh, Singleton access).

[ ] The "Auto-Link" (Optional): In SkillTreeManager, implement the GetComponentsInChildren logic so you don't have to manually fill the AllNodes list.

3. UI Implementation (The "Visual" Phase)
[x] Skill Tree Nodes: Logic complete. (Refresh visuals based on SO state, Handle Purchase, Currency Check).

[ ] The Skill Tree Layout: Actually place your buttons in the Unity Canvas in a "Tree" shape.

[ ] The Currency HUD: Create a small UI element that displays GameManager.Instance.MetaData.MetaCurrency so the player sees their money.

[ ] Visual Feedback: Tweak your RefreshNode colors to make the "Locked" state look distinct from the "Available" state.

4. Gameplay "Receivers" (The "Integration" Phase)
[ ] Status Effect System: Update your Enemy/Health script to actually store a string or enum for status (e.g., "Burning"). This is what the MetaManager will check against for stat boosts.

[ ] Combat Calculation: In the player's damage logic, call MetaManager.Instance.StatIncreaseCheck() to apply those permanent meta-bonuses to the base damage.

[ ] The "Split" Logic: Implement the string splitting in CombatHandler to separate the move name from the element name (e.g., LLH | Fire).

5. Saving & Persistence (The "Final Polish")
[x] Meta-Reset: You have the logic to wipe isUnlocked bools for testing.

[ ] Save to Disk: (Future Task) Implement a system to save your MetaData and MetaUnlock states to a file so progress isn't lost when the game closes.
*/