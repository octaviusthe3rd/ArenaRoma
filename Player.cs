using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Roma
{
    public class Player
    {
        public Vector2 Position;          // bottom of the sprite
        public Texture2D Texture;
        public float Scale;
        public float Speed = 260f;        // pixels per second
        public bool FacingLeft;
        public int Health = 100;

        float walkTime;
        bool moving;

        public float Width => Texture.Width * Scale;
        public float Height => Texture.Height * Scale;

        // Hitbox for enemy bullets, pickups (smaller than the sprite)
        public Rectangle Bounds => new Rectangle(
            (int)(Position.X - Width * 0.25f),
            (int)(Position.Y - Height * 0.8f),
            (int)(Width * 0.5f),
            (int)(Height * 0.8f));

        // Where bullets come outs.
        public Vector2 Muzzle => Position + new Vector2(
            (FacingLeft ? -1f : 1f) * Width * 0.48f,
            -Height * 0.5f);

        public Player(Texture2D texture, float targetHeight, Vector2 start)
        {
            Texture = texture;
            Scale = targetHeight / texture.Height;   // art is huge, so scale to a game-sized height
            Position = start;
        }

        public void Update(float dt, KeyboardState kb, MouseState mouse, Rectangle arena)
        {
            Vector2 move = Vector2.Zero;
            if (kb.IsKeyDown(Keys.W)) move.Y -= 1;
            if (kb.IsKeyDown(Keys.S)) move.Y += 1;
            if (kb.IsKeyDown(Keys.A)) move.X -= 1;
            if (kb.IsKeyDown(Keys.D)) move.X += 1;

            moving = move != Vector2.Zero;
            if (moving)
            {
                move.Normalize();                 // so diagonals aren't faster
                Position += move * Speed * dt;
                walkTime += dt;
            }

            // Keep the feet inside the arena
            Position.X = MathHelper.Clamp(Position.X, arena.Left + Width / 2f, arena.Right - Width / 2f);
            Position.Y = MathHelper.Clamp(Position.Y, arena.Top, arena.Bottom);

            // Face the mouse by flipping, not rotating.
            FacingLeft = mouse.X < Position.X;
        }

        public void Draw(SpriteBatch sb, float depth)
        {
            float bob = moving ? (float)Math.Sin(walkTime * 14f) * 2f : 0f;   // small up/down bounce
            Vector2 origin = new Vector2(Texture.Width / 2f, Texture.Height);  // feet = bottom center

            sb.Draw(Texture,
                    Position + new Vector2(0, bob),
                    null, Color.White, 0f, origin, Scale,
                    FacingLeft ? SpriteEffects.FlipHorizontally : SpriteEffects.None,
                    depth);
        }
    }
}