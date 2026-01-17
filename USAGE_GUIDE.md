# Chunk System Usage Guide

## Overview

The new chunk system combines **Road**, **Chunk** (obstacles & collectibles), and **Decoration** into a single composite that spawns together. All three components are managed through pools, with chunks and decorations being randomly selected.

## System Architecture

### Components

1. **AbstractPool<T>** - Base class for all pools
2. **ChunkPool** - Manages WorldChunk objects (obstacles & collectibles)
3. **DecorationPool** - Manages Decoration objects
4. **RoadPool** - Manages Road objects (can be unique or pooled)
5. **WorldChunkComposite** - Combines Road + Chunk + Decoration together
6. **ChunkLayoutSO** - ScriptableObject that defines which prefabs to use

## Setup Instructions

### Step 1: Create Prefabs

You need to create three types of prefabs:

#### Road Prefab
1. Create a GameObject for your road segment
2. Add the `Road` component
3. Set the `Road Length` field (e.g., 20f)
4. Add your road mesh/model as a child
5. Save as a prefab (e.g., `Road_Standard.prefab`)

#### Chunk Prefab (Obstacles & Collectibles)
1. Use your existing chunk prefabs (they should already have `WorldChunk` component)
2. These contain obstacles, collectibles, lanes, etc.
3. Make sure they have the `WorldChunk` component attached
4. **Important**: Set the `Difficulty` field on each chunk prefab (Easy, Medium, Hard, Extreme)
   - Each chunk prefab has its own difficulty level
   - This allows mixing different difficulty chunks in the same layout

#### Decoration Prefab
1. Create a GameObject for your decoration
2. Add the `Decoration` component
3. Set the `Decoration Length` field (should match your chunk/road length)
4. Add your decoration meshes/models as children
5. Save as a prefab (e.g., `Decoration_Trees.prefab`, `Decoration_Buildings.prefab`)

### Step 2: Create ChunkLayoutSO ScriptableObjects

1. Right-click in Project window → `Create → World → Chunk Layout`
2. Name it (e.g., `MixedChunkLayout`)
3. Configure the ScriptableObject:
   - **Lanes**: Number of lanes (usually 3)
   - **Road Prefab**: Assign your road prefab (can be the same for all layouts, or different)
   - **Chunk Prefabs**: Add a list of chunk prefabs (obstacles & collectibles) - these will be randomly selected
     - **Note**: Each chunk prefab should have its difficulty set in the prefab itself (on the WorldChunk component)
     - You can mix different difficulty chunks in the same layout
   - **Decoration Prefabs**: Add a list of decoration prefabs - these will be randomly selected

**Example Configuration:**
```
MixedChunkLayout:
  - Road Prefab: Road_Standard
  - Chunk Prefabs: 
      - EasyChunk1 (Difficulty: Easy - set on prefab)
      - EasyChunk2 (Difficulty: Easy - set on prefab)
      - MediumChunk1 (Difficulty: Medium - set on prefab)
  - Decoration Prefabs: [Decoration_Trees, Decoration_Bushes]

HardChunkLayout:
  - Road Prefab: Road_Standard (same road)
  - Chunk Prefabs: 
      - HardChunk1 (Difficulty: Hard - set on prefab)
      - HardChunk2 (Difficulty: Hard - set on prefab)
  - Decoration Prefabs: [Decoration_Buildings, Decoration_Trees]
```

**Important**: Difficulty is now set **per chunk prefab**, not per layout. This allows you to:
- Mix different difficulty chunks in the same layout
- Have more granular control over difficulty progression
- Access chunk difficulty via `WorldChunk.Difficulty` or `WorldChunkComposite.Difficulty`

### Step 3: Setup Scene Components

1. Find your **WorldManager** GameObject (or create one)
2. Add the following components to the same GameObject:
   - `ChunkSpawner` (already exists)
   - `ChunkPool` (already exists)
   - `DecorationPool` (NEW - add this)
   - `RoadPool` (NEW - add this)

3. Configure **ChunkSpawner**:
   - **Chunk Layouts**: Add your ChunkLayoutSO ScriptableObjects to the list
   - **Default Chunk Layout**: Assign a fallback layout
   - Other settings (spawn distance, chunks to keep ahead, etc.) remain the same

4. Configure Pool Settings (on each pool component):
   - **Initial Pool Size**: 5 (default)
   - **Max Pool Size**: 50 (default)
   - Adjust as needed based on your game's requirements

### Step 4: Verify Component Setup

Make sure your WorldManager GameObject has:
- ✅ `WorldMover` component
- ✅ `ChunkSpawner` component
- ✅ `ChunkPool` component
- ✅ `DecorationPool` component (NEW)
- ✅ `RoadPool` component (NEW)

## How It Works

### Spawning Logic

1. **ChunkSpawner** randomly selects a `ChunkLayoutSO` from the list
2. From that layout, it:
   - Gets the **road prefab** (same road, or from pool if multiple)
   - Randomly selects a **chunk prefab** from `chunkPrefabs` list
   - Randomly selects a **decoration prefab** from `decorationPrefabs` list
3. Gets instances from the respective pools:
   - `RoadPool.GetRoad(roadPrefab)`
   - `ChunkPool.GetChunk(chunkPrefab)`
   - `DecorationPool.GetDecoration(decorationPrefab)`
4. Creates a `WorldChunkComposite` that combines all three
5. Initializes and spawns the composite

### Despawning Logic

When a composite passes the player:
1. Components are returned to their respective pools
2. Composite GameObject is destroyed
3. Components are reset and ready for reuse

## Important Notes

### Road Behavior
- Road can be **unique** (same prefab always) or **pooled** (multiple road variants)
- If you want the same road every time, use the same prefab in all layouts
- If you want road variety, add multiple road prefabs to a pool (though they won't be randomly selected - you'd need to modify the spawner logic)

### Chunk & Decoration Randomization
- Chunks are **always randomly selected** from the `chunkPrefabs` list
- Decorations are **always randomly selected** from the `decorationPrefabs` list
- Each spawn gets a random combination

### Length Matching
- Make sure Road Length, Chunk Length, and Decoration Length are similar
- The composite uses the **maximum length** for spawn/despawn calculations
- Components are positioned at the same Z position (stacked visually)

### Legacy Support
- Old `chunkPrefab` field is still supported but marked as obsolete
- It will automatically add to the `chunkPrefabs` list
- Migrate to using `chunkPrefabs` list for better control

## Troubleshooting

### Components Not Spawning
- Check that all three pools are attached to the same GameObject as ChunkSpawner
- Verify ChunkLayoutSO has prefabs assigned
- Check console for error messages

### Double Movement / Positioning Issues
- Components are children of the composite, so they move with it
- Don't manually move individual components
- The composite handles all movement

### Pool Exhaustion
- Increase `Max Pool Size` if you see warnings
- Or reduce the number of active chunks (`chunksToKeepAhead`)

## Example Workflow

1. **Create 1 Road Prefab**: `Road_Standard.prefab` (length: 20)
2. **Create 3 Chunk Prefabs**: `EasyChunk1`, `EasyChunk2`, `EasyChunk3`
3. **Create 2 Decoration Prefabs**: `Trees`, `Buildings`
4. **Create ChunkLayoutSO**: 
   - Road: `Road_Standard`
   - Chunks: `[EasyChunk1, EasyChunk2, EasyChunk3]`
   - Decorations: `[Trees, Buildings]`
5. **Add to ChunkSpawner**: Add the ChunkLayoutSO to the list
6. **Play**: System will spawn Road + Random Chunk + Random Decoration together!

## Advanced: Multiple Road Types

If you want road variety (though not randomly selected):
1. Create multiple road prefabs
2. Use different roads in different ChunkLayoutSOs
3. The road will be selected based on which layout is chosen
4. Roads are still pooled, so they'll be reused efficiently
