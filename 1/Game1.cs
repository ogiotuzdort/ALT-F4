using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;

namespace _1;

enum GameState { Menu, Settings, Playing, Paused, GameOver }
enum WeaponType { Pistol, SMG, Shotgun }
enum Language { English, Turkish }

public class Game1 : Game
{
    private GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch;

    GameState currentState = GameState.Menu;
    GameState previousState = GameState.Menu;
    WeaponType currentWeapon = WeaponType.Pistol;
    Language currentLang = Language.English;

    Texture2D whitePixel, playerTexture, enemyTexture, backgroundTexture, heartTexture, bulletTexture;
    SpriteFont gameFont;
    Matrix cameraMatrix;
    MouseState oldMouse; KeyboardState oldKey;

    bool isMouseTrackingActive = true;
    Vector2 playerPos;
    float playerAngle = 0f;
    int health = 3;
    int score = 0;
    float preparationTime = 5.0f;
    
    // HARITA SINIRLARI
    const int MapWidth = 1400;
    const int MapHeight = 1400;

    List<Vector2> bullets = new List<Vector2>();
    List<Vector2> bulletDirections = new List<Vector2>();
    List<Vector2> enemies = new List<Vector2>();
    float lastShotTime = 0, lastEnemySpawnTime = 0;

    Rectangle btnStart = new Rectangle(540, 200, 200, 60);
    Rectangle btnSettings = new Rectangle(540, 280, 200, 60);
    Rectangle btnExit = new Rectangle(540, 360, 200, 60);
    Rectangle btnFullScreen = new Rectangle(540, 150, 200, 60);
    Rectangle btnWindowed = new Rectangle(540, 230, 200, 60);
    Rectangle btnMouseToggle = new Rectangle(540, 310, 200, 60);
    Rectangle btnLangToggle = new Rectangle(540, 390, 200, 60);
    Rectangle btnBack = new Rectangle(540, 480, 200, 60);
    Rectangle btnResume = new Rectangle(540, 220, 200, 60);
    Rectangle btnPauseSettings = new Rectangle(540, 300, 200, 60);
    Rectangle btnMainMenu = new Rectangle(540, 380, 200, 60);
    Rectangle btnPauseExit = new Rectangle(540, 460, 200, 60);

    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
        _graphics.PreferredBackBufferWidth = 1280;
        _graphics.PreferredBackBufferHeight = 720;
    }

    protected override void Initialize() { ResetGame(); base.Initialize(); }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);
        whitePixel = new Texture2D(GraphicsDevice, 1, 1);
        whitePixel.SetData(new[] { Color.White });
        
        try { gameFont = Content.Load<SpriteFont>("anaFont"); } catch { }
        try { playerTexture = Content.Load<Texture2D>("karakter"); } catch { }
        try { enemyTexture = Content.Load<Texture2D>("dusman"); } catch { }
        try { backgroundTexture = Content.Load<Texture2D>("arkaplan"); } catch { }
        try { heartTexture = Content.Load<Texture2D>("kalp"); } catch { }
        try { bulletTexture = Content.Load<Texture2D>("bullet"); } catch { bulletTexture = null; }
    }

    private void ResetGame()
    {
        health = 3; score = 0; preparationTime = 5.0f;
        playerPos = new Vector2(MapWidth / 2, MapHeight / 2);
        bullets.Clear(); bulletDirections.Clear(); enemies.Clear();
    }

    protected override void Update(GameTime gameTime)
    {
        var m = Mouse.GetState();
        var k = Keyboard.GetState();
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        bool isClicked = m.LeftButton == ButtonState.Pressed && oldMouse.LeftButton == ButtonState.Released;

        if (k.IsKeyDown(Keys.P) && oldKey.IsKeyUp(Keys.P))
        {
            if (currentState == GameState.Playing) currentState = GameState.Paused;
            else if (currentState == GameState.Paused) currentState = GameState.Playing;
        }

        switch (currentState)
        {
            case GameState.Menu:
                if (isClicked) {
                    if (btnStart.Contains(m.Position)) { ResetGame(); currentState = GameState.Playing; }
                    if (btnSettings.Contains(m.Position)) { previousState = GameState.Menu; currentState = GameState.Settings; }
                    if (btnExit.Contains(m.Position)) Exit();
                }
                break;

            case GameState.Playing:
                UpdateCamera();
                Vector2 worldMouse = Vector2.Transform(m.Position.ToVector2(), Matrix.Invert(cameraMatrix));
                if (isMouseTrackingActive)
                    playerAngle = (float)Math.Atan2(worldMouse.Y - playerPos.Y, worldMouse.X - playerPos.X);

                if (k.IsKeyDown(Keys.E) && oldKey.IsKeyUp(Keys.E)) 
                    currentWeapon = (WeaponType)(((int)currentWeapon + 1) % 3);

                if (preparationTime > 0) {
                    preparationTime -= dt;
                    UpdatePlayerOnly(k, dt);
                } else {
                    UpdateGameplay(k, m, dt);
                }
                break;

            case GameState.Settings:
                if (isClicked) {
                    if (btnFullScreen.Contains(m.Position)) { _graphics.IsFullScreen = true; _graphics.ApplyChanges(); }
                    if (btnWindowed.Contains(m.Position)) { _graphics.IsFullScreen = false; _graphics.ApplyChanges(); }
                    if (btnMouseToggle.Contains(m.Position)) isMouseTrackingActive = !isMouseTrackingActive;
                    if (btnLangToggle.Contains(m.Position)) currentLang = (currentLang == Language.English) ? Language.Turkish : Language.English;
                    if (btnBack.Contains(m.Position)) currentState = previousState;
                }
                break;

            case GameState.Paused:
                if (isClicked) {
                    if (btnResume.Contains(m.Position)) currentState = GameState.Playing;
                    if (btnPauseSettings.Contains(m.Position)) { previousState = GameState.Paused; currentState = GameState.Settings; }
                    if (btnMainMenu.Contains(m.Position)) currentState = GameState.Menu;
                    if (btnPauseExit.Contains(m.Position)) Exit();
                }
                break;

            case GameState.GameOver:
                if (isClicked) currentState = GameState.Menu;
                break;
        }

        oldMouse = m; oldKey = k;
        base.Update(gameTime);
    }

    private void UpdateCamera()
    {
        Vector2 camPos = new Vector2(
            MathHelper.Clamp(playerPos.X, 640, MapWidth - 640),
            MathHelper.Clamp(playerPos.Y, 360, MapHeight - 360));
        cameraMatrix = Matrix.CreateTranslation(new Vector3(-camPos, 0)) * Matrix.CreateTranslation(new Vector3(640, 360, 0));
    }

    private void UpdatePlayerOnly(KeyboardState k, float dt)
    {
        float speed = 550f;
        Vector2 move = Vector2.Zero;
        if (k.IsKeyDown(Keys.W)) move.Y -= 1;
        if (k.IsKeyDown(Keys.S)) move.Y += 1;
        if (k.IsKeyDown(Keys.A)) move.X -= 1;
        if (k.IsKeyDown(Keys.D)) move.X += 1;

        if (move != Vector2.Zero) {
            move.Normalize();
            Vector2 nextPos = playerPos + move * speed * dt;
            playerPos.X = MathHelper.Clamp(nextPos.X, 35, MapWidth - 35);
            playerPos.Y = MathHelper.Clamp(nextPos.Y, 35, MapHeight - 35);
            if (!isMouseTrackingActive) playerAngle = (float)Math.Atan2(move.Y, move.X);
        }
    }

    private void UpdateGameplay(KeyboardState k, MouseState m, float dt)
    {
        UpdatePlayerOnly(k, dt);
        lastShotTime += dt;
        float delay = currentWeapon switch { WeaponType.Pistol => 0.4f, WeaponType.SMG => 0.07f, _ => 1.1f };

        if (m.LeftButton == ButtonState.Pressed && lastShotTime >= delay) {
            FireWeapon();
            lastShotTime = 0;
        }

        for (int i = 0; i < bullets.Count; i++) {
            bullets[i] += bulletDirections[i] * 1800 * dt;
            if (bullets[i].X < 0 || bullets[i].X > MapWidth || bullets[i].Y < 0 || bullets[i].Y > MapHeight) { 
                bullets.RemoveAt(i); bulletDirections.RemoveAt(i); i--; 
            }
        }

        lastEnemySpawnTime += dt;
        if (lastEnemySpawnTime > 0.6f) {
            Random r = new Random();
            Vector2 spawnPos;
            int attempts = 0;

            
            do {
                spawnPos = new Vector2(r.Next(50, MapWidth - 50), r.Next(50, MapHeight - 50));
                attempts++;
            } while (Vector2.Distance(spawnPos, playerPos) < 400 && attempts < 100);

            enemies.Add(spawnPos);
            lastEnemySpawnTime = 0;
        }

        for (int i = 0; i < enemies.Count; i++) {
            Vector2 eDir = playerPos - enemies[i];
            if (eDir != Vector2.Zero) eDir.Normalize();
            
            // DÜZENLEME: Düşman hızı 240'tan 170'e düşürüldü
            enemies[i] += eDir * 170 * dt;

            for (int j = 0; j < bullets.Count; j++) {
                if (Vector2.Distance(enemies[i], bullets[j]) < 40) {
                    enemies.RemoveAt(i); bullets.RemoveAt(j); bulletDirections.RemoveAt(j);
                    score += 10; i--; break;
                }
            }
            if (i >= 0 && Vector2.Distance(playerPos, enemies[i]) < 45) {
                health--; enemies.RemoveAt(i); i--;
                if (health <= 0) currentState = GameState.GameOver;
            }
        }
    }

    private void FireWeapon()
    {
        if (currentWeapon == WeaponType.Shotgun) {
            for (int i = -2; i <= 2; i++) {
                float spread = i * 0.15f;
                bulletDirections.Add(new Vector2((float)Math.Cos(playerAngle + spread), (float)Math.Sin(playerAngle + spread)));
                bullets.Add(playerPos);
            }
        } else {
            float spread = 0;
            if (currentWeapon == WeaponType.SMG) {
                Random r = new Random();
                spread = (float)(r.NextDouble() * 0.14 - 0.07);
            }
            bulletDirections.Add(new Vector2((float)Math.Cos(playerAngle + spread), (float)Math.Sin(playerAngle + spread)));
            bullets.Add(playerPos);
        }
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(new Color(20, 20, 20));

        if (currentState == GameState.Playing || currentState == GameState.Paused || (currentState == GameState.Settings && previousState == GameState.Paused)) {
            _spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied, SamplerState.PointClamp, null, null, null, cameraMatrix);
            
            _spriteBatch.Draw(backgroundTexture ?? whitePixel, new Rectangle(0, 0, MapWidth, MapHeight), backgroundTexture != null ? Color.White : Color.White * 0.05f);

            for (int i = 0; i < bullets.Count; i++)
            {
                if (bulletTexture != null) {
                    float mAngle = (float)Math.Atan2(bulletDirections[i].Y, bulletDirections[i].X);
                    float correctedAngle = mAngle + MathHelper.PiOver2;
                    Vector2 mOrg = new Vector2(bulletTexture.Width / 2f, bulletTexture.Height / 2f);
                    _spriteBatch.Draw(bulletTexture, bullets[i], null, Color.White, correctedAngle, mOrg, 1.1f, SpriteEffects.None, 0);
                } else {
                    _spriteBatch.Draw(whitePixel, new Rectangle((int)bullets[i].X - 6, (int)bullets[i].Y - 6, 12, 12), Color.Yellow);
                }
            }

            foreach (var d in enemies) {
                float dAngle = (float)Math.Atan2(playerPos.Y - d.Y, playerPos.X - d.X);
                Vector2 dOrg = enemyTexture != null ? new Vector2(enemyTexture.Width / 2f, enemyTexture.Height / 2f) : Vector2.Zero;
                _spriteBatch.Draw(enemyTexture ?? whitePixel, d, null, Color.White, dAngle, dOrg, (enemyTexture != null ? 0.2f : 30f), SpriteEffects.None, 0);
            }

            Vector2 cOrg = playerTexture != null ? new Vector2(playerTexture.Width / 2f, playerTexture.Height / 2f) : Vector2.Zero;
            _spriteBatch.Draw(playerTexture ?? whitePixel, playerPos, null, Color.White, playerAngle, cOrg, (playerTexture != null ? 0.2f : 60f), SpriteEffects.None, 0);
            
            _spriteBatch.End();
        }

        _spriteBatch.Begin();
        DrawUI();
        _spriteBatch.End();
    }

    private void DrawUI()
    {
        bool isTr = currentLang == Language.Turkish;
        if (gameFont == null) return;

        if (currentState == GameState.Menu) {
            DrawBtn(btnStart, isTr ? "BASLAT" : "START", Color.Green);
            DrawBtn(btnSettings, isTr ? "AYARLAR" : "SETTINGS", Color.Gray);
            DrawBtn(btnExit, isTr ? "CIKIS" : "QUIT", Color.Maroon);
        } 
        else if (currentState == GameState.Settings) {
            _spriteBatch.Draw(whitePixel, new Rectangle(0, 0, 1280, 720), Color.Black * 0.5f);
            DrawBtn(btnFullScreen, isTr ? "TAM EKRAN" : "FULLSCREEN", Color.DodgerBlue);
            DrawBtn(btnWindowed, isTr ? "PENCERE" : "WINDOWED", Color.DodgerBlue);
            string aimTxt = isTr ? (isMouseTrackingActive ? "BAKIS: MOUSE" : "BAKIS: HAREKET") : (isMouseTrackingActive ? "AIM: MOUSE" : "AIM: MOVEMENT");
            DrawBtn(btnMouseToggle, aimTxt, isMouseTrackingActive ? Color.Green : Color.Orange);
            DrawBtn(btnLangToggle, isTr ? "DIL: TURKCE" : "LANG: ENGLISH", Color.Purple);
            DrawBtn(btnBack, isTr ? "GERI" : "BACK", Color.DarkGray);
        }
        else if (currentState == GameState.Paused) {
            _spriteBatch.Draw(whitePixel, new Rectangle(0, 0, 1280, 720), Color.Black * 0.7f);
            DrawBtn(btnResume, isTr ? "DEVAM ET" : "RESUME", Color.Green);
            DrawBtn(btnPauseSettings, isTr ? "AYARLAR" : "SETTINGS", Color.Gray);
            DrawBtn(btnMainMenu, isTr ? "ANA MENU" : "MAIN MENU", Color.Orange);
            DrawBtn(btnPauseExit, isTr ? "CIKIS" : "QUIT", Color.Maroon);
        } 
        else if (currentState == GameState.Playing) {
            for (int i = 0; i < health; i++) {
                if (heartTexture != null) _spriteBatch.Draw(heartTexture, new Vector2(25 + (i * 45), 25), null, Color.White, 0f, Vector2.Zero, 0.25f, SpriteEffects.None, 0);
                else _spriteBatch.DrawString(gameFont, "HP", new Vector2(25 + (i * 40), 25), Color.Red);
            }
            string wName = currentWeapon switch { WeaponType.Pistol => isTr ? "TABANCA" : "PISTOL", WeaponType.SMG => isTr ? "TARAMALI" : "SMG", WeaponType.Shotgun => isTr ? "POMPALI" : "SHOTGUN", _ => "" };
            _spriteBatch.DrawString(gameFont, $"{(isTr ? "SKOR" : "SCORE")}: {score}", new Vector2(25, 75), Color.Gold);
            _spriteBatch.DrawString(gameFont, $"{(isTr ? "SILAH" : "WEAPON")}: {wName}", new Vector2(25, 115), Color.White);
            if (preparationTime > 0) _spriteBatch.DrawString(gameFont, $"{(isTr ? "HAZIRLAN" : "GET READY")}: {(int)preparationTime}", new Vector2(580, 50), Color.Yellow);
        } 
        else if (currentState == GameState.GameOver) {
            _spriteBatch.DrawString(gameFont, isTr ? "OYUN BITTI" : "GAME OVER", new Vector2(480, 300), Color.Red, 0, Vector2.Zero, 2f, SpriteEffects.None, 0);
            _spriteBatch.DrawString(gameFont, $"{(isTr ? "TOPLAM SKOR" : "FINAL SCORE")}: {score}", new Vector2(560, 400), Color.White);
        }
    }

    void DrawBtn(Rectangle r, string t, Color c) { 
        _spriteBatch.Draw(whitePixel, r, c); 
        Vector2 size = gameFont.MeasureString(t);
        Vector2 pos = new Vector2(r.X + (r.Width - size.X) / 2, r.Y + (r.Height - size.Y) / 2);
        _spriteBatch.DrawString(gameFont, t, pos, Color.White);
    }
}