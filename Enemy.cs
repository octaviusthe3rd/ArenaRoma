using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Roma
{
    public class Enemy
    {
        public Vector2 Position;
        public Texture2D Texture;
        public float Scale;
        public float Speed = 140f;
        public int Health = 3;
        public bool FacingLeft;

        public bool IsShooter = false;
        public float ShootCooldown = 2f;
        public float PreferredDistance = 250f;

        public float FlashTimer = 0f; // NEW: White flash on hit

        public float Width => Texture.Width * Scale;
        public float Height => Texture.Height * Scale;

        public Rectangle Bounds => new Rectangle(
            (int)(Position.X - Width * 0.3f),
            (int)(Position.Y - Height * 0.85f),
            (int)(Width * 0.6f),
            (int)(Height * 0.85f)
        );

        public Enemy(Texture2D texture, float targetHeight, Vector2 start)
        {
            Texture = texture;
            Scale = targetHeight / texture.Height;
            Position = start;
        }

        public virtual void Update(float dt, Vector2 playerPos, List<EnemyProjectile> projectiles)
        {
            FlashTimer -= dt; // NEW

            Vector2 dir = playerPos - Position;
            FacingLeft = dir.X < 0;

            if (IsShooter)
            {
                float dist = dir.Length();
                if (dist > PreferredDistance + 30f)
                {
                    if (dir.LengthSquared() > 1f) dir.Normalize();
                    Position += dir * Speed * dt;
                }
                else if (dist < PreferredDistance - 30f)
                {
                    if (dir.LengthSquared() > 1f) dir.Normalize();
                    Position -= dir * Speed * dt * 0.5f;
                }

                ShootCooldown -= dt;
                if (ShootCooldown <= 0f)
                {
                    ShootCooldown = 2.5f;
                    if (dir.LengthSquared() > 1f) dir.Normalize();
                    projectiles.Add(new EnemyProjectile(Position + new Vector2(0, -Height * 0.5f), dir * 350f));
                }
            }
            else
            {
                if (dir.LengthSquared() > 1f)
                {
                    dir.Normalize();
                    Position += dir * Speed * dt;
                }
            }
        }

        public virtual void Draw(SpriteBatch sb, float depth)
        {
            float bob = (float)Math.Sin(DateTime.Now.Ticks / 2000000.0) * 3f;
            Vector2 origin = new Vector2(Texture.Width / 2f, Texture.Height);

            // NEW: Flash white when hit
            Color drawColor = FlashTimer > 0 ? Color.White : Color.White;

            sb.Draw(Texture, Position + new Vector2(0, bob), null, drawColor, 0f, origin, Scale,
                FacingLeft ? SpriteEffects.FlipHorizontally : SpriteEffects.None, depth);
        }
    }
}