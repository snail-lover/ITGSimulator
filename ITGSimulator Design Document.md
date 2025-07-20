# Introduction

ITG Simulator (ITG SIM) is a sandbox dating simulator set at the school of ITG. You play as a new student navigating a school brimming with hunky nerds, each with their own preferences and sensibilities. The game's objective is to romance one of the available characters. This document outlines the various systems and features intended for the 1.0 release.

# Gameplay

**Point and Click**  
The game is a 3D, top-down, point-and-click game. Use the left mouse button to walk and interact with objects. The middle mouse button allows you to move the camera. Scroll wheel to zoom. (Might add an arrowkey movement and interact key later we'll see (I'm thinking of adding a more satisfying movement system because clicking can get boring (and i don't want the player to feel "uuugh now i have to walk to the other side of the school(so i'm thinking of ways to make it more fun))))

**The Setting**  
The game takes place at a gymnasium called ITG. ITG stands for "IT-Gymnasium," the setting leans into a technology focused environment. However, not all students and teachers are solely focused on technology, and have diverse interests. The school has three different floors and an outside.

**Sandbox Design**  
In this context, "sandbox" does not imply a lack of structure. Instead, players are presented with a variety of established systems they can interact with in any order. Items possess tags, and multiple items can resolve a single issue. The game aims to focus on actions that don't yield definitive true/false outcomes, but rather influence a scale. There are multiple paths to gaining a specific character's affection, and each of those paths has its own multiple ways of being achieved.

Things to facilitate a sandbox nature:

- Items have tags (e.g., "camera," "technology," "liquid") making multiple items viable for a situation without needing hard-coded solutions.
    
- Game objectives should not have a deep dependency on prior steps. If the player has the necessary items or situation, an action should be available.
    
- There is no set order for romancing a character. The player can talk, interact, and explore at their own pace and in their preferred order. They will achieve their goal if their actions consistently contribute to romancing their chosen character.
    

**Interactables**  
The school contains various things the player can interact with. The primary methods of interaction are clicking or dragging objects.

- Example: Doors, light switches, etc. The player might need to drag a couch forward to reach something behind it.
    

**Items**  
Some interactables are items that the player can pick up and place in their inventory. These items can be moved by dragging them with the mouse. Some items have the ability to interact with elements in the world to achieve specific outcomes.

- Example: The player finds a screwdriver, drags it to their inventory. They notice the coffee machine is broken and drag the screwdriver to it, fixing it.
    

**Dialogue**  
NPCs are the notable characters within ITG who move around the school. Not all NPCs are romanceable; romanceable NPCs have special interactions and stories. However, NPCs can still provide players with quest-like objectives. For a more in-depth description of **Quests** and **NPCs**, please refer to the NPC and Quest section. Dialogue is a primary method for players to interact with NPCs and gain context.

*Standard Dialogue* 
Standard dialogue begins with a greeting, followed by presented dialogue options. There are four types of dialogue options:  
1. Quest - Dialogue options related to a current objective the player can complete.  
2. Love - Dialogue options unlocked based on the character's affection level towards the player.  
3. Contextual - Dialogue options available depending on certain game flags or items the player possesses.  
4. General - General dialogue that requires no prerequisites.

*High Priority Dialogue*  
High priority dialogue supersedes standard dialogue and is initiated either by the NPC or when the player starts a conversation. These instances involve things the NPC wants to discuss with the player or special interactions that necessitate this dialogue type.

- Example: The player fixes the coffee machine. An NPC hears this, and the next time they interact, the NPC remarks, "I heard you fixed the coffee machine..."
    

**Quests**  
Some elements can be unlocked by completing quests. Quests are not rigid, step-by-step instructions but rather narrative-wrapped actions that provide narrative context and clues for player actions, ultimately changing the world or rewarding the player.

- Example: Suppose the player needs a key to unlock a shortcut through the fire escape.
    
    1. The player attempts to open the door.
        
    2. They talk to Holm about the door.
        
    3. Holm mentions he used the key in his computer because Windows was asking for one.
        
    4. The player finds Holm's computer and retrieves the key.
        
    5. The player can now open the door.  
	
	If the player had gone to Holm's computer first, they would have found the key and been able to open the door. Quests provide context and clues for actions.
        

**Classes**  
The game features a schedule where classes take place, such as Swedish, programming, math, and physics, with short breaks in between. Not all NPCs attend classes. Only one teacher NPC will be present. NPCs with less academic drive will be mostly absent, perhaps attending only classes they enjoy.  
NPCs should have:  
1. A percentage chance of attending class.  
2. Preferred classes that increase their attendance probability.  
3. Ways to interact with them while they are in class.

# NPCs

**The Current NPC Philosophy**  
This is not the initial design document for ITG SIM. The first version focused on the player increasing affection by selecting correct dialogue options and fetching items for characters. Inspired by the GDC talk "[Kindness Coins, or Chemistry Casino](https://www.google.com/url?sa=E&q=https%3A%2F%2Fwww.youtube.com%2Fwatch%3Fv%3DvlyH_NAs3f0)," I wanted to expand and improve NPC interactions. NPCs are not passive objects to be "won" but should possess distinct desires and agency. NPCs like different things and are different individuals. Through interaction with the world and other NPCs, the player must foster behaviors that make romanceable NPCs like them. The game's AI and systems are intended to support this philosophy, creating NPCs that players feel respond to their actions and have their own objectives.

**Attraction Traits**  
Each NPC possesses traits they are attracted to, such as being technology-savvy, kind, funny, or mischievous. To gain an NPC's interest, the player must cultivate these personality traits through dialogue and world interactions. These traits should exist as opposing poles. For instance, the opposite of "mischievous" could be "security-conscious." If the player "messes with others" in the world, their mischievous trait increases while security decreases. Helping people would have the opposite effect. Different levels and combinations of traits unlock different interactions with various NPCs.

**Needs-Based Behavior**  
NPC actions are guided by their needs, including hunger, energy, and engagement. These needs exist in a dynamic hierarchy, for example; prioritizing engagement over hunger unless hunger is critically low. This system blends basic human needs with "spiritual" needs like engagement and fulfilling goals. While all NPCs share basic needs, their spiritual needs are met in different ways. Zozk's goal might be to achieve good grades, so attending class fulfills some of his needs. Viktor's goal might be to cause trouble, so his needs are met by creating distress.

Example of needs:
- Hunger
- Bladder
- Energy
- Engagement
- Purpose

**Regular Activities**  
NPCs move around and engage in activities to meet their needs, such as using the restroom or eating.

**Cooperative Activities**  
Cooperative Activities are activities NPCs engage in that the player can join, potentially requiring the player to build rapport with the NPC first. For example, the player could join an NPC eating in the cafeteria, or a study session.

**Random Activities**  
Unforeseen events can occur to NPCs, such as being approached by another NPC, their computer breaking, a full police chase takes place in the school and the suspect throws a gun and 407$ of unmarked bills to the NPC and they have to explain to the police what happened. These events may not directly fulfill a need but create opportunities for the player to get involved.

**Awareness of the Player**  
NPCs should react to the player:  
1. NPCs should notice the player walking around, perhaps turning their heads, waving, or saying hello.  
2. They should react when the player interacts with the world.

**NPC to NPC Interaction**  
For NPCs to be believable, they should talk to each other. This likely won't have a direct impact on gameplay.

**Initiating Interaction with the Player**  
NPCs should be able to stop the player, similar to Animal Crossing, to share thoughts or convey something they want. Usually if the player has gotten to know the NPC.

# Questions That Need to Be Answered:

1. What does "interacting with the world" truly entail? The player can drag interactables around, but what are they ultimately trying to create? The goal is to make an NPC like them. How can they achieve this through the environment, and how can NPCs have agency over the player's actions?
    
    - Would it be feasible for the player to create opportunities for NPCs to interact with them? Is this too complex? Can it be simplified?
        
2. I'm not fond of the idea of players seeing explicit attraction trait statistics. However, players will likely discover that Zozk, for instance, likes a certain "design trait." Won't this simply turn into trait farming? Is that necessarily a bad thing?
    
    - How will the player know if they have improved or worsened a relationship or trait?


* Can the player create things in the world, that satisfies the NPC needs?
