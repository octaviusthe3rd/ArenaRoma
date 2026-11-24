using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Roma
{
    public class FloatingText
    {
        public Vector2 Position;
        public Vector2 Velocity;
        public string Text;
        public Color Color;
        public float Life = 0.8f;
        public float MaxLife = 0.8f;
        public float Scale = 2.0f; // Increased from 1.5f

        public FloatingText(Vector2 pos, string text, Color color)
        {
            Position = pos;
            Text = text;
            Color = color;
            Velocity = new Vector2(0, -100f); // Faster float
        }

        public void Update(float dt)
        {
            Position += Velocity * dt;
            Velocity *= 0.95f;
            Life -= dt;
        }

        public bool IsAlive => Life > 0;

        public void Draw(SpriteBatch sb, SpriteFont font, float depth)
        {
            float alpha = Life / MaxLife;
            Vector2 origin = font.MeasureString(Text) / 2f;
            sb.DrawString(font, Text, Position, Color * alpha, 0f, origin, Scale, SpriteEffects.None, depth);
        }
    }
}