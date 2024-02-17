using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class PathTextUpdate : MonoBehaviour
{
    public TextMeshProUGUI tx;
    void Update()
    {
        tx.text = "Path: " + ButtonHandler.midiPath;
    }
}
