using NUnit.Framework;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class HolmBehaviur : BaseInteract
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    // Update is called once per frame
    void Update()
    {
        List<Vector3> playerlocs = new List<Vector3>();

        // detect all players and path somewhat close to them
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        foreach (var item in players)
        {
            // add item to playerlocs
            playerlocs.Add(item.transform.localPosition);
        }


        // now make Holm go close to the player


        var holm = GameObject.FindGameObjectWithTag("Holm");

        if (holm.IsUnityNull()) return;

        

        if (Vector3.Distance(holm.transform.position, players[0].transform.position) > 10)
        {
            float step = 1 * Time.deltaTime; // Adjust speed value as necessary

            // Move Holm closer to the nearest player
            holm.transform.position = Vector3.MoveTowards(holm.transform.position, players[0].transform.position, step);
        
        }
        else
        {
            // send a msgbox but only with a 1 in 100 chance!
        }



    }
}
