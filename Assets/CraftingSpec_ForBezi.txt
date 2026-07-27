# Brave the Wilderness — ScriptableObject Generation Spec (for Bezi)

## What I need you to do
Create Unity **ScriptableObject asset files** in this project — nothing else. Do **not** create prefabs, scenes, icons, or edit any C# scripts. I will make the prefabs and assign icons myself.

Two asset types to generate:
1. **ItemData** assets (the `ItemData` ScriptableObject already exists in code).
2. **CraftingRecipe** assets (the `CraftingRecipe` ScriptableObject already exists in code).

The C# classes are already written — reuse them, don't recreate them.

## Where the classes live (do not modify these)
- `Assets/_Scripts/MainGame/Inventory/ItemData.cs` — `menuName = "Survival/Item"`
- `Assets/Items/Crafting/CraftingRecipe.cs` — `menuName = "Survival/Crafting Recipe"`

## Folders to create the assets in
Place assets in the existing organized folders — **do not** make a `Generated` catch-all. Match each item/recipe to its category folder (create the folder if it doesn't exist yet):

**ItemData → `Assets/Items/<Category>/`**
- Raw materials, refined materials, and components → `Assets/Items/Resources/`
- Tools → `Assets/Items/Tools/`
- Weapons → `Assets/Items/Weapons/`
- Armor → `Assets/Items/Armor/`
- Consumables → `Assets/Items/Consumables/`

**CraftingRecipe → `Assets/CraftingRecipes/<Category>/`** — mirror the same grouping:
- `Assets/CraftingRecipes/Materials/`
- `Assets/CraftingRecipes/Components/`
- `Assets/CraftingRecipes/Tools/`
- `Assets/CraftingRecipes/Weapons/`
- `Assets/CraftingRecipes/Armor/`
- `Assets/CraftingRecipes/Consumables/`

### Handling the old assets already in these folders
`Assets/Items/` and `Assets/CraftingRecipes/` already contain a few assets from an earlier naming pass (e.g. Rock, RedFlower, BlueFlower, WoodLog, Rope, Iron_Ingot, RoughAxe, RoughPickaxe, RoughHammer, RoughShovel, ReaperScythe, Tier_2_Sword, Tier_3_Sword, Apple, Zombie_Steak, and the recipes NewRecipe/RopeRed/RoughAxe/RoughPickaxe/HealthPotionRecipe). This spec is the canonical source of truth. **Do not delete anything yourself** — instead, list every old asset that overlaps or is now superseded so I can review and remove them manually. Create the new assets from this spec regardless; if a filename would collide, name the new one from this spec and flag the conflict.

---

## ItemData schema (fields to set)
```
string itemId          // set to the snake_case id in the tables below — this is the key everything references
string displayName     // human name
ItemCategory category  // Resource | Food | Tool | Weapon | Armor | Potion | Consumable | Bow
ArmorSlot armorSlot    // None | Helmet | Chest | Shoulders | Gloves | Pants | Boots
int maxStackSize       // 100 for materials/components/consumables; 1 for tools/weapons/armor
bool isConsumable      // true only for Consumables
int healthRestore      // see consumable notes
ToolType toolType      // None | Axe | Pickaxe | Hammer | Shovel | Sickle | Hoe
int tier               // 1..5 (from the tier column)
float effectiveness    // tools only: 1,2,3,4,5 by tier (T1=1 ... T5=5)
ItemStatModifier[] statModifiers   // weapons/armor — see stats columns
```
`ItemStatModifier { StatType statType; float flatAmount; float percentageAmount; }`
- `StatType` = Attack | Defense | MovementSpeed | AttackSpeed | CriticalChance | CriticalDamage
- `percentageAmount` is a fraction: 10% → `0.1`. Use `flatAmount` for flat numbers, `percentageAmount` for `%` stats.
- Leave `icon`, `heldPrefab`, `projectilePrefab`, `armorModelPath`, hold pos/rot at defaults — I'll wire those.

### Schema gaps to be aware of (do NOT invent fields)
The current `ItemData` has **no field** for: Max HP, timed buffs, buff duration, or regen. Several armor/consumable effects below therefore can't be fully stored yet. Where an effect can't map, set what *does* map (e.g. `healthRestore`) and put the rest in the item's `description` text so it's not lost. Flag these back to me; don't add new enum values or fields on your own.

## CraftingRecipe schema (fields to set)
```
string recipeName
RecipeIngredient[] ingredients   // RecipeIngredient { ItemData item; int quantity; }
ItemData outputItem
int outputQuantity               // always 1
bool requiresWorkstation         // always false
string requiredWorkstation       // empty
```

### THE MERGE RULE (critical — read carefully)
Every recipe is a merge of **exactly two items** into one. Represent it like this:
- **Two different inputs** (e.g. Sharp Edge + Handle) → `ingredients` has **two entries**, each `quantity: 1`.
- **Same input twice** (e.g. Branch + Branch) → `ingredients` has **one entry** with `quantity: 2`.

`outputQuantity` is always `1`. `requiresWorkstation` always `false`.

Resolve every `item`/`outputItem` reference by matching the **itemId** in the tables below to the ItemData asset you created. **Create all ItemData assets first, then the recipes** (recipes reference items — items must exist first).

---

## PART 1 — ItemData assets to create

### Raw materials — category `Resource`, maxStackSize 100, no stats
| itemId | displayName | tier |
|---|---|---|
| branch | Branch | 1 |
| stone | Stone | 1 |
| flower | Flower | 1 |
| vine | Vine | 1 |
| zombie_steak | Zombie Steak | 3 |
| zombie_tooth | Zombie Tooth | 3 |
| quartz | Quartz | 3 |
| copper_ore | Copper Ore | 2 |
| iron_ore | Iron Ore | 3 |
| silver_ore | Silver Ore | 4 |
| gold_ore | Gold Ore | 4 |
| moon_ore | Moon Ore | 5 |
| hell_ore | Hell Ore | 5 |
| corrupted_claw | Corrupted Claw | 4 |
| infected_blood | Infected Blood | 4 |
| gem | Gem | 4 |
| dense_bone | Dense Bone | 5 |
| zombie_core | Zombie Core | 5 |
| diamond | Diamond | 5 |

### Refined materials — category `Resource`, maxStackSize 100, no stats
| itemId | displayName | tier |
|---|---|---|
| stick | Stick | 1 |
| wood_log | Wood Log | 2 |
| plank | Plank | 2 |
| flint | Flint | 1 |
| stone_chunk | Stone Chunk | 2 |
| cut_stone | Cut Stone | 2 |
| cord | Cord | 1 |
| rope | Rope | 2 |
| crushed_petals | Crushed Petals | 1 |
| flower_essence | Flower Essence | 3 |
| potent_extract | Potent Extract | 4 |
| copper_ingot | Copper Ingot | 2 |
| iron_ingot | Iron Ingot | 3 |
| silver_ingot | Silver Ingot | 4 |
| gold_ingot | Gold Ingot | 4 |
| moon_ingot | Moon Ingot | 5 |
| hell_ingot | Hell Ingot | 5 |
| cured_hide | Cured Hide | 3 |
| tough_leather | Tough Leather | 3 |
| bone_shard | Bone Shard | 3 |
| quartz_cluster | Quartz Cluster | 4 |
| refined_crystal | Refined Crystal | 4 |
| pure_crystal | Pure Crystal | 4 |
| claw_cluster | Claw Cluster | 4 |
| corrupted_alloy | Corrupted Alloy | 4 |
| bone_plate | Bone Plate | 5 |
| void_shard | Void Shard | 5 |
| voidsteel | Voidsteel | 5 |

### Components — category `Resource`, maxStackSize 100, no stats
| itemId | displayName | tier |
|---|---|---|
| long_shaft | Long Shaft | 1 |
| handle | Handle | 1 |
| sharp_edge | Sharp Edge | 1 |
| heavy_head | Heavy Head | 1 |
| padding | Padding | 1 |
| bark_plate | Bark Plate | 1 |

### Tools — category `Tool`, maxStackSize 1, set toolType, tier, effectiveness (=tier)
| itemId | displayName | toolType | tier |
|---|---|---|---|
| stone_axe | Stone Axe | Axe | 1 |
| reinforced_axe | Reinforced Axe | Axe | 2 |
| iron_axe | Iron Axe | Axe | 3 |
| crystal_axe | Crystal Axe | Axe | 4 |
| void_axe | Void Axe | Axe | 5 |
| stone_pickaxe | Stone Pickaxe | Pickaxe | 1 |
| reinforced_pickaxe | Reinforced Pickaxe | Pickaxe | 2 |
| iron_pickaxe | Iron Pickaxe | Pickaxe | 3 |
| crystal_pickaxe | Crystal Pickaxe | Pickaxe | 4 |
| void_pickaxe | Void Pickaxe | Pickaxe | 5 |

### Weapons — category `Weapon`, maxStackSize 1, set tier + statModifiers
Stat mapping: Atk → `{Attack, flat}`; Crit% → `{CriticalChance, pct}`; Crit dmg% → `{CriticalDamage, pct}`.
| itemId | displayName | tier | statModifiers |
|---|---|---|---|
| flint_dagger | Flint Dagger | 1 | Attack flat 8; CriticalChance pct 0.08; CriticalDamage pct 0.50 |
| serrated_knife | Serrated Knife | 2 | Attack flat 30; CriticalChance pct 0.10; CriticalDamage pct 0.50 |
| iron_dagger | Iron Dagger | 3 | Attack flat 110; CriticalChance pct 0.13; CriticalDamage pct 0.60 |
| crystal_fang | Crystal Fang | 4 | Attack flat 380; CriticalChance pct 0.18; CriticalDamage pct 0.75 |
| void_reaper | Void Reaper | 5 | Attack flat 1100; CriticalChance pct 0.25; CriticalDamage pct 1.00 |
| lunar_reaper | Lunar Reaper | 5 | Attack flat 1900; CriticalChance pct 0.35; CriticalDamage pct 1.50 |
| wooden_club | Wooden Club | 1 | Attack flat 18 |
| reinforced_maul | Reinforced Maul | 2 | Attack flat 70 |
| iron_mace | Iron Mace | 3 | Attack flat 250 |
| crystal_crusher | Crystal Crusher | 4 | Attack flat 850 |
| void_breaker | Void Breaker | 5 | Attack flat 2500 |
| lunar_breaker | Lunar Breaker | 5 | Attack flat 4300 |
| flint_spear | Flint Spear | 1 | Attack flat 12 |
| reinforced_pike | Reinforced Pike | 2 | Attack flat 45 |
| iron_halberd | Iron Halberd | 3 | Attack flat 165 |
| crystal_lance | Crystal Lance | 4 | Attack flat 560 |
| void_impaler | Void Impaler | 5 | Attack flat 1600 |
| lunar_impaler | Lunar Impaler | 5 | Attack flat 2800 |

### Armor — category `Armor`, maxStackSize 1, set armorSlot + tier + statModifiers
Note: `+% Max HP` has **no matching StatType** — store the rest and note "+X% Max HP" in `description`.
| itemId | displayName | armorSlot | tier | statModifiers |
|---|---|---|---|---|
| bark_boots | Bark Boots | Boots | 1 | Defense flat 3 |
| reinforced_boots | Reinforced Boots | Boots | 2 | Defense flat 6 |
| iron_boots | Iron Boots | Boots | 3 | Defense flat 10; Attack flat 3 |
| crystal_boots | Crystal Boots | Boots | 4 | Defense flat 15; Attack pct 0.05 |
| void_boots | Void Boots | Boots | 5 | Defense pct 0.08 (+6% Max HP → description) |
| bark_pants | Bark Pants | Pants | 1 | Defense flat 3 |
| reinforced_pants | Reinforced Pants | Pants | 2 | Defense flat 6 |
| iron_pants | Iron Pants | Pants | 3 | Defense flat 10; Attack flat 3 |
| crystal_pants | Crystal Pants | Pants | 4 | Defense flat 15; Attack pct 0.05 |
| void_pants | Void Pants | Pants | 5 | Defense pct 0.08 (+6% Max HP → description) |
| bark_helmet | Bark Helmet | Helmet | 1 | Defense flat 3 |
| reinforced_helmet | Reinforced Helmet | Helmet | 2 | Defense flat 6 |
| iron_helmet | Iron Helmet | Helmet | 3 | Defense flat 10; Attack flat 3 |
| crystal_helmet | Crystal Helmet | Helmet | 4 | Defense flat 15; Attack pct 0.05 |
| void_helmet | Void Helmet | Helmet | 5 | Defense pct 0.08 (+6% Max HP → description) |
| bark_chest | Bark Chest | Chest | 1 | Defense flat 3 |
| reinforced_chest | Reinforced Chest | Chest | 2 | Defense flat 6 |
| iron_chest | Iron Chest | Chest | 3 | Defense flat 10; Attack flat 3 |
| crystal_chest | Crystal Chest | Chest | 4 | Defense flat 15; Attack pct 0.05 |
| void_chest | Void Chest | Chest | 5 | Defense pct 0.08 (+6% Max HP → description) |
| reinforced_gloves | Reinforced Gloves | Gloves | 2 | Defense flat 5; Attack flat 5 |
| iron_gloves | Iron Gloves | Gloves | 3 | Attack flat 8; MovementSpeed pct 0.04 |
| crystal_gloves | Crystal Gloves | Gloves | 4 | Attack pct 0.08; MovementSpeed pct 0.06 |
| void_gloves | Void Gloves | Gloves | 5 | Attack pct 0.06; Defense pct 0.06; MovementSpeed pct 0.06 (+6% ALL — chase item) |
| reinforced_shoulders | Reinforced Shoulders | Shoulders | 2 | Defense flat 5; Attack flat 5 |
| iron_shoulders | Iron Shoulders | Shoulders | 3 | Attack flat 8; MovementSpeed pct 0.04 |
| crystal_shoulders | Crystal Shoulders | Shoulders | 4 | Attack pct 0.08; MovementSpeed pct 0.06 |
| void_shoulders | Void Shoulders | Shoulders | 5 | Attack pct 0.06; Defense pct 0.06; MovementSpeed pct 0.06 (+6% ALL — hardest item) |

### Consumables — category `Consumable`, maxStackSize 100, isConsumable true
Only `healthRestore` maps cleanly; timed buffs/traps have no data field yet — put the full effect in `description`.
| itemId | displayName | tier | healthRestore | description (effect) |
|---|---|---|---|---|
| herbal_poultice | Herbal Poultice | 1 | 15 | +15 HP instant |
| stoneskin_brew | Stoneskin Brew | 2 | 0 | +20 Defense for 20s |
| cooked_steak | Cooked Steak | 3 | 40 | +40 HP + regen 8s |
| bone_caltrops | Bone Caltrops | 3 | 0 | Deployable trap, area damage |
| adrenaline_shot | Adrenaline Shot | 4 | 0 | +15% Move & Attack Speed for 12s |
| rage_draught | Rage Draught | 4 | 0 | +25% Attack for 15s |
| fortune_draught | Fortune Draught | 4 | 0 | +30% survival points earned for 25s |
| void_elixir | Void Elixir | 5 | 0 | +15% ALL Stats for 20s |

---

## PART 2 — CraftingRecipe assets to create (98 total)
Format: **Output = Input A + Input B**. Apply the merge rule (same+same → one ingredient qty 2; different → two ingredients qty 1). `recipeName` = output displayName. All `outputQuantity: 1`, `requiresWorkstation: false`.

### Materials (28)
- stick = branch + branch
- wood_log = stick + stick
- plank = wood_log + wood_log
- flint = stone + stone
- stone_chunk = flint + flint
- cut_stone = stone_chunk + stone_chunk
- cord = vine + vine
- rope = cord + cord
- crushed_petals = flower + flower
- flower_essence = crushed_petals + crushed_petals
- potent_extract = flower_essence + flower_essence
- copper_ingot = copper_ore + copper_ore
- iron_ingot = iron_ore + iron_ore
- silver_ingot = silver_ore + silver_ore
- gold_ingot = gold_ore + gold_ore
- moon_ingot = moon_ore + moon_ore
- hell_ingot = hell_ore + hell_ore
- cured_hide = zombie_steak + zombie_steak
- tough_leather = cured_hide + cured_hide
- bone_shard = zombie_tooth + zombie_tooth
- quartz_cluster = quartz + quartz
- refined_crystal = quartz_cluster + gem
- pure_crystal = refined_crystal + refined_crystal
- claw_cluster = corrupted_claw + corrupted_claw
- corrupted_alloy = corrupted_claw + infected_blood
- bone_plate = dense_bone + dense_bone
- void_shard = zombie_core + diamond
- voidsteel = void_shard + bone_plate

### Components (6)
- long_shaft = stick + stick
- handle = stick + cord
- sharp_edge = flint + stone
- heavy_head = flint + flint
- padding = cord + cord
- bark_plate = stick + cord

### Tools (10)
- stone_axe = sharp_edge + handle
- reinforced_axe = stone_axe + stone_chunk
- iron_axe = reinforced_axe + iron_ingot
- crystal_axe = iron_axe + refined_crystal
- void_axe = crystal_axe + voidsteel
- stone_pickaxe = heavy_head + long_shaft
- reinforced_pickaxe = stone_pickaxe + stone_chunk
- iron_pickaxe = reinforced_pickaxe + iron_ingot
- crystal_pickaxe = iron_pickaxe + refined_crystal
- void_pickaxe = crystal_pickaxe + voidsteel

### Weapons (18)
- flint_dagger = sharp_edge + stick
- serrated_knife = flint_dagger + copper_ingot
- iron_dagger = serrated_knife + iron_ingot
- crystal_fang = iron_dagger + silver_ingot
- void_reaper = crystal_fang + hell_ingot
- lunar_reaper = void_reaper + moon_ingot
- wooden_club = heavy_head + handle
- reinforced_maul = wooden_club + copper_ingot
- iron_mace = reinforced_maul + iron_ingot
- crystal_crusher = iron_mace + silver_ingot
- void_breaker = crystal_crusher + hell_ingot
- lunar_breaker = void_breaker + moon_ingot
- flint_spear = sharp_edge + long_shaft
- reinforced_pike = flint_spear + copper_ingot
- iron_halberd = reinforced_pike + iron_ingot
- crystal_lance = iron_halberd + silver_ingot
- void_impaler = crystal_lance + hell_ingot
- lunar_impaler = void_impaler + moon_ingot

### Armor (28)
- bark_boots = padding + cord
- reinforced_boots = bark_boots + stone_chunk
- iron_boots = reinforced_boots + iron_ingot
- crystal_boots = iron_boots + refined_crystal
- void_boots = crystal_boots + voidsteel
- bark_pants = padding + bark_plate
- reinforced_pants = bark_pants + stone_chunk
- iron_pants = reinforced_pants + iron_ingot
- crystal_pants = iron_pants + refined_crystal
- void_pants = crystal_pants + voidsteel
- bark_helmet = bark_plate + cord
- reinforced_helmet = bark_helmet + stone_chunk
- iron_helmet = reinforced_helmet + iron_ingot
- crystal_helmet = iron_helmet + refined_crystal
- void_helmet = crystal_helmet + voidsteel
- bark_chest = bark_plate + bark_plate
- reinforced_chest = bark_chest + stone_chunk
- iron_chest = reinforced_chest + iron_ingot
- crystal_chest = iron_chest + refined_crystal
- void_chest = crystal_chest + voidsteel
- reinforced_gloves = padding + cured_hide
- iron_gloves = reinforced_gloves + iron_ingot
- crystal_gloves = iron_gloves + refined_crystal
- void_gloves = crystal_gloves + voidsteel
- reinforced_shoulders = bark_plate + cured_hide
- iron_shoulders = reinforced_shoulders + iron_ingot
- crystal_shoulders = iron_shoulders + refined_crystal
- void_shoulders = crystal_shoulders + voidsteel

### Consumables (8)
- herbal_poultice = crushed_petals + cord
- stoneskin_brew = cut_stone + flower_essence
- cooked_steak = zombie_steak + flint
- bone_caltrops = bone_shard + stick
- adrenaline_shot = infected_blood + cord
- rage_draught = corrupted_alloy + flower_essence
- fortune_draught = gold_ingot + flower_essence
- void_elixir = void_shard + potent_extract

---

## Final checklist for you (Bezi)
1. Create all ItemData assets first (117 of them), itemId set exactly as in Part 1.
2. Then create all CraftingRecipe assets (98), resolving inputs/outputs by itemId.
3. Apply the merge rule for same-item recipes (one ingredient, quantity 2).
4. Put each asset in its category folder under `Assets/Items/<Category>/` and `Assets/CraftingRecipes/<Category>/` — no `Generated` folder.
5. Do not delete old assets; list the outdated/superseded ones (and any filename conflicts) for me to clean up manually.
6. Report back any effect I flagged as "no matching field" (Max HP, timed buffs, regen, traps) so I can decide whether to extend ItemData later.
