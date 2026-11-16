using UnityEngine;

public class PaintPot : MonoBehaviour
{
    public Color color = Color.red;
    public bool isWater;  // dipping in this switches the finger to wipe mode
    void Start()
    {
        if (!isWater)
            GetComponentInChildren<Renderer>().material.color = color;
    }
}