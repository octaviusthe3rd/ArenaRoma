using System;
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

        static readonly Color Gold = new Color(212, 175, 55);
        static readonly Color DarkGold = new Color(139, 115, 35);
        static readonly Color AgedWhite = new Color(220, 210, 190);
        static readonly Color DarkBg = new Color(15, 12, 10);

        enum GameState { Menu, Playing, WaveTransition, LevelUp, GameOver }
        GameState currentState = GameState.Menu;

        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;
        Random random = new Random();

        Texture2D playerTex, bulletTex, floorTex;
        Texture2D shadowTex, glowTex;
        Texture2D chaserTex, shooterTex;
        Texture2D sparkTex, explosionTex, enemyBulletTex, pickupTex;
        Texture2D pixelTex, vignetteTex, noiseTex;
        SpriteFont font;

        Player player;
        List<Bullet> bullets = new List<Bullet>();
        List<Enemy> enemies = new List<Enemy>();
        List<EnemyProjectile> enemyProjectiles = new List<EnemyProjectile>();
        List<Pickup> pickups = new List<Pickup>();
        List<Particle> particles = new List<Particle>();
        List<FloatingText> floatingTexts = new List<FloatingText>();
        List<Shockwave> shockwaves = new List<Shockwave>();
        List<Particle> ambientParticles = new List<Particle>();

        float fireTimer;
        float fireDelay = 0.15f;
        float bulletSpeed = 900f;
        float spawnTimer = 0f;
        float damageCooldown = 0f;

        int currentWave = 1;
        int enemiesToSpawnThisWave = 5;
        int enemiesSpawnedThisWave = 0;
        float waveTransitionTimer = 0f;

        List<Upgrade> currentUpgradeChoices = new List<Upgrade>();

        float shakeTimer = 0f;
        float shakeMagnitude = 0f;
        Vector2 shakeOffset = Vector2.Zero;

        float hitStopTimer = 0f;
        int comboCount = 0;
        float comboTimer = 0f;
        float comboTimeout = 2.0f;

        float cameraZoom = 1.0f;
        float targetZoom = 1.0f;

        bool hasShotgun = false;
        bool hasPiercing = false;

        public int Score = 0;

        Rectangle screenRect = new Rectangle(0, 0, ScreenW, ScreenH);
        Rectangle arena = new Rectangle(80, 200, ScreenW - 160, ScreenH - 280);

        public Game1()
        {
            graphics = new GraphicsDeviceManager(this) { PreferredBackBufferWidth = ScreenW, PreferredBackBufferHeight = ScreenH };
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
            Window.Title = "Arena Roma";
        }

        protected override void Initialize() { base.Initialize(); }

        protected override void LoadContent()
        {
            spriteBatch = new SpriteBatch(GraphicsDevice);

            playerTex = Content.Load<Texture2D>("player");
            bulletTex = Content.Load<Texture2D>("bullet_player");
            floorTex = Content.Load<Texture2D>("floor1");
            chaserTex = Content.Load<Texture2D>("chaser");
            shooterTex = Content.Load<Texture2D>("shooter");
            sparkTex = Content.Load<Texture2D>("spark");
            explosionTex = Content.Load<Texture2D>("explosion");
            enemyBulletTex = Content.Load<Texture2D>("bullet_enemy");
            pickupTex = Content.Load<Texture2D>("pu_heal");

            pixelTex = new Texture2D(GraphicsDevice, 1, 1);
            pixelTex.SetData(new Color[] { Color.White });

            vignetteTex = MakeVignette();
            noiseTex = MakeNoise(256);

            try { font = Content.Load<SpriteFont>("font"); } catch { font = null; }

            shadowTex = MakeRadial(64, Color.Black);
            glowTex = MakeRadial(64, Color.White);

            for (int i = 0; i < 50; i++)
            {
                ambientParticles.Add(new Particle(
                    new Vector2((float)random.NextDouble() * ScreenW, (float)random.NextDouble() * ScreenH),
                    new Vector2((float)(random.NextDouble() * 20 - 10), (float)(random.NextDouble() * -30 - 10)),
                    10f, 0.05f + (float)random.NextDouble() * 0.1f,
                    new Color(100, 150, 200, 50)
                ));
            }

            ResetGame();
        }

        void ResetGame()
        {
            player = new Player(playerTex, 110f, new Vector2(ScreenW / 2f, ScreenH * 0.7f));
            enemies.Clear(); bullets.Clear(); enemyProjectiles.Clear(); pickups.Clear();
            particles.Clear(); floatingTexts.Clear(); shockwaves.Clear();
            Score = 0;
            currentWave = 1;
            enemiesToSpawnThisWave = 5;
            enemiesSpawnedThisWave = 0;
            fireDelay = 0.15f;
            bulletSpeed = 900f;
            comboCount = 0;
            comboTimer = 0f;
            hasShotgun = false;
            hasPiercing = false;
            cameraZoom = 1.0f;
            targetZoom = 1.0f;
        }

        protected override void Update(GameTime gameTime)
        {
            KeyboardState kb = Keyboard.GetState();
            MouseState mouse = Mouse.GetState();
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

            if (kb.IsKeyDown(Keys.Escape)) Exit();

            if (hitStopTimer > 0)
            {
                hitStopTimer -= dt;
                return;
            }

            cameraZoom = MathHelper.Lerp(cameraZoom, targetZoom, dt * 5f);

            if (shakeTimer > 0)
            {
                shakeTimer -= dt;
                shakeOffset = new Vector2((float)(random.NextDouble() * 2 - 1) * shakeMagnitude, (float)(random.NextDouble() * 2 - 1) * shakeMagnitude);
                shakeMagnitude *= 0.9f;
            }
            else { shakeOffset = Vector2.Zero; }

            if (comboTimer > 0)
            {
                comboTimer -= dt;
                if (comboTimer <= 0) comboCount = 0;
            }

            switch (currentState)
            {
                case GameState.Menu:
                    targetZoom = 0.95f;
                    if (kb.IsKeyDown(Keys.Enter)) { currentState = GameState.WaveTransition; waveTransitionTimer = 2.0f; }
                    break;
                case GameState.WaveTransition:
                    targetZoom = 1.0f;
                    waveTransitionTimer -= dt;
                    if (waveTransitionTimer <= 0) currentState = GameState.Playing;
                    break;
                case GameState.LevelUp:
                    targetZoom = 0.9f;
                    if (kb.IsKeyDown(Keys.D1) && currentUpgradeChoices.Count > 0) ApplyUpgrade(0);
                    else if (kb.IsKeyDown(Keys.D2) && currentUpgradeChoices.Count > 1) ApplyUpgrade(1);
                    else if (kb.IsKeyDown(Keys.D3) && currentUpgradeChoices.Count > 2) ApplyUpgrade(2);
                    break;
                case GameState.Playing:
                    targetZoom = 1.05f;
                    UpdateGameplay(dt, kb, mouse);
                    break;
                case GameState.GameOver:
                    targetZoom = 0.9f;
                    if (kb.IsKeyDown(Keys.R)) { ResetGame(); currentState = GameState.Menu; }
                    break;
            }

            base.Update(gameTime);
        }

        void UpdateGameplay(float dt, KeyboardState kb, MouseState mouse)
        {
            player.Update(dt, kb, mouse, arena);

            if (player.IsDashing)
            {
                for (int i = 0; i < 3; i++)
                {
                    Vector2 offset = new Vector2((float)(random.NextDouble() * 30 - 15), (float)(random.NextDouble() * 30 - 15));
                    particles.Add(new Particle(player.Position + offset, Vector2.Zero, 0.25f, 0.5f, new Color(150, 200, 255, 200)));
                }

                foreach (var enemy in enemies)
                {
                    if (enemy.Bounds.Intersects(player.Bounds))
                    {
                        enemy.Health -= player.DashDamage;
                        enemy.FlashTimer = 0.1f;
                        SpawnSparks(enemy.Position, 5, Color.Cyan);
                        floatingTexts.Add(new FloatingText(enemy.Position + new Vector2(0, -50), "DASH!", Color.Cyan));

                        if (enemy.Health <= 0)
                        {
                            KillEnemy(enemy);
                        }
                    }
                }

                if (player.DashDuration > 0.14f)
                {
                    shockwaves.Add(new Shockwave { Position = player.Position });
                    Shake(6f, 0.2f);
                }
            }

            if (player.JustLeveledUp)
            {
                currentState = GameState.LevelUp;
                GenerateUpgradeChoices();
                Shake(12f, 0.4f);
                SpawnSparks(player.Position, 20, Gold);
                return;
            }

            fireTimer -= dt;
            if (mouse.LeftButton == ButtonState.Pressed && fireTimer <= 0f)
            {
                Fire(mouse);
                fireTimer = fireDelay;

                Vector2 muzzle = player.Muzzle;
                particles.Add(new Particle(muzzle, Vector2.Zero, 0.08f, 0.4f, Color.White));
                particles.Add(new Particle(muzzle, Vector2.Zero, 0.06f, 0.6f, Color.Yellow));
            }

            if (enemiesSpawnedThisWave < enemiesToSpawnThisWave)
            {
                spawnTimer -= dt;
                if (spawnTimer <= 0f)
                {
                    float spawnX = arena.Left + 50 + (float)random.NextDouble() * (arena.Width - 100);
                    float spawnY = arena.Top + 20;
                    bool isShooter = random.NextDouble() < 0.3;
                    float targetHeight = isShooter ? 100f : 90f;
                    var newEnemy = new Enemy(isShooter ? shooterTex : chaserTex, targetHeight, new Vector2(spawnX, spawnY));
                    if (isShooter) { newEnemy.IsShooter = true; newEnemy.Speed = 80f; newEnemy.Health = 2; }
                    enemies.Add(newEnemy);
                    enemiesSpawnedThisWave++;
                    spawnTimer = 1.5f;
                }
            }
            else if (enemies.Count == 0)
            {
                currentWave++;
                enemiesToSpawnThisWave = 5 + (currentWave * 3);
                enemiesSpawnedThisWave = 0;
                currentState = GameState.WaveTransition;
                waveTransitionTimer = 3.0f;
            }

            damageCooldown -= dt;
            foreach (var enemy in enemies)
            {
                enemy.Update(dt, player.Position, enemyProjectiles);
                if (damageCooldown <= 0f && !player.IsDashing && enemy.Bounds.Intersects(player.Bounds))
                {
                    player.Health -= 25;
                    damageCooldown = 0.5f;
                    Shake(8f, 0.3f);
                    SpawnSparks(player.Position, 8, Color.Red);
                    if (player.Health <= 0) { currentState = GameState.GameOver; Shake(15f, 0.5f); }
                }
            }

            for (int i = enemyProjectiles.Count - 1; i >= 0; i--)
            {
                enemyProjectiles[i].Update(dt);
                if (!enemyProjectiles[i].IsAlive) { enemyProjectiles.RemoveAt(i); continue; }
                if (damageCooldown <= 0f && !player.IsDashing && enemyProjectiles[i].Bounds.Intersects(player.Bounds))
                {
                    player.Health -= 20;
                    damageCooldown = 0.5f;
                    Shake(6f, 0.2f);
                    enemyProjectiles.RemoveAt(i);
                    if (player.Health <= 0) { currentState = GameState.GameOver; Shake(15f, 0.5f); }
                }
            }

            for (int i = bullets.Count - 1; i >= 0; i--)
            {
                bullets[i].Update(dt);
                bool hit = false;
                for (int j = enemies.Count - 1; j >= 0; j--)
                {
                    if (bullets[i].Bounds.Intersects(enemies[j].Bounds))
                    {
                        enemies[j].Health--;
                        enemies[j].FlashTimer = 0.1f;
                        hit = true;
                        SpawnSparks(bullets[i].Position, 2, Color.Yellow);

                        floatingTexts.Add(new FloatingText(enemies[j].Position + new Vector2(0, -50), "1", Gold));

                        if (enemies[j].Health <= 0)
                        {
                            KillEnemy(enemies[j]);

                            if (!hasPiercing)
                            {
                                bullets.RemoveAt(i);
                                break;
                            }
                        }
                        else
                        {
                            if (!hasPiercing)
                            {
                                bullets.RemoveAt(i);
                                break;
                            }
                        }
                    }
                }
                if (hit && !hasPiercing) continue;
                if (!bullets[i].IsAlive(screenRect)) bullets.RemoveAt(i);
            }

            for (int i = pickups.Count - 1; i >= 0; i--)
            {
                pickups[i].Update(dt);
                if (!pickups[i].IsAlive) { pickups.RemoveAt(i); continue; }
                if (pickups[i].Bounds.Intersects(player.Bounds))
                {
                    player.Health = Math.Min(player.Health + pickups[i].HealAmount, player.MaxHealth);
                    SpawnSparks(pickups[i].Position, 10, Color.LimeGreen);
                    floatingTexts.Add(new FloatingText(pickups[i].Position, "+25", Color.LimeGreen));
                    pickups.RemoveAt(i);
                }
            }

            for (int i = shockwaves.Count - 1; i >= 0; i--)
            {
                shockwaves[i].Update(dt);
                if (!shockwaves[i].IsAlive) { shockwaves.RemoveAt(i); continue; }

                if (!shockwaves[i].HasHit)
                {
                    foreach (var enemy in enemies)
                    {
                        float dist = Vector2.Distance(shockwaves[i].Position, enemy.Position);
                        if (dist < shockwaves[i].Radius && dist > shockwaves[i].Radius - 30)
                        {
                            enemy.Health -= shockwaves[i].Damage;
                            enemy.FlashTimer = 0.1f;
                            SpawnSparks(enemy.Position, 3, Color.Cyan);

                            if (enemy.Health <= 0)
                            {
                                KillEnemy(enemy);
                            }
                        }
                    }
                    shockwaves[i].HasHit = true;
                }
            }

            for (int i = particles.Count - 1; i >= 0; i--)
            {
                particles[i].Update(dt);
                if (!particles[i].IsAlive) particles.RemoveAt(i);
            }

            for (int i = floatingTexts.Count - 1; i >= 0; i--)
            {
                floatingTexts[i].Update(dt);
                if (!floatingTexts[i].IsAlive) floatingTexts.RemoveAt(i);
            }

            for (int i = 0; i < ambientParticles.Count; i++)
            {
                ambientParticles[i].Update(dt);
                if (!ambientParticles[i].IsAlive || ambientParticles[i].Position.Y < -50)
                {
                    ambientParticles[i].Position = new Vector2((float)random.NextDouble() * ScreenW, ScreenH + 50);
                    ambientParticles[i].Life = ambientParticles[i].MaxLife;
                }
            }
        }

        void KillEnemy(Enemy enemy)
        {
            SpawnExplosion(enemy.Position, enemy.IsShooter ? Color.Purple : Color.Crimson);
            if (random.NextDouble() < 0.15) pickups.Add(new Pickup { Position = enemy.Position });
            enemies.Remove(enemy);

            comboCount++;
            comboTimer = comboTimeout;
            int comboMultiplier = Math.Min(comboCount, 10);
            int scoreGain = 10 * comboMultiplier;
            Score += scoreGain;

            if (comboCount > 1)
            {
                floatingTexts.Add(new FloatingText(enemy.Position + new Vector2(0, -80), $"x{comboMultiplier}", Gold));
            }

            player.GainXP(15);
            Shake(4f, 0.15f);
            hitStopTimer = 0.05f;
        }

        void SpawnExplosion(Vector2 pos, Color color)
        {
            particles.Add(new Particle(pos, Vector2.Zero, 0.15f, 0.6f, Color.White) { UseExplosionTexture = true });
            particles.Add(new Particle(pos, Vector2.Zero, 0.3f, 0.4f, color) { UseExplosionTexture = true, ScaleOverTime = true });

            for (int i = 0; i < 8; i++)
            {
                Vector2 vel = new Vector2((float)(random.NextDouble() * 300 - 150), (float)(random.NextDouble() * 300 - 150));
                particles.Add(new Particle(pos, vel, 0.4f, 0.2f, color));
            }
        }

        void GenerateUpgradeChoices()
        {
            currentUpgradeChoices.Clear();
            var allUpgrades = new List<Upgrade> {
                new Upgrade("Vitality", "+25 Max Health, Heal 25", Color.LimeGreen, (p, g) => { p.MaxHealth += 25; p.Health = Math.Min(p.Health + 25, p.MaxHealth); }),
                new Upgrade("Agility", "+15% Move Speed", Color.Cyan, (p, g) => { p.Speed *= 1.15f; }),
                new Upgrade("Rapid Fire", "-15% Fire Delay", Gold, (p, g) => { g.fireDelay *= 0.85f; }),
                new Upgrade("Heavy Caliber", "+20% Bullet Speed", Color.OrangeRed, (p, g) => { g.bulletSpeed *= 1.2f; }),
                new Upgrade("Thick Skin", "Heal 30 HP", Color.CornflowerBlue, (p, g) => { p.Health = Math.Min(p.Health + 30, p.MaxHealth); }),
                new Upgrade("Shotgun", "Fire 3 bullets in a spread", Color.Red, (p, g) => { g.hasShotgun = true; }),
                new Upgrade("Piercing", "Bullets pass through enemies", Color.Magenta, (p, g) => { g.hasPiercing = true; })
            };

            for (int i = 0; i < 3; i++)
            {
                if (allUpgrades.Count == 0) break;
                int idx = random.Next(allUpgrades.Count);
                currentUpgradeChoices.Add(allUpgrades[idx]);
                allUpgrades.RemoveAt(idx);
            }
        }

        void ApplyUpgrade(int index)
        {
            if (index < currentUpgradeChoices.Count)
            {
                currentUpgradeChoices[index].ApplyEffect(player, this);
                currentState = GameState.Playing;
                currentUpgradeChoices.Clear();
            }
        }

        void Shake(float magnitude, float duration) { shakeMagnitude = Math.Max(shakeMagnitude, magnitude); shakeTimer = Math.Max(shakeTimer, duration); }

        void Fire(MouseState mouse)
        {
            Vector2 muzzle = player.Muzzle;
            Vector2 dir = new Vector2(mouse.X, mouse.Y) - muzzle;
            if (dir.LengthSquared() < 1f) return;
            dir.Normalize();
            float bScale = 44f / bulletTex.Width;

            if (hasShotgun)
            {
                for (int i = -1; i <= 1; i++)
                {
                    float angle = (float)Math.Atan2(dir.Y, dir.X) + i * 0.15f;
                    Vector2 spreadDir = new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle));
                    bullets.Add(new Bullet(muzzle, spreadDir * bulletSpeed, bScale));
                }
            }
            else
            {
                bullets.Add(new Bullet(muzzle, dir * bulletSpeed, bScale));
            }
        }

        void SpawnSparks(Vector2 pos, int count, Color color)
        {
            for (int i = 0; i < count; i++)
            {
                Vector2 vel = new Vector2((float)(random.NextDouble() * 250 - 125), (float)(random.NextDouble() * 250 - 125));
                particles.Add(new Particle(pos, vel, 0.3f, 0.15f, color));
            }
        }

        static float DepthOf(float y) => 0.01f + 0.98f * MathHelper.Clamp(y / ScreenH, 0f, 1f);

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(DarkBg);

            Matrix zoomMatrix = Matrix.CreateScale(cameraZoom) * Matrix.CreateTranslation(
                ScreenW / 2f - (ScreenW / 2f * cameraZoom),
                ScreenH / 2f - (ScreenH / 2f * cameraZoom), 0f);
            Matrix shakeMatrix = zoomMatrix * Matrix.CreateTranslation(shakeOffset.X, shakeOffset.Y, 0f);

            spriteBatch.Begin(SpriteSortMode.FrontToBack, BlendState.AlphaBlend, SamplerState.LinearClamp, transformMatrix: shakeMatrix);
            DrawFloor();

            foreach (var ap in ambientParticles)
            {
                ap.Draw(spriteBatch, sparkTex, 0.005f);
            }

            var drawQueue = new List<(float depth, object obj)>();
            drawQueue.Add((DepthOf(player.Position.Y), player));
            foreach (var e in enemies) drawQueue.Add((DepthOf(e.Position.Y), e));
            foreach (var b in bullets) drawQueue.Add((DepthOf(b.Position.Y + 50), b));
            foreach (var ep in enemyProjectiles) drawQueue.Add((DepthOf(ep.Position.Y + 50), ep));
            foreach (var pk in pickups) drawQueue.Add((DepthOf(pk.Position.Y), pk));
            foreach (var p in particles) drawQueue.Add((DepthOf(p.Position.Y), p));
            foreach (var sw in shockwaves) drawQueue.Add((DepthOf(sw.Position.Y), sw));
            drawQueue.Sort((a, b) => a.depth.CompareTo(b.depth));

            foreach (var entity in drawQueue)
            {
                if (entity.obj is Player p) { DrawShadow(p.Position, p.Width * 0.7f, 0.45f); p.Draw(spriteBatch, entity.depth); }
                else if (entity.obj is Enemy e) { DrawShadow(e.Position, e.Width * 0.7f, 0.45f); e.Draw(spriteBatch, entity.depth); }
                else if (entity.obj is Bullet b)
                {
                    Vector2 ground = b.Position + new Vector2(0, 50); DrawShadow(ground, 22f, 0.25f);
                    spriteBatch.Draw(bulletTex, b.Position, null, Color.White, b.Rotation, new Vector2(bulletTex.Width / 2f, bulletTex.Height / 2f), b.Scale, SpriteEffects.None, entity.depth + 0.001f);
                }
                else if (entity.obj is EnemyProjectile ep)
                {
                    Vector2 ground = ep.Position + new Vector2(0, 50); DrawShadow(ground, 15f, 0.2f);
                    ep.Draw(spriteBatch, enemyBulletTex, entity.depth + 0.001f);
                }
                else if (entity.obj is Pickup pk) { DrawShadow(pk.Position, 15f, 0.3f); pk.Draw(spriteBatch, pickupTex, entity.depth); }
                else if (entity.obj is Particle pt)
                {
                    if (pt.UseExplosionTexture)
                    {
                        float alpha = pt.Life / pt.MaxLife;
                        float scale = pt.ScaleOverTime ? pt.Scale * (1f + (1f - alpha) * 0.5f) : pt.Scale;
                        spriteBatch.Draw(explosionTex, pt.Position, null, pt.Tint * alpha, 0f, new Vector2(explosionTex.Width / 2f, explosionTex.Height / 2f), scale, SpriteEffects.None, entity.depth + 0.002f);
                    }
                    else
                        pt.Draw(spriteBatch, sparkTex, entity.depth + 0.002f);
                }
                else if (entity.obj is Shockwave sw) { sw.Draw(spriteBatch, pixelTex, entity.depth + 0.001f); }
            }

            spriteBatch.End();

            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp);
            if (font != null)
            {
                foreach (var ft in floatingTexts)
                {
                    Vector2 origin = font.MeasureString(ft.Text) / 2f;
                    float alpha = ft.Life / ft.MaxLife;

                    // Tighter glow - single layer at 1.2f scale
                    spriteBatch.DrawString(font, ft.Text, ft.Position, ft.Color * alpha * 0.4f, 0f, origin, ft.Scale * 1.2f, SpriteEffects.None, 0.98f);

                    Vector2 shadowPos = ft.Position + new Vector2(2, 2);
                    spriteBatch.DrawString(font, ft.Text, shadowPos, Color.Black * alpha, 0f, origin, ft.Scale, SpriteEffects.None, 0.99f);

                    ft.Draw(spriteBatch, font, 1.0f);
                }
            }
            spriteBatch.End();

            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, SamplerState.LinearClamp, transformMatrix: shakeMatrix);
            Vector2 glowOrigin = new Vector2(glowTex.Width / 2f, glowTex.Height / 2f);
            foreach (Bullet b in bullets)
            {
                spriteBatch.Draw(glowTex, b.Position, null, Color.Cyan * 0.8f, 0f, glowOrigin, 60f / glowTex.Width, SpriteEffects.None, 0f);
                spriteBatch.Draw(glowTex, b.Position, null, Color.White * 0.4f, 0f, glowOrigin, 40f / glowTex.Width, SpriteEffects.None, 0f);
            }
            foreach (EnemyProjectile ep in enemyProjectiles)
            {
                spriteBatch.Draw(glowTex, ep.Position, null, Color.OrangeRed * 0.7f, 0f, glowOrigin, 45f / glowTex.Width, SpriteEffects.None, 0f);
                spriteBatch.Draw(glowTex, ep.Position, null, Color.Yellow * 0.3f, 0f, glowOrigin, 30f / glowTex.Width, SpriteEffects.None, 0f);
            }
            spriteBatch.End();

            spriteBatch.Begin();

            if (currentState == GameState.Menu) DrawMenu();
            else if (currentState == GameState.Playing || currentState == GameState.WaveTransition) DrawHUD();
            else if (currentState == GameState.LevelUp) DrawLevelUpScreen();
            else if (currentState == GameState.GameOver) DrawGameOver();

            if (currentState == GameState.WaveTransition) DrawWaveTransition();

            if (currentState == GameState.Playing)
            {
                spriteBatch.Draw(vignetteTex, new Rectangle(0, 0, ScreenW, ScreenH), Color.White * 0.7f);
                spriteBatch.Draw(noiseTex, new Rectangle(0, 0, ScreenW, ScreenH), Color.White * 0.03f);

                if (player.Health < 30)
                {
                    float pulse = (float)Math.Sin(gameTime.TotalGameTime.TotalSeconds * 5) * 0.5f + 0.5f;
                    spriteBatch.Draw(pixelTex, new Rectangle(0, 0, ScreenW, ScreenH), Color.Red * (0.25f + pulse * 0.2f));
                }
            }

            spriteBatch.End();

            base.Draw(gameTime);
        }

        void DrawMenu()
        {
            spriteBatch.Draw(pixelTex, new Rectangle(0, 0, ScreenW, ScreenH), new Color(0, 0, 0, 220));

            float pulse = (float)Math.Sin(DateTime.Now.Ticks / 5000000.0) * 0.05f + 1f;
            DrawTextGlow("ARENA ROMA", new Vector2(ScreenW / 2, 200), Gold, 4.0f * pulse, true);
            DrawText("WASD/Arrow Keys to Move | SPACE + movement to Dash | MOUSE to Shoot", new Vector2(ScreenW / 2, 450), DarkGold, 1.0f, true);
            DrawTextGlow("Press ENTER to Begin", new Vector2(ScreenW / 2, 550), Gold, 1.6f, true);
        }

        void DrawHUD()
        {
            DrawText("VIGOR", new Vector2(30, 30), AgedWhite, 0.8f);

            spriteBatch.Draw(pixelTex, new Rectangle(30, 50, 250, 8), new Color(20, 15, 10));
            spriteBatch.Draw(pixelTex, new Rectangle(29, 49, 252, 10), DarkGold * 0.3f);

            float healthPercent = (float)player.Health / player.MaxHealth;
            Color healthColor = healthPercent > 0.5f ? new Color(180, 50, 50) : (healthPercent > 0.25f ? new Color(200, 150, 50) : new Color(220, 50, 50));

            spriteBatch.Draw(pixelTex, new Rectangle(30, 50, (int)(250 * healthPercent), 8), healthColor);
            spriteBatch.Draw(pixelTex, new Rectangle(30, 50, (int)(250 * healthPercent), 2), Color.White * 0.3f);

            DrawText($"{player.Health}", new Vector2(290, 45), AgedWhite, 0.9f);

            DrawText("ASHEN", new Vector2(30, 75), AgedWhite, 0.7f);
            spriteBatch.Draw(pixelTex, new Rectangle(30, 90, 150, 6), new Color(20, 15, 10));
            float dashPercent = MathHelper.Clamp(1f - (player.DashCooldown / 1.2f), 0f, 1f);
            spriteBatch.Draw(pixelTex, new Rectangle(30, 90, (int)(150 * dashPercent), 6), player.IsDashing ? AgedWhite : Gold);

            if (player.DashCooldown <= 0f)
            {
                DrawTextGlow("[SPACE]", new Vector2(190, 85), Gold, 0.8f);
            }

            DrawText($"RUNES: {Score}", new Vector2(ScreenW - 250, 30), Gold, 1.1f);
            DrawText($"WAVE {currentWave}", new Vector2(ScreenW - 250, 60), AgedWhite, 1.0f);

            if (comboCount > 1)
            {
                float comboAlpha = comboTimer / comboTimeout;
                float comboPulse = (float)Math.Sin(DateTime.Now.Ticks / 2000000.0) * 0.1f + 1f;
                DrawTextGlow($"COMBO x{Math.Min(comboCount, 10)}", new Vector2(ScreenW - 250, 100), Gold * comboAlpha, 1.5f * comboPulse);
            }

            spriteBatch.Draw(pixelTex, new Rectangle(30, ScreenH - 40, 300, 4), new Color(20, 15, 10));
            float xpPercent = (float)player.XP / player.XPToNextLevel;
            spriteBatch.Draw(pixelTex, new Rectangle(30, ScreenH - 40, (int)(300 * xpPercent), 4), Gold);
            DrawText($"LEVEL {player.Level}", new Vector2(340, ScreenH - 45), Gold, 0.9f);
        }

        void DrawWaveTransition()
        {
            spriteBatch.Draw(pixelTex, new Rectangle(0, 0, ScreenW, ScreenH), new Color(0, 0, 0, 200));

            float pulse = (float)Math.Sin(DateTime.Now.Ticks / 3000000.0) * 0.1f + 1f;
            DrawTextGlow($"WAVE {currentWave}", new Vector2(ScreenW / 2, ScreenH / 2 - 50), Gold, 4.0f * pulse, true);
            DrawText("Prepare thyself", new Vector2(ScreenW / 2, ScreenH / 2 + 60), AgedWhite, 1.4f, true);
        }

        void DrawLevelUpScreen()
        {
            spriteBatch.Draw(pixelTex, new Rectangle(0, 0, ScreenW, ScreenH), new Color(0, 0, 0, 220));

            float pulse = (float)Math.Sin(DateTime.Now.Ticks / 2000000.0) * 0.1f + 1f;
            DrawTextGlow("LEVEL ASCENDED", new Vector2(ScreenW / 2, 100), Gold, 3.0f * pulse, true);
            DrawText("Choose thy blessing (Press 1, 2, or 3)", new Vector2(ScreenW / 2, 180), AgedWhite, 1.2f, true);

            for (int i = 0; i < currentUpgradeChoices.Count; i++)
            {
                var upg = currentUpgradeChoices[i];
                float x = ScreenW / 2 - 350 + (i * 250);
                float y = 300;

                spriteBatch.Draw(pixelTex, new Rectangle((int)x, (int)y, 200, 250), new Color(25, 20, 15));
                spriteBatch.Draw(pixelTex, new Rectangle((int)x - 1, (int)y - 1, 202, 252), DarkGold * 0.5f);
                spriteBatch.Draw(pixelTex, new Rectangle((int)x, (int)y, 200, 6), upg.Color);

                DrawTextGlow($"[{i + 1}]", new Vector2(x + 100, y + 30), upg.Color, 2.0f, true);
                DrawText(upg.Name, new Vector2(x + 100, y + 90), AgedWhite, 1.2f, true);

                string[] words = upg.Description.Split(' ');
                string line1 = "", line2 = "";
                foreach (var w in words)
                {
                    if (font != null && font.MeasureString(line1 + " " + w).X < 180) line1 += " " + w;
                    else line2 += " " + w;
                }
                DrawText(line1.Trim(), new Vector2(x + 100, y + 140), DarkGold, 0.9f, true);
                DrawText(line2.Trim(), new Vector2(x + 100, y + 165), DarkGold, 0.9f, true);
            }
        }

        void DrawGameOver()
        {
            spriteBatch.Draw(pixelTex, new Rectangle(0, 0, ScreenW, ScreenH), new Color(0, 0, 0, 230));

            DrawTextGlow("THOU HAST FALLEN", new Vector2(ScreenW / 2, 250), new Color(180, 50, 50), 3.5f, true);
            DrawText($"Runes Gathered: {Score}", new Vector2(ScreenW / 2, 350), Gold, 1.5f, true);
            DrawText($"Waves Survived: {currentWave}", new Vector2(ScreenW / 2, 400), AgedWhite, 1.4f, true);
            DrawText("Press 'R' to Rise Again", new Vector2(ScreenW / 2, 500), DarkGold, 1.2f, true);
        }

        // FIXED: Tighter glow - only 1 subtle layer instead of 2
        void DrawTextGlow(string text, Vector2 pos, Color color, float scale, bool center = false)
        {
            if (font == null)
            {
                DrawText(text, pos, color, scale, center);
                return;
            }

            Vector2 origin = center ? font.MeasureString(text) / 2f : Vector2.Zero;

            // Single subtle glow layer at 1.08f scale (was 1.3f and 1.15f)
            spriteBatch.DrawString(font, text, pos, color * 0.4f, 0f, origin, scale * 1.08f, SpriteEffects.None, 0f);

            // Crisp main text on top
            spriteBatch.DrawString(font, text, pos, color, 0f, origin, scale, SpriteEffects.None, 0f);
        }

        void DrawText(string text, Vector2 pos, Color color, float scale, bool center = false)
        {
            Vector2 origin;

            if (font == null)
            {
                float w = text.Length * 15 * scale;
                float h = 25 * scale;
                origin = center ? new Vector2(w / 2f, h / 2f) : Vector2.Zero;
                spriteBatch.Draw(pixelTex, new Rectangle((int)(pos.X - origin.X), (int)(pos.Y - origin.Y), (int)w, (int)h), color * 0.8f);
                return;
            }

            origin = center ? font.MeasureString(text) / 2f : Vector2.Zero;

            spriteBatch.DrawString(font, text, pos + new Vector2(2, 2), Color.Black * 0.5f, 0f, origin, scale, SpriteEffects.None, 0f);

            spriteBatch.DrawString(font, text, pos, color, 0f, origin, scale, SpriteEffects.None, 0f);
        }

        void DrawFloor()
        {
            float scale = TileSize / (float)floorTex.Width;
            for (int y = arena.Top - TileSize; y < ScreenH; y += TileSize)
                for (int x = 0; x < ScreenW; x += TileSize)
                    spriteBatch.Draw(floorTex, new Vector2(x, y), null, Color.White, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
        }

        void DrawShadow(Vector2 ground, float width, float alpha)
        {
            float sx = width / shadowTex.Width;
            spriteBatch.Draw(shadowTex, ground, null, Color.Black * alpha, 0f, new Vector2(shadowTex.Width / 2f, shadowTex.Height / 2f), new Vector2(sx, sx * 0.35f), SpriteEffects.None, DepthOf(ground.Y) - 0.005f);
        }

        Texture2D MakeRadial(int size, Color color)
        {
            Texture2D t = new Texture2D(GraphicsDevice, size, size);
            Color[] data = new Color[size * size];
            float r = size / 2f;
            for (int y = 0; y < size; y++) for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(r, r)) / r;
                float a = MathHelper.Clamp(1f - d, 0f, 1f);
                a *= a;
                data[y * size + x] = color * a;
            }
            t.SetData(data); return t;
        }

        Texture2D MakeVignette()
        {
            Texture2D t = new Texture2D(GraphicsDevice, ScreenW, ScreenH);
            Color[] data = new Color[ScreenW * ScreenH];
            Vector2 center = new Vector2(ScreenW / 2f, ScreenH / 2f);
            float maxDist = Vector2.Distance(center, Vector2.Zero);

            for (int y = 0; y < ScreenH; y++)
                for (int x = 0; x < ScreenW; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), center) / maxDist;
                    float alpha = MathHelper.Clamp(dist * dist * 2.5f - 0.1f, 0f, 1f);
                    data[y * ScreenW + x] = Color.Black * alpha;
                }
            t.SetData(data);
            return t;
        }

        Texture2D MakeNoise(int size)
        {
            Texture2D t = new Texture2D(GraphicsDevice, size, size);
            Color[] data = new Color[size * size];
            for (int i = 0; i < data.Length; i++)
            {
                byte v = (byte)random.Next(256);
                data[i] = new Color(v, v, v, (byte)255);
            }
            t.SetData(data);
            return t;
        }
    }
}