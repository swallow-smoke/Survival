# Blueprint JSON

Crafting data is loaded from `Assets/003_Resources/Data/Blueprints.json`.
`BluePrints.asset` only holds the reference to that JSON file; do not add recipes to the asset inspector.

```json
{
  "blueprints": [
    {
      "resultCraft": 2,
      "recipe": [
        { "item": 0, "count": 2 }
      ],
      "craftTime": 1.5,
      "requiredLevel": 0,
      "isUnlocked": true,
      "categoryPath": "Materials/Metal/Iron",
      "bluePrintName": "Simple Oxygen Filter",
      "bluePrintId": 0
    }
  ]
}
```

- `resultCraft` and each recipe `item` are item IDs from `ItemDataBase`.
- `categoryPath` accepts any slash-separated depth. Empty paths become `Misc`.
- Blueprint IDs and names must be unique. Recipe counts must be greater than zero.
- Saving the JSON and returning to Unity reloads the database on the next asset/domain load.

## First-person item view contract

Assign a view prefab to `Item.firstPersonPrefab`. The holder spawns it under its `mount`.
An optional Animator may expose `Equip` and `Use` trigger parameters. Optional custom behaviour can implement
`IHeldItemAction` for equip and primary-action callbacks.
