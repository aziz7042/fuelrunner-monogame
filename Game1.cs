/*

Fuel Runner ist ein kleines 3D-Spiel im Endless-Runner-Stil. Der Spieler steuert ein Auto, das sich auf einer Straße nach vorne bewegt. Dabei muss man
Hindernissen ausweichen und Treibstoffkanister einsammeln.
Die Steuerung ist einfach: Das Auto kann zwischen drei Fahrspuren nach links und rechts wechseln. Das Spiel endet, wenn man ein Hindernis berührt oder 
wenn der Treibstoff leer ist.
Während der Fahrt sinkt der Tankstand ständig, und durch das Einsammeln von Treibstoffkanistern kann man den Tank wieder auffüllen
Als Feedback sieht der Spieler oben links den Score und den Tankstand. Zusätzlich gibt es eine Tankleiste, die den aktuellen Füllstand anzeigt. und Wenn
nur noch wenig Treibstoff vorhanden ist, erscheint eine Warnung.

*/


using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace FuelRunner;

public class Game1 : Game
{
    // Hier werden die Grafik-Verwaltung und die Zeichen-Objekte gespeichert.
    private GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch;

    private const float PlayerModelRotationY = MathHelper.PiOver2;

    private BasicEffect _effect;
    private SpriteFont _font;
    private Model _playerModel;
    private Model _fuelCanisterModel;
    private Matrix _view;
    private Matrix _projection;
    private VertexPositionColor[] _unitCubeVertices;

    // Hier werden der Spieler-Zustand und die Fahrspur-Logik gespeichert.
    private Vector3 _playerPosition = new Vector3(0, 1, 0);
    private int _currentLane = 0;
    private float _visualX = 0f;
    private const float LaneWidth = 4f;

    // Hier werden alle aktiven Objekte im Spiel gespeichert.
    private List<Entity> _obstacles = new();
    private List<Entity> _fuelCanisters = new();

    // Hier werden die zentralen Spielwerte wie Score, Treibstoff und Geschwindigkeit gespeichert.
    private int _score = 0;
    private float _scoreProgress = 0f;
    private float _fuel = 100f;
    private const float MaxFuel = 100f;
    private const float FuelDrainPerSecond = 8f;
    private const float FuelPickupAmount = 25f;
    private const float FuelSpawnChance = 0.35f;
    private const float BaseGameSpeed = 20f;
    private float _gameSpeed = 20f;
    private bool _isGameOver = false;
    private bool _gameStarted = false;

    // Hier wird der Eingabezustand des letzten Frames gespeichert, damit Tasten nur einmal pro Drücken erkannt werden.
    private KeyboardState _previousState;
    private MouseState _previousMouseState;
    private Rectangle _startButtonRect = new Rectangle(900, 300, 280, 80);
    private Texture2D _pixel;

    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
        Window.Title = "Fuel Runner";
        _graphics.PreferredBackBufferWidth = 1280;
        _graphics.PreferredBackBufferHeight = 720;
    }

    protected override void Initialize()
    {
        // Hier werden Kamera und Projektion für die 3D-Szene eingerichtet.
        _view = Matrix.CreateLookAt(new Vector3(0, 10, 18), new Vector3(0, 0, -5), Vector3.Up);
        _projection = Matrix.CreatePerspectiveFieldOfView(
            MathHelper.ToRadians(45f),
            GraphicsDevice.Viewport.AspectRatio,
            0.1f,
            150f);

        // Hier wird die Würfel-Geometrie für einfache Hindernisse und Markierungen erzeugt.
        _unitCubeVertices = CreateUnitCubeVertices();
        ResetGame();
        base.Initialize();
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);
        _effect = new BasicEffect(GraphicsDevice) { VertexColorEnabled = true };

        // Hier wird die Schriftart optional geladen; wenn sie fehlt, läuft das Spiel trotzdem weiter.
        try { _font = Content.Load<SpriteFont>("DefaultFont"); } catch { }

        // Hier werden die 3D-Modelle geladen.
        _playerModel = Content.Load<Model>("player_vehicle");
        _fuelCanisterModel = Content.Load<Model>("fuel_canister");

        // Hier wird eine 1x1-Textur als Baustein für UI-Rechtecke wie Buttons, Balken und Rahmen erstellt.
        _pixel = new Texture2D(GraphicsDevice, 1, 1);
        _pixel.SetData([Color.White]);
    }

    private void ResetGame()
    {
        // Hier werden die Startwerte für eine neue Runde gesetzt.
        _playerPosition = new Vector3(0, 1, 0);
        _currentLane = 0;
        _visualX = 0f;
        _score = 0;
        _scoreProgress = 0f;
        _fuel = MaxFuel;
        _gameSpeed = BaseGameSpeed;
        _isGameOver = false;

        _obstacles.Clear();
        _fuelCanisters.Clear();

        // Hier werden mehrere Wellen im Voraus erzeugt, damit die Strecke direkt gefüllt ist.
        for (int i = 0; i < 6; i++)
            SpawnWave(-30 - (i * 20));
    }

    private void SpawnWave(float zPos)
    {
        // Hier wird ein Hindernis zufällig auf der linken, mittleren oder rechten Spur erzeugt.
        int lane = Random.Shared.Next(-1, 2);
        _obstacles.Add(new Entity(new Vector3(lane * LaneWidth, 1, zPos), 2.5f, Color.Black));

        // Hier wird mit einer Wahrscheinlichkeit zusätzlich ein Kanister erzeugt.
        if (Random.Shared.NextSingle() < FuelSpawnChance)
        {
            // Hier wird der Kanister bevorzugt auf einer anderen Spur platziert.
            int fuelLane = (lane == 1) ? Random.Shared.Next(-1, 1) : lane + 1;
            _fuelCanisters.Add(new Entity(new Vector3(fuelLane * LaneWidth, 1.6f, zPos), 1.2f, Color.Gold));
        }
    }

    protected override void Update(GameTime gameTime)
    {
        // Hier werden pro Frame die Eingaben und die vergangene Zeit gelesen.
        KeyboardState ks = Keyboard.GetState();
        MouseState ms = Mouse.GetState();
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

        // Hier wird im Start- oder Game-Over-Bildschirm auf Start oder Neustart gewartet.
        if (!_gameStarted || _isGameOver)
        {
            bool enterPressed = ks.IsKeyDown(Keys.Enter) && _previousState.IsKeyUp(Keys.Enter);

            bool mouseClicked =
                ms.LeftButton == ButtonState.Pressed &&
                _previousMouseState.LeftButton == ButtonState.Released &&
                _startButtonRect.Contains(ms.Position);

            if (enterPressed || mouseClicked)
            {
                ResetGame();
                _gameStarted = true;
            }

            _previousState = ks;
            _previousMouseState = ms;
            return;
        }

        // Hier wird ein Spurwechsel nur einmal pro Tastendruck ausgelöst.
        if (ks.IsKeyDown(Keys.Left) && _previousState.IsKeyUp(Keys.Left) && _currentLane > -1)
            _currentLane--;

        if (ks.IsKeyDown(Keys.Right) && _previousState.IsKeyUp(Keys.Right) && _currentLane < 1)
            _currentLane++;

        // Hier wird die Bewegung zur Zielspur weich interpoliert statt abrupt zu springen.
        _visualX = MathHelper.Lerp(_visualX, _currentLane * LaneWidth, dt * 12f);
        _playerPosition.X = _visualX;

        // Hier wird der Treibstoffverbrauch über die Zeit berechnet.
        _fuel -= FuelDrainPerSecond * dt;
        if (_fuel <= 0f)
        {
            _fuel = 0f;
            _isGameOver = true;
        }

        // Hier werden Objekte bewegt, Kollisionen geprüft und alte Objekte entfernt.
        UpdateList(_obstacles, dt, true);
        UpdateList(_fuelCanisters, dt, false);

        // Hier steigen der Score und die Schwierigkeit mit der Zeit.
        _scoreProgress += dt * 10f;
        _score = (int)_scoreProgress;
        _gameSpeed = BaseGameSpeed + (_scoreProgress * 0.06f);

        _previousState = ks;
        _previousMouseState = ms;
        base.Update(gameTime);
    }

    private void UpdateList(List<Entity> list, float dt, bool isObstacle)
    {
        // Hier wird die Kollision mit zwei Boxen geprüft: Vorderteil und Hauptkörper.
        Vector3 frontHalfSize = new Vector3(0.7f, 0.6f, 0.5f);
        Vector3 frontCenter = _playerPosition + new Vector3(0f, 0.4f, -5f);
        BoundingBox frontBox = new BoundingBox(frontCenter - frontHalfSize, frontCenter + frontHalfSize);

        Vector3 bodyHalfSize = new Vector3(0.9f, 0.6f, 1.8f);
        Vector3 bodyCenter = _playerPosition + new Vector3(0f, 0.4f, 0.8f);
        BoundingBox bodyBox = new BoundingBox(bodyCenter - bodyHalfSize, bodyCenter + bodyHalfSize);

        for (int i = list.Count - 1; i >= 0; i--)
        {
            var e = list[i];
            // Hier wird das Objekt in Richtung Spieler bewegt.
            e.Position.Z += _gameSpeed * dt;

            Vector3 entityHalfSize;

            if (isObstacle)
                entityHalfSize = new Vector3(1.0f, 1.0f, 1.0f);
            else
                entityHalfSize = new Vector3(0f, 0f, -0.5f);

            BoundingBox eBox = new BoundingBox(e.Position - entityHalfSize, e.Position + entityHalfSize);

            bool intersects = frontBox.Intersects(eBox) || bodyBox.Intersects(eBox);

            if (intersects)
            {
                if (isObstacle)
                {
                    // Hier wird bei einer Hindernis-Kollision die Runde beendet.
                    _isGameOver = true;
                }
                else
                {
                    // Hier wird bei einer Kanister-Kollision der Treibstoff bis zur Obergrenze aufgefüllt.
                    _fuel += FuelPickupAmount;
                    if (_fuel > MaxFuel)
                        _fuel = MaxFuel;

                    list.RemoveAt(i);
                    continue;
                }
            }

            if (e.Position.Z > 15)
            {
                // Hier wird ein Objekt hinter der Kamera entfernt und bei Bedarf eine neue Welle erzeugt.
                list.RemoveAt(i);
                if (isObstacle)
                    SpawnWave(-100);
            }
            else
            {
                list[i] = e;
            }
        }
    }

    protected override void Draw(GameTime gameTime)
    {
        // Hier wird die 3D-Szene vorbereitet.
        GraphicsDevice.Clear(Color.CornflowerBlue);
        GraphicsDevice.DepthStencilState = DepthStencilState.Default;

        // Hier werden Boden, Spurmarkierungen, Spieler und Objekte gezeichnet.
        DrawEntity(new Vector3(0, -0.1f, -40), new Vector3(15, 0.1f, 120), Color.DarkSlateGray);
        DrawLaneMarkings();

        DrawPlayerModel(_playerPosition + new Vector3(0f, 0f, 0.2f), 1.0f);

        foreach (var obstacle in _obstacles)
            DrawEntity(obstacle.Position, new Vector3(obstacle.Size), obstacle.Color);

        foreach (var fuelCanister in _fuelCanisters)
            DrawFuelCanisterModel(fuelCanister.Position + new Vector3(0f, 0.15f, 0f), 0.4f);

        // Hier wird danach die 2D-Benutzeroberfläche über die 3D-Welt gelegt.
        _spriteBatch.Begin();

        string text = !_gameStarted
            ? "FUEL RUNNER"
            : _isGameOver
                ? $"GAME OVER! SCORE: {_score}"
                : $"SCORE: {_score}";

        Vector2 pos = new Vector2(50, 50);
        if (_font != null)
            _spriteBatch.DrawString(_font, text, pos, _isGameOver ? Color.Red : Color.White);

        Rectangle fuelBarBack = new Rectangle(50, 100, 300, 25);
        _spriteBatch.Draw(_pixel, fuelBarBack, Color.DarkGray);

        int fuelWidth = (int)(300 * (_fuel / MaxFuel));
        Rectangle fuelBarFill = new Rectangle(50, 100, fuelWidth, 25);
        _spriteBatch.Draw(_pixel, fuelBarFill, GetFuelBarColor());

        // Hier wird der Rahmen für den Treibstoffbalken gezeichnet.
        _spriteBatch.Draw(_pixel, new Rectangle(48, 98, 304, 2), Color.White);
        _spriteBatch.Draw(_pixel, new Rectangle(48, 127, 304, 2), Color.White);
        _spriteBatch.Draw(_pixel, new Rectangle(48, 98, 2, 29), Color.White);
        _spriteBatch.Draw(_pixel, new Rectangle(350, 98, 2, 29), Color.White);

        if (_font != null)
            _spriteBatch.DrawString(_font, $"FUEL: {(int)_fuel}", new Vector2(50, 130), Color.White);

        if (_gameStarted && !_isGameOver && _fuel <= 25f && _font != null)
            _spriteBatch.DrawString(_font, "LOW FUEL!", new Vector2(50, 165), Color.Red);

        // Hier wird das Start- oder Game-Over-Overlay mit Anleitung und Button gezeichnet.
        if (!_gameStarted || _isGameOver)
        {
            MouseState ms = Mouse.GetState();
            bool hover = _startButtonRect.Contains(ms.Position);

            if (_font != null)
            {
                string titleText = "How to play:";
                string rule1 = "Avoid obstacles.";
                string rule2 = "Collect fuel canisters.";
                string controlsHeader = "Controls:";
                string controls1 = "Left / Right Arrow: Change lane";
                string controls2 = "ENTER or click button: Start / Restart";

                float leftX = 50f;
                float baseY = 220f;

                _spriteBatch.DrawString(_font, titleText, new Vector2(leftX, baseY), Color.Red);
                _spriteBatch.DrawString(_font, rule1, new Vector2(leftX, baseY + 30f), Color.White);
                _spriteBatch.DrawString(_font, rule2, new Vector2(leftX, baseY + 58f), Color.White);
                _spriteBatch.DrawString(_font, controlsHeader, new Vector2(leftX, baseY + 96f), Color.Red);
                _spriteBatch.DrawString(_font, controls1, new Vector2(leftX, baseY + 126f), Color.White);
                _spriteBatch.DrawString(_font, controls2, new Vector2(leftX, baseY + 154f), Color.White);
            }

            Color buttonColor = hover ? Color.Orange : Color.DarkOrange;
            _spriteBatch.Draw(_pixel, _startButtonRect, buttonColor);

            if (_font != null)
            {
                string buttonText = !_gameStarted ? "START GAME" : "RESTART";
                Vector2 textSize = _font.MeasureString(buttonText);
                Vector2 textPos = new Vector2(
                    _startButtonRect.X + (_startButtonRect.Width - textSize.X) / 2f,
                    _startButtonRect.Y + (_startButtonRect.Height - textSize.Y) / 2f
                );

                _spriteBatch.DrawString(_font, buttonText, textPos, Color.White);
            }
        }

        _spriteBatch.End();

        base.Draw(gameTime);
    }

    private void DrawEntity(Vector3 pos, Vector3 scale, Color color)
    {
        // Hier wird ein einfacher farbiger Würfel mit eigener Weltmatrix gezeichnet.
        _effect.World = Matrix.CreateScale(scale) * Matrix.CreateTranslation(pos);
        _effect.View = _view;
        _effect.Projection = _projection;
        _effect.DiffuseColor = color.ToVector3();

        foreach (var pass in _effect.CurrentTechnique.Passes)
        {
            pass.Apply();
            GraphicsDevice.DrawUserPrimitives(PrimitiveType.TriangleList, _unitCubeVertices, 0, 12);
        }
    }

    private void DrawPlayerModel(Vector3 position, float scale)
    {
        // Hier wird das Spieler-Modell skaliert, gedreht und an die richtige Position gesetzt.
        Matrix world =
            Matrix.CreateScale(scale) *
            Matrix.CreateRotationY(PlayerModelRotationY) *
            Matrix.CreateTranslation(position);

        foreach (ModelMesh mesh in _playerModel.Meshes)
        {
            foreach (BasicEffect effect in mesh.Effects)
            {
                effect.World = world;
                effect.View = _view;
                effect.Projection = _projection;
                effect.EnableDefaultLighting();
                effect.PreferPerPixelLighting = true;
                effect.TextureEnabled = false;
                effect.VertexColorEnabled = false;
                effect.DiffuseColor = Color.Blue.ToVector3();
            }

            mesh.Draw();
        }
    }

    private void DrawFuelCanisterModel(Vector3 position, float scale)
    {
        // Hier wird das Kanister-Modell ohne Licht dargestellt, damit es leicht leuchtet.
        Matrix world =
            Matrix.CreateScale(scale) *
            Matrix.CreateTranslation(position);

        foreach (ModelMesh mesh in _fuelCanisterModel.Meshes)
        {
            foreach (BasicEffect effect in mesh.Effects)
            {
                Vector3 originalDiffuse = effect.DiffuseColor;

                effect.World = world;
                effect.View = _view;
                effect.Projection = _projection;
                effect.TextureEnabled = effect.Texture != null;
                effect.VertexColorEnabled = false;
                effect.LightingEnabled = false;

                if (!effect.TextureEnabled)
                {
                    if (originalDiffuse == Vector3.Zero)
                        originalDiffuse = new Vector3(1f, 0.45f, 0.1f);

                    effect.DiffuseColor = originalDiffuse;
                    effect.EmissiveColor = originalDiffuse * 0.8f;
                }
            }

            mesh.Draw();
        }
    }

    private Color GetFuelBarColor()
    {
        // Hier wird die Farbe des Balkens je nach verbleibendem Treibstoff bestimmt.
        float fuelPercent = _fuel / MaxFuel;

        if (fuelPercent > 0.6f)
            return Color.LimeGreen;
        if (fuelPercent > 0.3f)
            return Color.Orange;

        return Color.Red;
    }

    private void DrawLaneMarkings()
    {
        // Hier werden gestrichelte Linien für die drei Fahrspuren gezeichnet.
        float laneLineWidth = 0.15f;
        float laneLineHeight = 0.02f;
        float segmentLength = 4f;
        float gapLength = 3f;

        for (float z = 10f; z >= -110f; z -= (segmentLength + gapLength))
        {
            DrawEntity(
                new Vector3(-2f, 0.01f, z),
                new Vector3(laneLineWidth, laneLineHeight, segmentLength),
                Color.White
            );

            DrawEntity(
                new Vector3(2f, 0.01f, z),
                new Vector3(laneLineWidth, laneLineHeight, segmentLength),
                Color.White
            );
        }
    }

    private struct Entity(Vector3 p, float s, Color c)
    {
        // Hier wird ein kleiner Datencontainer für Hindernisse und Kanister definiert.
        public Vector3 Position = p;
        public float Size = s;
        public Color Color = c;
    }

    private VertexPositionColor[] CreateUnitCubeVertices()
    {
        // Hier werden 12 Dreiecke mit insgesamt 36 Vertices für einen Einheitswürfel erzeugt.
        Color c = Color.White;
        float h = 0.5f;
        Vector3 pLBB = new Vector3(-h, -h, -h), pRBB = new Vector3(h, -h, -h), pLBT = new Vector3(-h, h, -h), pRBT = new Vector3(h, h, -h);
        Vector3 pLFB = new Vector3(-h, -h, h), pRFB = new Vector3(h, -h, h), pLFT = new Vector3(-h, h, h), pRFT = new Vector3(h, h, h);

        return
        [
            new(pLFB,c), new(pLFT,c), new(pRFT,c), new(pLFB,c), new(pRFT,c), new(pRFB,c),
            new(pRBB,c), new(pRBT,c), new(pLBT,c), new(pRBB,c), new(pLBT,c), new(pLBB,c),
            new(pLBB,c), new(pLBT,c), new(pLFT,c), new(pLBB,c), new(pLFT,c), new(pLFB,c),
            new(pRFB,c), new(pRFT,c), new(pRBT,c), new(pRFB,c), new(pRBT,c), new(pRBB,c),
            new(pLFT,c), new(pLBT,c), new(pRBT,c), new(pLFT,c), new(pRBT,c), new(pRFT,c),
            new(pLBB,c), new(pLFB,c), new(pRFB,c), new(pLBB,c), new(pRFB,c), new(pRBB,c)
        ];
    }
}
