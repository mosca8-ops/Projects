using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace TXT.WEAVR.Player
{
    public class SpriteCache
    {
        private static SpriteCache s_instance;
        public static SpriteCache Instance
        {
            get
            {
                if(s_instance == null)
                {
                    s_instance = new SpriteCache();
                }
                return s_instance;
            }
        }

        private Dictionary<Texture2D, Sprite> m_sprites = new Dictionary<Texture2D, Sprite>();

        public Sprite Get(Texture2D texture)
        {
            if (!texture)
            {
                return null;
            }
            if(!m_sprites.TryGetValue(texture, out Sprite sprite))
            {
                sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
                m_sprites[texture] = sprite;
            }
            return sprite;
        }
    }
}
