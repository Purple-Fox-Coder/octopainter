using System.Collections.Generic;
using UnityEngine;

struct Drip
{
    public Vector2 uv;
    public Color color;
    public float size, speed, life;
}

// hey I like this feature
[RequireComponent(typeof(Renderer))]
public class CanvasPainter : MonoBehaviour
{
    public int resolution = 2048;
    public Texture2D paintTexture;
    public Material paintMaterial;
    public float dripGravity = 0.12f;

    RenderTexture rt;
    readonly List<Drip> drips = new();
    
    void Start()
    {
        rt = new RenderTexture(resolution, resolution, 0, RenderTextureFormat.ARGB32);
        rt.Create();
        GetComponent<Renderer>().material.mainTexture = rt;
        ClearCanvas();
    }

    public void ClearCanvas()
    {
        // clear render texture
        var prev = RenderTexture.active;
        RenderTexture.active = rt;
        GL.Clear(true, true, Color.white);
        RenderTexture.active = prev;
        // remove active paint drips
        drips.Clear();
    }

    #region UV-related utility functions

    public Vector2 WorldToUV(Vector3 world, out float distInFront)
    {
        var l = transform.InverseTransformPoint(world);
        distInFront = -l.z * transform.lossyScale.z;
        return new Vector2(l.x + 0.5f, l.y + 0.5f);
    }
    public Vector3 Normal => -transform.forward;
    public static bool InBounds(Vector2 uv) => uv.x >= 0 && uv.x <= 1 && uv.y >= 0 && uv.y <= 1;

    #endregion

    #region Paint & splatter

    public void Paint(Vector2 uv, Color c, float sizeUV)
    {
        float s = sizeUV * resolution;
        RenderTexture.active = rt;
        GL.PushMatrix();
        GL.LoadPixelMatrix(0, resolution, 0, resolution); // might need to flip these idk yet
        Graphics.DrawTexture(
            new Rect(uv.x * resolution - s / 2, uv.y * resolution - s / 2, s, s),
            paintTexture, new Rect(0, 0, 1, 1), 0, 0, 0, 0, c, paintMaterial
        );
        GL.PopMatrix();
        RenderTexture.active = null;
    }

    public void Splat(Vector2 uv, Color c, float size)
    {
        // draw main splatter
        Paint(uv, c, size);
        // draw extra smaller splatters around the main one
        int extraSplats = Random.Range(6, 12);
        for (int i = 0; i < extraSplats; i++)
        {
            var offset = Random.insideUnitCircle * size * Random.Range(0.6f, 1.6f);
            Paint(uv + offset, c, size * Random.Range(0.08f, 0.3f));
        }
        // paint dripping effect
        int dripCount = Random.Range(1, 4);
        for (int i = 0; i < dripCount; i++)
        {
            drips.Add(new Drip
            {
                uv = uv + new Vector2(Random.Range(-0.4f, 0.4f) * size, 0),
                color = c,
                size = size * Random.Range(0.06f, 0.12f),
                speed = dripGravity * Random.Range(0.4f, 1f),
                life = Random.Range(0.4f, 1.2f)
            });
        }
    }

    #endregion

    void Update()
    {
        // draw the dripping paint
        // funny trick: loop backwards so when a drip is removed the indexes don't shift around
        // now for my next trick, I make all the bugs DISAPPEAR!!!
        for (int i = drips.Count - 1; i >= 0; i--)
        {
            var d = drips[i];
            d.uv.y -= d.speed * Time.deltaTime;
            d.life -= Time.deltaTime;
            d.size *= 1f - 0.15f * Time.deltaTime;
            d.speed *= 1f - 0.4f * Time.deltaTime;
            if (d.life <= 0 || d.uv.y < 0)
            {
                drips.RemoveAt(i);
                continue;
            }
            Paint(d.uv, d.color, d.size);
            drips[i] = d;
        }
    }
}
