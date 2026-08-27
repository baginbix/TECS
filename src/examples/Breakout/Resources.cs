using System.Collections.Generic;
using Microsoft.Xna.Framework.Graphics;
using TECS;
using TECS.Resources;

namespace Breakout;

public class Assets:IResource
{
    private Dictionary<string, Texture2D>  textures = new();

    public void RegisterTexture(string name, Texture2D texture2D)
    {
        textures.Add(name, texture2D);
    }
    
    public Texture2D GetTexture(string name)
    {
        return textures[name];
    }
}