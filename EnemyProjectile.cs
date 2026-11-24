using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Roma
{
    public class EnemyProjectile
    {
        public Vector2 Position;
        public Vector2 Velocity;
        public float Life = 3f;
        public float Rotation;

        public Rectangle Bounds => new Rectangle((int)Position.X - 8, (int)Position.Y - 8, 16, 16);

        public EnemyProjectile(Vector2 pos, Vector2 vel)
        {
            Position = pos;
            Velocity = vel;
            Rotation = (float)Math.Atan2(vel.Y, vel.X);
        }

        public void Update(float dt)
        {
            Position += Velocity * dt;
            Life -= dt;
        }

        public bool IsAlive => Life > 0f;

        public void Draw(SpriteBatch sb, Texture2D tex, float depth)
        {
            Vector2 origin = new Vector2(tex.Width / 2f, tex.Height / 2f);
            // Reduced scale from 0.6f to 0.35f
            sb.Draw(tex, Position, null, Color.White, Rotation, origin, 0.35f, SpriteEffects.None, depth);
        }
    }
}