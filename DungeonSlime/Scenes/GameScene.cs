using System;
using DungeonSlime.ECS.Components;
using DungeonSlime.ECS.Systems;
using DungeonSlime.UI;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGameGum;
using MonoGameLibrary;
using MonoGameLibrary.Graphics;
using MonoGameLibrary.Scenes;
using Friflo.Engine.ECS;

namespace DungeonSlime.Scenes;

public class GameScene : Scene
{
    private enum GameState
    {
        Playing,
        Paused,
        GameOver
    }

    private EcsWorld _world;
    private EcsEntity _slimeEntity;
    private EcsEntity _batEntity;

    // Defines the tilemap to draw.
    private Tilemap _tilemap;

    // Defines the bounds of the room that the slime and bat are contained within.
    private Rectangle _roomBounds;

    // The sound effect to play when the slime eats a bat.
    private SoundEffect _collectSoundEffect;

    // Tracks the players score.
    private int _score;

    private GameSceneUI _ui;

    private GameState _state;

    private SlimeComponent Slime => _slimeEntity.Get<SlimeComponent>();
    private PositionComponent BatPosition => _batEntity.Get<PositionComponent>();
    private VelocityComponent BatVelocity => _batEntity.Get<VelocityComponent>();
    private AnimatedSpriteComponent BatSprite => _batEntity.Get<AnimatedSpriteComponent>();
    private BatComponent Bat => _batEntity.Get<BatComponent>();

    public override void Initialize()
    {
        // LoadContent is called during base.Initialize().
        base.Initialize();

        // During the game scene, we want to disable exit on escape. Instead,
        // the escape key will be used to return back to the title screen.
        Core.ExitOnEscape = false;

        // Create the room bounds by getting the bounds of the screen then
        // using the Inflate method to "Deflate" the bounds by the width and
        // height of a tile so that the bounds only covers the inside room of
        // the dungeon tilemap.
        _roomBounds = Core.GraphicsDevice.PresentationParameters.Bounds;
        _roomBounds.Inflate(-_tilemap.TileWidth, -_tilemap.TileHeight);

        // Subscribe to the slime's BodyCollision event so that a game over
        // can be triggered when this event is raised.
        Slime.BodyCollision += OnSlimeBodyCollision;

        // Create any UI elements from the root element created in previous
        // scenes.
        GumService.Default.Root.Children.Clear();

        // Initialize the user interface for the game scene.
        InitializeUI();

        // Initialize a new game to be played.
        InitializeNewGame();
    }

    private void InitializeUI()
    {
        // Clear out any previous UI element incase we came here
        // from a different scene.
        GumService.Default.Root.Children.Clear();

        // Create the game scene ui instance.
        _ui = new GameSceneUI();

        // Subscribe to the events from the game scene ui.
        _ui.ResumeButtonClick += OnResumeButtonClicked;
        _ui.RetryButtonClick += OnRetryButtonClicked;
        _ui.QuitButtonClick += OnQuitButtonClicked;
    }


    private void OnResumeButtonClicked(object sender, EventArgs args)
    {
        // Change the game state back to playing.
        _state = GameState.Playing;
    }

    private void OnRetryButtonClicked(object sender, EventArgs args)
    {
        // Player has chosen to retry, so initialize a new game.
        InitializeNewGame();
    }

    private void OnQuitButtonClicked(object sender, EventArgs args)
    {
        // Player has chosen to quit, so return back to the title scene.
        Core.ChangeScene(new TitleScene());
    }

    private void InitializeNewGame()
    {
        // Calculate the position for the slime, which will be at the center
        // tile of the tile map.
        Vector2 slimePos = new Vector2();
        slimePos.X = (_tilemap.Columns / 2) * _tilemap.TileWidth;
        slimePos.Y = (_tilemap.Rows / 2) * _tilemap.TileHeight;

        // Initialize the slime.
        Slime.Reset(slimePos, _tilemap.TileWidth);

        // Initialize the bat.
        RandomizeBatVelocity();
        PositionBatAwayFromSlime();

        // Reset the score.
        _score = 0;

        _ui.UpdateScoreText(_score);

        // Set the game state to playing.
        _state = GameState.Playing;
    }

    public override void LoadContent()
    {
        // Create the texture atlas from the XML configuration file.
        TextureAtlas atlas = TextureAtlas.FromFile(Core.Content, "images/atlas-definition.xml");

        // Create the tilemap from the XML configuration file.
        _tilemap = Tilemap.FromFile(Content, "images/tilemap-definition.xml");
        _tilemap.Scale = new Vector2(4.0f, 4.0f);

        // Create the animated sprite for the slime from the atlas.
        AnimatedSprite slimeAnimation = atlas.CreateAnimatedSprite("slime-animation");
        slimeAnimation.Scale = new Vector2(4.0f, 4.0f);

        // Create the animated sprite for the bat from the atlas.
        AnimatedSprite batAnimation = atlas.CreateAnimatedSprite("bat-animation");
        batAnimation.Scale = new Vector2(4.0f, 4.0f);

        // Load the bounce sound effect for the bat.
        SoundEffect bounceSoundEffect = Content.Load<SoundEffect>("audio/bounce");

        // Load the collect sound effect.
        _collectSoundEffect = Content.Load<SoundEffect>("audio/collect");

        _world = new EcsWorld();

        _slimeEntity = _world.CreateEntity();
        _slimeEntity.Add(new SlimeComponent(slimeAnimation));

        _batEntity = _world.CreateEntity();
        _batEntity.Add(new PositionComponent());
        _batEntity.Add(new VelocityComponent());
        _batEntity.Add(new AnimatedSpriteComponent(batAnimation));
        _batEntity.Add(new BatComponent(bounceSoundEffect));

        _world.AddUpdateSystem(new SlimeInputSystem(Core.Input.Keyboard, Core.Input.GamePads[(int)PlayerIndex.One], () => _state == GameState.Playing));
        _world.AddUpdateSystem(new SlimeMovementSystem(() => _state == GameState.Playing));
        _world.AddUpdateSystem(new BatMovementSystem(() => _state == GameState.Playing));

        _world.AddDrawSystem(new SlimeDrawSystem(Core.SpriteBatch));
        _world.AddDrawSystem(new BatDrawSystem(Core.SpriteBatch));
    }

    public override void Update(GameTime gameTime)
    {
        // Ensure the UI is always updated.
        _ui.Update(gameTime);

        // If the game is in a game over state, immediately return back
        // here.
        if (_state == GameState.GameOver)
        {
            return;
        }

        // If the pause button is pressed, toggle the pause state.
        if (GameController.Pause())
        {
            TogglePause();
        }

        // At this point, if the game is paused, just return back early.
        if (_state == GameState.Paused)
        {
            return;
        }

        _world.Update(gameTime);

        // Perform collision checks.
        CollisionChecks();
    }

    private void CollisionChecks()
    {
        // Capture the current bounds of the slime and bat.
        Circle slimeBounds = Slime.GetBounds();
        Circle batBounds = GetBatBounds();

        // FIrst perform a collision check to see if the slime is colliding with
        // the bat, which means the slime eats the bat.
        if (slimeBounds.Intersects(batBounds))
        {
            // Move the bat to a new position away from the slime.
            PositionBatAwayFromSlime();

            // Randomize the velocity of the bat.
            RandomizeBatVelocity();

            // Tell the slime to grow.
            Slime.Grow();

            // Increment the score.
            _score += 100;

            // Update the score display on the UI.
            _ui.UpdateScoreText(_score);

            // Play the collect sound effect.
            Core.Audio.PlaySoundEffect(_collectSoundEffect);
        }

        // Next check if the slime is colliding with the wall by validating if
        // it is within the bounds of the room.  If it is outside the room
        // bounds, then it collided with a wall which triggers a game over.
        if (slimeBounds.Top < _roomBounds.Top ||
            slimeBounds.Bottom > _roomBounds.Bottom ||
            slimeBounds.Left < _roomBounds.Left ||
            slimeBounds.Right > _roomBounds.Right)
        {
            GameOver();
            return;
        }

        // Finally, check if the bat is colliding with a wall by validating if
        // it is within the bounds of the room.  If it is outside the room
        // bounds, then it collided with a wall, and the bat should bounce
        // off of that wall.
        if (batBounds.Top < _roomBounds.Top)
        {
            BounceBat(Vector2.UnitY);
        }
        else if (batBounds.Bottom > _roomBounds.Bottom)
        {
            BounceBat(-Vector2.UnitY);
        }

        if (batBounds.Left < _roomBounds.Left)
        {
            BounceBat(Vector2.UnitX);
        }
        else if (batBounds.Right > _roomBounds.Right)
        {
            BounceBat(-Vector2.UnitX);
        }
    }

    private void PositionBatAwayFromSlime()
    {
        // Calculate the position that is in the center of the bounds
        // of the room.
        float roomCenterX = _roomBounds.X + _roomBounds.Width * 0.5f;
        float roomCenterY = _roomBounds.Y + _roomBounds.Height * 0.5f;
        Vector2 roomCenter = new Vector2(roomCenterX, roomCenterY);

        // Get the bounds of the slime and calculate the center position.
        Circle slimeBounds = Slime.GetBounds();
        Vector2 slimeCenter = new Vector2(slimeBounds.X, slimeBounds.Y);

        // Calculate the distance vector from the center of the room to the
        // center of the slime.
        Vector2 centerToSlime = slimeCenter - roomCenter;

        // Get the bounds of the bat.
        Circle batBounds = GetBatBounds();

        // Calculate the amount of padding we will add to the new position of
        // the bat to ensure it is not sticking to walls
        int padding = batBounds.Radius * 2;

        // Calculate the new position of the bat by finding which component of
        // the center to slime vector (X or Y) is larger and in which direction.
        Vector2 newBatPosition = Vector2.Zero;
        if (Math.Abs(centerToSlime.X) > Math.Abs(centerToSlime.Y))
        {
            // The slime is closer to either the left or right wall, so the Y
            // position will be a random position between the top and bottom
            // walls.
            newBatPosition.Y = Random.Shared.Next(
                _roomBounds.Top + padding,
                _roomBounds.Bottom - padding
            );

            if (centerToSlime.X > 0)
            {
                // The slime is closer to the right side wall, so place the
                // bat on the left side wall.
                newBatPosition.X = _roomBounds.Left + padding;
            }
            else
            {
                // The slime is closer ot the left side wall, so place the
                // bat on the right side wall.
                newBatPosition.X = _roomBounds.Right - padding * 2;
            }
        }
        else
        {
            // The slime is closer to either the top or bottom wall, so the X
            // position will be a random position between the left and right
            // walls.
            newBatPosition.X = Random.Shared.Next(
                _roomBounds.Left + padding,
                _roomBounds.Right - padding
            );

            if (centerToSlime.Y > 0)
            {
                // The slime is closer to the top wall, so place the bat on the
                // bottom wall.
                newBatPosition.Y = _roomBounds.Top + padding;
            }
            else
            {
                // The slime is closer to the bottom wall, so place the bat on
                // the top wall.
                newBatPosition.Y = _roomBounds.Bottom - padding * 2;
            }
        }

        // Assign the new bat position.
        BatPosition.Value = newBatPosition;
    }


    private void RandomizeBatVelocity()
    {
        float angle = (float)(Random.Shared.NextDouble() * MathHelper.TwoPi);
        Vector2 direction = new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle));
        const float movementSpeed = 5.0f;
        BatVelocity.Value = direction * movementSpeed;
    }

    private void BounceBat(Vector2 normal)
    {
        Vector2 newPosition = BatPosition.Value;

        if (normal.X != 0)
        {
            newPosition.X += normal.X * (BatSprite.Sprite.Width * 0.1f);
        }

        if (normal.Y != 0)
        {
            newPosition.Y += normal.Y * (BatSprite.Sprite.Height * 0.1f);
        }

        BatPosition.Value = newPosition;

        normal.Normalize();
        BatVelocity.Value = Vector2.Reflect(BatVelocity.Value, normal);
        Core.Audio.PlaySoundEffect(Bat.BounceSoundEffect);
    }

    private Circle GetBatBounds()
    {
        AnimatedSprite sprite = BatSprite.Sprite;
        Vector2 position = BatPosition.Value;
        int x = (int)(position.X + sprite.Width * 0.5f);
        int y = (int)(position.Y + sprite.Height * 0.5f);
        int radius = (int)(sprite.Width * 0.25f);

        return new Circle(x, y, radius);
    }


    private void OnSlimeBodyCollision(object sender, EventArgs args)
    {
        GameOver();
    }

    private void TogglePause()
    {
        if (_state == GameState.Paused)
        {
            // We're now unpausing the game, so hide the pause panel.
            _ui.HidePausePanel();

            // And set the state back to playing.
            _state = GameState.Playing;
        }
        else
        {
            // We're now pausing the game, so show the pause panel.
            _ui.ShowPausePanel();

            // And set the state to paused.
            _state = GameState.Paused;
        }
    }

    private void GameOver()
    {
        // Show the game over panel.
        _ui.ShowGameOverPanel();

        // Set the game state to game over.
        _state = GameState.GameOver;
    }

    public override void Draw(GameTime gameTime)
    {
        // Clear the back buffer.
        Core.GraphicsDevice.Clear(Color.CornflowerBlue);

        // Begin the sprite batch to prepare for rendering.
        Core.SpriteBatch.Begin(samplerState: SamplerState.PointClamp);

        // Draw the tilemap
        _tilemap.Draw(Core.SpriteBatch);

        _world.Draw(gameTime);

        // Always end the sprite batch when finished.
        Core.SpriteBatch.End();

        // Draw the UI.
        _ui.Draw();
    }
}