using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Roma
{
    public class Game1 : Game
    {
        const int ScreenW = 1280;
        const int ScreenH = 720;
        const int TileSize = 128;
        const float FireDelay = 0.12f;      // seconds between shots
        const float BulletSpeed = 900f;     // pixels per second

        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;

        Texture2D playerTex, bulletTex, floorTex;
        Texture2D shadowTex, glowTex;

        Player player;
        List<Bullet> bullets = new List<Bullet>();
        float fireTimer;

        Rectangle screenRect = new Rectangle(0, 0, ScreenW, ScreenH);
        Rectangle arena = new Rectangle(0, 160, ScreenW, ScreenH - 160);

        public Game1()
        {
            graphics = new GraphicsDeviceManager(this)
            {
                PreferredBackBufferWidth = ScreenW,
                PreferredBackBufferHeight = ScreenH
            };
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
            Window.Title = "Roma";
        }

        protected override void Initialize()
        {
            base.Initialize();   // triggers LoadContent
        }

        protected override void LoadContent()
        {
            spriteBatch = new SpriteBatch(GraphicsDevice);

            // Asset names have no extension type. Placeholders show up if the file is missing.
            playerTex = LoadTex("player", 80, 110, Color.CornflowerBlue);
            bulletTex = LoadTex("bullet_player", 40, 12, Color.Cyan);
            floorTex = LoadTex("floor1", 64, 64, new Color(40, 40, 55));

            shadowTex = MakeRadial(64);
            glowTex = MakeRadial(64);

            player = new Player(playerTex, 110f, new Vector2(ScreenW / 2f, ScreenH * 0.7f));
        }

        protected override void Update(GameTime gameTime)
        {
            KeyboardState kb = Keyboard.GetState();
            MouseState mouse = Mouse.GetState();
            if (kb.IsKeyDown(Keys.Escape)) Exit();

            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

            player.Update(dt, kb, mouse, arena);

            // Hold the left button to keep firing
            fireTimer -= dt;
            if (mouse.LeftButton == ButtonState.Pressed && fireTimer <= 0f)
            {
                Fire(mouse);
                fireTimer = FireDelay;
            }

            
            for (int i = bullets.Count - 1; i >= 0; i--)
            {
                bullets[i].Update(dt);
                if (!bullets[i].IsAlive(screenRect))
                    bullets.RemoveAt(i);
            }

            base.Update(gameTime);
        }

        void Fire(MouseState mouse)
        {
            Vector2 muzzle = player.Muzzle;
            Vector2 dir = new Vector2(mouse.X, mouse.Y) - muzzle;
            if (dir.LengthSquared() < 1f) return;    // cursor is on the muzzle
            dir.Normalize();
            bullets.Add(new Bullet(muzzle, dir * BulletSpeed));
        }


        static float DepthOf(float y)
        {
            return 0.01f + 0.98f * MathHelper.Clamp(y / ScreenH, 0f, 1f);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(new Color(10, 10, 16));

            // LinearClamp because the art is scaled down a lot. PointClamp would look noisy.
            spriteBatch.Begin(SpriteSortMode.FrontToBack, BlendState.AlphaBlend, SamplerState.LinearClamp);

            DrawFloor();

            DrawShadow(player.Position, player.Width * 0.7f, 0.45f);
            player.Draw(spriteBatch, DepthOf(player.Position.Y));

            Vector2 bulletOrigin = new Vector2(bulletTex.Width / 2f, bulletTex.Height / 2f);
            float bulletScale = 44f / bulletTex.Width;      // about 44 px long on screen
            foreach (Bullet b in bullets)
            {
                // Bullets fly about 50 px above the ground, so their shadow sits below them
                Vector2 ground = b.Position + new Vector2(0, 50);
                DrawShadow(ground, 22f, 0.25f);
                spriteBatch.Draw(bulletTex, b.Position, null, Color.White, b.Rotation,
                                 bulletOrigin, bulletScale, SpriteEffects.None,
                                 DepthOf(ground.Y) + 0.001f);
            }
            spriteBatch.End();

            //Glow added on top as light
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, SamplerState.LinearClamp);
            Vector2 glowOrigin = new Vector2(glowTex.Width / 2f, glowTex.Height / 2f);
            foreach (Bullet b in bullets)
                spriteBatch.Draw(glowTex, b.Position, null, Color.Cyan * 0.7f, 0f,
                                 glowOrigin, 56f / glowTex.Width, SpriteEffects.None, 0f);
            spriteBatch.End();

            base.Draw(gameTime);
        }

        void DrawFloor()
        {
            float scale = TileSize / (float)floorTex.Width;
            for (int y = arena.Top - TileSize; y < ScreenH; y += TileSize)
                for (int x = 0; x < ScreenW; x += TileSize)
                    spriteBatch.Draw(floorTex, new Vector2(x, y), null, Color.White, 0f,
                                     Vector2.Zero, scale, SpriteEffects.None, 0f);
        }

        // A squashed dark ellipse under whatever is standing or flying
        void DrawShadow(Vector2 ground, float width, float alpha)
        {
            float sx = width / shadowTex.Width;
            spriteBatch.Draw(shadowTex, ground, null, Color.Black * alpha, 0f,
                             new Vector2(shadowTex.Width / 2f, shadowTex.Height / 2f),
                             new Vector2(sx, sx * 0.35f), SpriteEffects.None,
                             DepthOf(ground.Y) - 0.005f);
        }

        Texture2D LoadTex(string name, int w, int h, Color fallback)
        {
            try
            {
                return Content.Load<Texture2D>(name);
            }
            catch (ContentLoadException)
            {
                Texture2D t = new Texture2D(GraphicsDevice, w, h);
                Color[] data = new Color[w * h];
                for (int i = 0; i < data.Length; i++) data[i] = fallback;
                t.SetData(data);
                return t;
            }
        }

        // White circle that fades out toward the edge
        Texture2D MakeRadial(int size)
        {
            Texture2D t = new Texture2D(GraphicsDevice, size, size);
            Color[] data = new Color[size * size];
            float r = size / 2f;
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(r, r)) / r;
                    float a = MathHelper.Clamp(1f - d, 0f, 1f);
                    a *= a;
                    data[y * size + x] = new Color(a, a, a, a);
                }
            t.SetData(data);
            return t;
        }
    }
}