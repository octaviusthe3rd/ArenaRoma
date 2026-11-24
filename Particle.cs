using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Roma
{
    public class Particle
    {
        public Vector2 Position;
        public Vector2 Velocity;
        public float Life;
        public float MaxLife;
        public float Scale;
        public Color Tint;
        public bool UseExplosionTexture = false;
        public bool ScaleOverTime = false; // NEW

        public Particle(Vector2 pos, Vector2 vel, float life, float scale, Color tint)
        {
            Position = pos;
            Velocity = vel;
            Life = life;
            MaxLife = life;
            Scale = scale;
            Tint = tint;
        }

        public void Update(float dt)
        {
            Position += Velocity * dt;
            Velocity *= 0.92f;
            Life -= dt;
        }

        public void Draw(SpriteBatch sb, Texture2D tex, float depth)
        {
            float alpha = Life / MaxLife;
            Color drawColor = Tint * alpha;
            Vector2 origin = new Vector2(tex.Width / 2f, tex.Height / 2f);
            sb.Draw(tex, Position, null, drawColor, 0f, origin, Scale, SpriteEffects.None, depth);
        }

        public bool IsAlive => Life > 0;
    }
}