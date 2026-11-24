using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Roma
{
    public class Pickup
    {
        public Vector2 Position;
        public float Life = 10f;
        public int HealAmount = 25;
        public float BobTime = 0f;

        public Rectangle Bounds => new Rectangle((int)Position.X - 12, (int)Position.Y - 24, 24, 24);

        public void Update(float dt)
        {
            Life -= dt;
            BobTime += dt;
        }

        public bool IsAlive => Life > 0f;

        public void Draw(SpriteBatch sb, Texture2D tex, float depth)
        {
            float bob = (float)Math.Sin(BobTime * 4f) * 3f;
            Vector2 origin = new Vector2(tex.Width / 2f, tex.Height);

            float alpha = Life < 2f ? (Life / 2f) : 1f;
            // Reduced scale from 0.5f to 0.3f
            sb.Draw(tex, Position + new Vector2(0, bob), null, Color.White * alpha, 0f, origin, 0.3f, SpriteEffects.None, depth);
        }
    }
}