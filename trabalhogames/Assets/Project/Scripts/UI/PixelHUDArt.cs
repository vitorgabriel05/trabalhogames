using System.Collections.Generic;
using UnityEngine;

// Small, original pixel drawings. Kept separate from the height/record rules.
public class PixelHUDArt : MonoBehaviour
{
    private readonly List<Sprite> sprites = new List<Sprite>();
    private static Color32 Color(string hex) { ColorUtility.TryParseHtmlString("#" + hex, out Color color); return color; }

    public Sprite Frame(int width, int height, bool gold)
    {
        var pixels = new Color32[width * height];
        string[] colors = gold
            ? new[] { "111E32", "754717", "FFDB64", "BF7A25", "FFE793", "F9BC39" }
            : new[] { "071222", "37649A", "79C6FF", "0A203B", "356AA0", "101E35" };
        for (int inset = 0; inset < colors.Length; inset++)
        {
            int cut = Mathf.Max(1, 5 - inset);
            for (int y = inset; y < height - inset; y++)
            for (int x = inset; x < width - inset; x++)
            {
                int dx = Mathf.Min(x - inset, width - inset - 1 - x);
                int dy = Mathf.Min(y - inset, height - inset - 1 - y);
                if (dx + dy >= cut) pixels[y * width + x] = Color(colors[inset]);
            }
        }
        // Highlights, lower bevel, and the four little corner rivets.
        for (int x = 8; x < width - 8; x++)
        {
            pixels[(height - 7) * width + x] = Color(gold ? "FFE485" : "182F4E");
            pixels[6 * width + x] = Color(gold ? "E89F27" : "0A172B");
        }
        foreach (int x in new[] { 5, width - 6 })
        foreach (int y in new[] { 5, height - 6 })
        {
            pixels[y * width + x] = Color(gold ? "FFF0A0" : "9DDAFF");
            pixels[(y - 1) * width + x] = Color(gold ? "9C5F1C" : "285888");
        }
        return Make(width, height, pixels);
    }

    public Sprite Trophy()
    {
        string[] rows = {
            ".....................",
            ".....ddddddddddd.....",
            "....dHHHHHyyyysd.....",
            "..ddddHHHHyyyysdddd..",
            ".dyydHHHHyyyyysdysd.",
            ".dH.dHHHHyyyyysd.sd.",
            ".dy.dHHHyyyyyss.dsd.",
            "..dydHHHyyyyyssdsd..",
            "...dddHyyyyyssddd...",
            ".....ddyyyyssdd.....",
            "......ddyyssdd......",
            ".......ddssdd.......",
            "........dysd........",
            "........dysd........",
            ".......ddysdd.......",
            "......dHyyyssd......",
            ".....dddddddddd.....",
            ".....dHyyyyyssd.....",
            ".....dddddddddd....."
        };
        var pixels = new Color32[21 * rows.Length];
        for (int y = 0; y < rows.Length; y++)
        for (int x = 0; x < rows[y].Length; x++)
        {
            char c = rows[y][x];
            if (c == '.') continue;
            pixels[(rows.Length - 1 - y) * 21 + x] = Color(c == 'H' ? "FFF3A8" : c == 'y' ? "FFC637" : c == 's' ? "C77B19" : "57391D");
        }
        return Make(21, rows.Length, pixels);
    }

    public Sprite Sparkle()
    {
        const int size = 13;
        var pixels = new Color32[size * size];
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            int dx = Mathf.Abs(x - 6), dy = Mathf.Abs(y - 6);
            if ((dx <= 1 && dy <= 5) || (dy <= 1 && dx <= 4) || dx + dy <= 3)
                pixels[y * size + x] = Color(dx + dy <= 2 ? "FFFFEA" : "FFD04C");
        }
        return Make(size, size, pixels);
    }

    private Sprite Make(int width, int height, Color32[] pixels)
    {
        var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.SetPixels32(pixels);
        texture.Apply();
        var sprite = Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 1);
        sprites.Add(sprite);
        return sprite;
    }

    private void OnDestroy()
    {
        foreach (var sprite in sprites)
        {
            Destroy(sprite.texture);
            Destroy(sprite);
        }
    }
}
