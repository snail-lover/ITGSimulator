using System.Collections.Generic; // Add this at the top

[System.Serializable]
public class DialogueData {
    public string npcID;
    public int startingLove;
    public List<DialogueNode> nodes; // Changed from Dictionary to List
    public Dictionary<string, DialogueNode> nodeDictionary; // Will populate this at runtime
}

[System.Serializable]
public class DialogueNode {
    public string nodeID; // Added identifier field
    public string text;
    public List<DialogueChoice> choices;
    public ItemGate itemGate;
}

[System.Serializable]
public class DialogueChoice {
    public string text;
    public string targetNode;
    public int loveChange;
}

[System.Serializable]
public class ItemGate {
    public int requiredLove;
    public string requiredItem;
    public string successNode;
    public string failNode;
    public LoveChange onSuccess;
}

[System.Serializable]
public class LoveChange {
    public int loveChange;
    public string removeItem;
}