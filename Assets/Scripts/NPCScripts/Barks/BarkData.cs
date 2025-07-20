using System.Collections.Generic;

// These classes are designed to directly match the structure of your barks.json file.
// The [System.Serializable] attribute is crucial for Unity's JsonUtility to parse them.

[System.Serializable]
public class Bark
{
    public string id;
    public string type;
    public string text;
}

[System.Serializable]
public class BarkCollection
{
    public List<Bark> barks;
}