using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Roma
{
    public class Player
    {
        public Vector2 Position;
        public Texture2D Texture;
        public float Scale;
        public float Speed = 260f;
        public bool FacingLeft;

        public int Health = 100;
        public int MaxHealth = 100;
        public int XP = 0;
        public int Level = 1;
        public int XPToNextLevel = 50;
        public bool JustLeveledUp = false;

        public float DashCooldown = 0f;
        public float DashDuration = 0f;
        public bool IsDashing => DashDuration > 0f;
        public int DashDamage = 2;

        float walkTime;
        bool moving;

        public float Width => Texture.Width * Scale;
        public float Height => Texture.Height * Scale;

        public Rectangle Bounds => new Rectangle(
            (int)(Position.X - Width * 0.25f),
            (int)(Position.Y - Height * 0.8f),
            (int)(Width * 0.5f),
            (int)(Height * 0.8f));

        public Vector2 Muzzle => Position + new Vector2(
            (FacingLeft ? -1f : 1f) * Width * 0.48f,
            -Height * 0.5f);

        public Player(Texture2D texture, float targetHeight, Vector2 start)
        {
            Texture = texture;
            Scale = targetHeight / texture.Height;
            Position = start;
        }

        public void Update(float dt, KeyboardState kb, MouseState mouse, Rectangle arena)
        {
            Vector2 move = Vector2.Zero;

            // Both WASD and Arrow Keys work now
            if (kb.IsKeyDown(Keys.W) || kb.IsKeyDown(Keys.Up)) move.Y -= 1;
            if (kb.IsKeyDown(Keys.S) || kb.IsKeyDown(Keys.Down)) move.Y += 1;
            if (kb.IsKeyDown(Keys.A) || kb.IsKeyDown(Keys.Left)) move.X -= 1;
            if (kb.IsKeyDown(Keys.D) || kb.IsKeyDown(Keys.Right)) move.X += 1;

            moving = move != Vector2.Zero;

            DashCooldown -= dt;
            DashDuration -= dt;

            if (kb.IsKeyDown(Keys.Space) && DashCooldown <= 0f && !IsDashing)
            {
                DashDuration = 0.15f;
                DashCooldown = 1.2f;
            }

            float currentSpeed = Speed;
            if (IsDashing) currentSpeed *= 2.5f;

            if (moving)
            {
                move.Normalize();
                Position += move * currentSpeed * dt;
                walkTime += dt;
            }

            Position.X = MathHelper.Clamp(Position.X, arena.Left + Width / 2f, arena.Right - Width / 2f);
            Position.Y = MathHelper.Clamp(Position.Y, arena.Top, arena.Bottom);
            FacingLeft = mouse.X < Position.X;

            JustLeveledUp = false;
        }

        public void GainXP(int amount)
        {
            XP += amount;
            if (XP >= XPToNextLevel)
            {
                XP -= XPToNextLevel;
                Level++;
                XPToNextLevel = (int)(XPToNextLevel * 1.3f);
                MaxHealth += 10;
                Health = Math.Min(Health + 10, MaxHealth);
                JustLeveledUp = true;
            }
        }

        public void Draw(SpriteBatch sb, float depth)
        {
            float bob = moving ? (float)Math.Sin(walkTime * 14f) * 2f : 0f;
            Vector2 origin = new Vector2(Texture.Width / 2f, Texture.Height);
            Color drawColor = IsDashing ? new Color(255, 255, 255, 200) : Color.White;

            sb.Draw(Texture, Position + new Vector2(0, bob), null, drawColor, 0f, origin, Scale,
                FacingLeft ? SpriteEffects.FlipHorizontally : SpriteEffects.None, depth);
        }
    }
}