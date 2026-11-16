using Unity.Mathematics;
using Unity.VectorGraphics;
using UnityEngine;

// I split this script off of the CanvasPainter script because I wanted to add as many colliders and interactors
// as I wanted, and I could just throw this script on like 20 different tentacles or fingers or whatever and it
// would Just Work

public class CanvasInteractor : MonoBehaviour
{
    public CanvasPainter canvas;
    public Renderer tipRenderer; // shows the selected color on the fingertip/tentacle-tip (is that even a word?)
    public float touchDist = 0.04f; // distance from canvas surface
    public float strokeSize = 0.02f;

    Color color;
    bool wipe;
    float load;
    Vector2 lastUV;
    bool hadLast;

    public void SetPaint(Color c, bool isWipe = false)
    {
        color = c;
        wipe = isWipe;
        load = 1f;
        if (tipRenderer) tipRenderer.material.color = c;
    }

    void Update()
    {
        if (load <= 0f)
        {
            hadLast = false;
            return;
        }

        Vector2 uv = canvas.WorldToUV(transform.position, out float dist);
        bool touching = 
            Mathf.Abs(dist) < touchDist && // obvious
            CanvasPainter.InBounds(uv); // weird uv out-of-range issues
        if (!touching)
        {
            hadLast = false;
            return;
        }

        float size = wipe ? strokeSize * 4f : strokeSize;
        Color c = wipe ? Color.white : color;

        if (hadLast)
        {
            float len = Vector2.Distance(lastUV, uv);
            int steps = Mathf.Max(1, Mathf.CeilToInt(len / (size * 0.3f)));
            for (int i = 0; i < steps; i++)
                canvas.Splat(Vector2.Lerp(lastUV, uv, i / (float)steps), c, size);
        }
        else
            canvas.Splat(uv, c, size);

        lastUV = uv;
        hadLast = true;
        load -= (wipe ? 0.05f : 0.03f) * Time.deltaTime;

        if (load <= 0f && tipRenderer) tipRenderer.material.color = Color.white;
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent<PaintPot>(out var pot))
            SetPaint(pot.color, pot.isWater);
    }
}
