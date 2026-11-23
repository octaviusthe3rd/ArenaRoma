using System;
using Microsoft.Xna.Framework;

namespace Roma
{
    public class Bullet
    {
        public Vector2 Position;
        public Vector2 Velocity;      // pixels per second
        public float Rotation;
        public float Life = 1.5f;     // seconds before it disappears

        public Bullet(Vector2 position, Vector2 velocity)
        {
            Position = position;
            Velocity = velocity;
            Rotation = (float)Math.Atan2(velocity.Y, velocity.X);
        }

        public void Update(float dt)
        {
            Position += Velocity * dt;
            Life -= dt;
        }

        public bool IsAlive(Rectangle screen)
        {
            return Life > 0f && screen.Contains(Position.ToPoint());
        }
    }
}