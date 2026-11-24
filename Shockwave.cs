using System; // ADDED: This was missing
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Roma
{
    public class Shockwave
    {
        public Vector2 Position;
        public float Radius = 0f;
        public float MaxRadius = 150f;
        public float Speed = 400f;
        public float Life = 0.3f;
        public float MaxLife = 0.3f;
        public int Damage = 1;
        public bool HasHit = false;

        public void Update(float dt)
        {
            Radius += Speed * dt;
            Life -= dt;
        }

        public bool IsAlive => Life > 0 && Radius < MaxRadius;

        public void Draw(SpriteBatch sb, Texture2D pixelTex, float depth)
        {
            float alpha = Life / MaxLife;
            int segments = 32;
            float angleStep = MathHelper.TwoPi / segments;

            for (int i = 0; i < segments; i++)
            {
                float angle = i * angleStep;
                Vector2 pos = Position + new Vector2(
                    (float)Math.Cos(angle) * Radius,
                    (float)Math.Sin(angle) * Radius
                );
                sb.Draw(pixelTex, pos, null, Color.Cyan * alpha * 0.5f, 0f, Vector2.Zero, 3f, SpriteEffects.None, depth);
            }
        }
    }
}