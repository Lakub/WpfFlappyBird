using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.IO;
using System.Media;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Documents.DocumentStructures;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Navigation;
using System.Windows.Shapes;
using FlappyBirdClone.Models;
using Microsoft.EntityFrameworkCore;
using static System.Formats.Asn1.AsnWriter;

namespace FlappyBirdClone
{
    public class Time
    {
        double dS;
        public double deltaSeconds
        {
            get
            {
                return dS* timeScale;
            }
            set
            {
                dS = value;
            }
        }
        public double timeScale = 1;
        
    }
    
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class FlappyBirdWindow : Window
    {
        public Random random;
        public Time time;
        Stopwatch sw;
        Size originalSize;
        int canvasWindowOriginalHeightDiff;
        public Bird bird;
        public Pipe templatePipe;
        public List<Pipe> pipes;
        public List<MovingModulo> backgroundProps;
        int points_val;
        public bool gameStarted=false;
        public int points
        {
            get
            {
                return points_val;
            }
            set
            {
                points_val = value;
                scoreCounter.Text = points.ToString();
            }
        }

        bool muted_val;
        public bool muted
        {
            get
            {
                return muted_val;
            }
            set
            {
                muted_val = value;
                if (muted){
                    SoundButtonImage.Source = FindResource("MutedIcon") as BitmapImage;
                }
                else{
                    SoundButtonImage.Source = FindResource("UnmutedIcon") as BitmapImage;
                }
            }
        }
        TextBlock scoreCounter;
        AppDbContext context;
        public FlappyBirdWindow()
        {
            InitializeComponent();
            context = new();
            muted = true;
            GameOverMenu.Visibility = Visibility.Collapsed;
            PauseMenu.Visibility = Visibility.Collapsed;
            StartMenu.Visibility = Visibility.Visible;
            scoreCounter = ScoreCounterTextBlock;
            points = 0;
            random = new();
            pipes = new();
            originalSize = new Size(Width, GameCanvas.Height);
            canvasWindowOriginalHeightDiff = (int)(Height - GameCanvas.Height);
            time = new();
            time.deltaSeconds=0;
            sw = new();
            sw.Start();
            GameOver = false;
            backgroundProps = new();
            backgroundProps.Add(new MovingModulo(BackClouds,this,3));
            backgroundProps.Add(new MovingModulo(Clouds,this,20));
            backgroundProps.Add(new MovingModulo(Mountains, this, 15));
            backgroundProps.Add(new MovingModulo(Foreground, this, 140));
            backgroundProps.Add(new MovingModulo(MountainsClose, this, 55));
            bird = new Bird(BirdSprite, this);
            templatePipe = new Pipe(PipeSprite, this, false);
            templatePipe.enabled = false;
            CompositionTarget.Rendering += UpdateGameElements;
            SizeChanged += ResizeCanvas;
            KeyDown += AlertBirdKeyboard;
            MouseDown += AlertBirdMouse;
            Deactivated += PauseAction;
        }
        void ResetGame()
        {
            bird.Restart();
            foreach (var p in pipes)
            {
                p.Restart();
            }
            foreach (var e in backgroundProps)
            {
                e.Restart();
            }
            points = 0;
            counter = 0;
            GameOver = false;
            GameOverMenu.Visibility = Visibility.Collapsed;
            SoundButtonParent.Visibility=Visibility.Collapsed;
            sw.Restart();
        }
        public void AlertBirdKeyboard(object o, KeyEventArgs e)
        {
            if (!gameStarted)
                StartGame();
            if (e.Key == Key.Up || e.Key == Key.Space || e.Key == Key.W){
                if (GameOver)
                    ResetGame();
                else
                    bird.PressedJump();
            }
            else if(e.Key == Key.Escape)
            {
                if (time.timeScale > 0)
                    Pause();
                else
                    Unpause();
            }
        }
        void StartGame()
        {
            gameStarted = true;
            StartMenu.Visibility = Visibility.Collapsed;
            SoundButtonParent.Visibility=Visibility.Collapsed;
        }
        void Unpause()
        {
            if (GameOver) return;
            time.timeScale = 1;
            PauseMenu.Visibility= Visibility.Collapsed;
            SoundButtonParent.Visibility=Visibility.Collapsed;
            sw.Restart();
        }
        void Pause()
        {
            if (GameOver) return;
            time.timeScale = 0;
            PauseMenu.Visibility= Visibility.Visible;
            SoundButtonParent.Visibility=Visibility.Visible;
        }
        void ExitAction(object o, EventArgs e)
        {
            Close();
        }
        void PauseAction(object o, EventArgs e)
        {
            Pause();
        }
        void PauseAction(object o, RoutedEventArgs e)
        {
            Pause();
        }
        void UnpauseAction(object o, RoutedEventArgs e)
        {
            Focus();
            Unpause();
        }
        void RestartAction(object o, EventArgs e)
        {
            ResetGame();
        }

        public void AlertBirdMouse(object o, MouseButtonEventArgs e)
        {
            if (!gameStarted)
                StartGame();
            if (!GameOver)
                bird.PressedJump();
        }
        public void ResizeCanvas(object sender, SizeChangedEventArgs e)
        {
            var scale = (GameCanvas.LayoutTransform as ScaleTransform);
            var diffHeight = (e.NewSize.Height- canvasWindowOriginalHeightDiff) / originalSize.Height;
            var diffWidth = e.NewSize.Width / originalSize.Width;
            double scaleVal = 0;
            if (diffWidth > diffHeight)
            {
                scaleVal = diffHeight;
            }
            else
            {
                scaleVal = diffWidth;
            }
            scale.ScaleX = scaleVal;
            scale.ScaleY = scaleVal;
        }
        void CreatePipe()
        {
            if (!gameStarted) return;
            if (GameOver) return;
            var pipe = pipes.Find(e => !e.enabled);
            if (pipe == null)
                templatePipe.DeepCopy();
            else
                pipe.ResetPosition();
        }
        double counter=0;
        public void UpdateGameElements(object sender,EventArgs e)
        {
            time.deltaSeconds = sw.Elapsed.TotalSeconds;
            sw.Restart();
            counter += time.deltaSeconds;
            if (counter >= 3.8)
            {
                counter = 0;
                CreatePipe();
            }
            bird.Update();
            foreach (var element in pipes)
            {
                element.Update();
            }
            foreach(var element in backgroundProps)
                element.Update();
        }

        public void SoundButtonPress(object sender, RoutedEventArgs e)
        {
            muted = !muted;
        }

        public bool GameOver;
        public async void AddScoreButton(object o, RoutedEventArgs e)
        {
            UserInputScoreInput.Visibility = Visibility.Hidden;
            var score = new Score
            {
                UserName = UserInputScoreName.Text,
                Points = points
            };
            await context.Scores.AddAsync(score);
            await context.SaveChangesAsync();
            LeaderboardScoresTable.ItemsSource = await GetTop5ScoresAsync();

        }
        Task<List<Score>> GetTop5ScoresAsync()
        {
            return context.Scores.AsQueryable().OrderByDescending(e => e.Points).Take(5).ToListAsync();
        }
        public async void StopGame()
        {
            GameOver = true;
            UserInputScoreInput.Visibility = Visibility.Visible;
            ScorePresentTextBlock.Text = "Score: " + points;
            SoundButtonParent.Visibility = Visibility.Visible;
            GameOverMenu.Visibility = Visibility.Visible;
            LeaderboardScoresTable.ItemsSource = await GetTop5ScoresAsync();
        }
    }
    public abstract class GameElement
    {
        public FlappyBirdWindow gameWindow;
        public virtual bool enabled
        {
            get;
            set;
        }
        public UIElement uiElement
        {
            get;
            protected set;
        }
        protected GameElement(UIElement uiElement, FlappyBirdWindow gameWindow)
        {
            this.uiElement = uiElement;
            this.gameWindow = gameWindow;
        }
        protected void Move(double x, double y)
        {
            Canvas.SetLeft(uiElement, Canvas.GetLeft(uiElement) + x);
            Canvas.SetTop(uiElement, Canvas.GetTop(uiElement) - y);
        }
        abstract public void Update();
        protected UIElement CopyElement()
        {
            string saved = XamlWriter.Save(uiElement);
            string pattern = "Name\\s*=\\s*\"([a-zA-Z]*|\\s*|\\d*)\"\\s";
            var element = XamlReader.Parse(Regex.Replace(saved, pattern, String.Empty));
            gameWindow.GameCanvas.Children.Add(element as UIElement);
            return element as UIElement;
        }

        public abstract GameElement DeepCopy();
        public void CreateCopy(GameElement gameElement)
        {
            var elementClone = gameElement.DeepCopy();
            gameWindow.GameCanvas.Children.Add(gameElement.uiElement);
        }
    }

    public class MovingModulo : GameElement
    {
        double speed;
        public MovingModulo(UIElement uiElement, FlappyBirdWindow gameWindow, double speed) : base(uiElement, gameWindow)
        {
            Canvas.SetLeft(uiElement, gameWindow.random.Next(-1599, 0));
            Canvas.SetTop(uiElement, 0);
            this.speed = speed;
            enabled = true;
        }
        public void Restart()
        {
            enabled = true;
            Canvas.SetLeft(uiElement, gameWindow.random.Next(-1599, 0));
        }

        public override GameElement DeepCopy()
        {
            throw new NotImplementedException();
        }

        public override void Update()
        {
            if (gameWindow.GameOver) return;
            if (!enabled) return;
            Move(-speed * gameWindow.time.deltaSeconds, 0);
            var x = Canvas.GetLeft(uiElement);
            if (x <= -1600)
            {
                while (x <= -1600)
                    x += 1600;
                Canvas.SetLeft(uiElement, x);
            }
        }
    }

    public class Pipe : GameElement
    {
        int minY = 131;
        int maxY = 406;
        int minYDistance = -701;
        int maxYDistance = -640;
        int startingX = 805;
        int endingX = -105;

        bool gavePoints;

        Pipe topPipe;
        double width, height;
        double[] bounds;
        public override bool enabled
        {
            get
            {
                return uiElement.Visibility == Visibility.Visible;
            }
            set
            {
                if (value)
                    uiElement.Visibility = Visibility.Visible;
                else
                    uiElement.Visibility = Visibility.Collapsed;
            }
        }

        public void Restart()
        {
            if (topPipe != null)
                topPipe.Restart();
            enabled = false;
        }
        public Pipe(UIElement uiElement, FlappyBirdWindow gameWindow, bool enabled = true) : base(uiElement, gameWindow) // for the bottom pipe
        {
            if (enabled)
            {
                Trace.WriteLine("Created new pipes");
                gameWindow.pipes.Add(this);
                topPipe = CreateTopPipe();
                InitBounds();
                ResetPosition();
            }
        }
        public Pipe(UIElement uiElement, FlappyBirdWindow gameWindow, int dum) : base(uiElement, gameWindow)
        { // for the top pipe
            InitBounds();
            ((uiElement as Rectangle).RenderTransform as ScaleTransform).ScaleY = -1;
        }

        void InitBounds()
        {
            width = (uiElement as Rectangle).Width;
            height = (uiElement as Rectangle).Height;
            bounds = new double[4];
        }

        public void ResetPosition()
        {
            enabled = true;
            gavePoints = false;
            Canvas.SetLeft(uiElement, startingX);
            var myY = gameWindow.random.Next(minY, maxY + 1);
            Canvas.SetTop(uiElement, myY);
            topPipe.ResetTopPosition(myY);
        }
        public void ResetTopPosition(int botY)
        {
            enabled = true;
            Canvas.SetLeft(uiElement, startingX);
            var pos = botY + gameWindow.random.Next(minYDistance, maxYDistance + 1);
            Canvas.SetTop(uiElement, pos);
        }

        public Pipe CreateTopPipe()
        {
            return new Pipe(CopyElement(), gameWindow, 0);
        }

        public override GameElement DeepCopy()
        {
            return new Pipe(CopyElement(), gameWindow);
        }

        public bool IsCollidingWithBird(Point[] birdBounds)
        {
            if (gavePoints)
                return false;
            if (topPipe != null){
                if (topPipe.IsCollidingWithBird(birdBounds))
                    return true;
                bounds[2] = Canvas.GetTop(uiElement);
            }
            else
            {
                bounds[2] = double.MinValue;
            }
            bounds[0] = Canvas.GetLeft(uiElement);
            bounds[1] = bounds[0] + width;
            bounds[3] = Canvas.GetTop(uiElement) + height;
            for (int i = 0; i < 4; i++)
            {
                if (IsPointInBounds(birdBounds[i]))
                    return true;
            }
            return false;
        }
        bool IsPointInBounds(Point point)
        {
            // 0 1
            // 2 3
            if (point.X > bounds[0] && point.X < bounds[1]
                && point.Y > bounds[2] && point.Y < bounds[3])
                return true;
            return false;
        }

        public override void Update()
        {
            if (!enabled) return;
            if (!gameWindow.gameStarted) return;
            if (gameWindow.GameOver) return;
            if (topPipe != null)
                topPipe.Update();
            Move(-140 * gameWindow.time.deltaSeconds, 0);
            var x = Canvas.GetLeft(uiElement);
            if (!gavePoints && x <= 49 && topPipe != null)
            {
                gameWindow.points += 1;
                gavePoints = true;
            }
            if (x <= endingX)
                enabled = false;
        }
    }
    public class Bird : GameElement
    {
        bool jumpPressed;
        double verticalAcceleration;
        RotateTransform rotateTransform;
        int startingY = 112;
        public Bird(UIElement uiElement, FlappyBirdWindow gameWindow) : base(uiElement, gameWindow)
        {
            rotateTransform = uiElement.RenderTransform as RotateTransform;
            enabled = true;
        }
        public void Restart()
        {
            Canvas.SetTop(uiElement, startingY);
            verticalAcceleration = 0;
            enabled = true;
            rotateTransform.Angle = 0;
        }
        public override void Update()
        {
            if (!enabled) return;
            if (!gameWindow.gameStarted) return;
            if (!gameWindow.GameOver)
            {
                if (jumpPressed)
                {
                    verticalAcceleration = 400;
                }
                else
                {
                    verticalAcceleration -= 850 * gameWindow.time.deltaSeconds;
                }
            }
            else
            {
                verticalAcceleration -= 850 * gameWindow.time.deltaSeconds;
            }
            rotateTransform.Angle += -verticalAcceleration * 5 * gameWindow.time.deltaSeconds;
            rotateTransform.Angle = Math.Clamp(rotateTransform.Angle, -30, 30);
            Move(0, verticalAcceleration * gameWindow.time.deltaSeconds);
            if (IsColliding())
                gameWindow.StopGame();
            jumpPressed = false;
        }

        public bool IsColliding()
        {
            var bounds = GetBounds();
            if (bounds.Any(e => e.Y >= 440))
            {
                enabled = false;
                return true;
            }
            if (!gameWindow.GameOver)
                foreach (var pipe in gameWindow.pipes)
                {
                    if (!pipe.enabled) continue;
                    if (pipe.IsCollidingWithBird(bounds)) return true;
                }
            return false;
        }
        double leeRoom = 1.5;
        public Point[] GetBounds()
        {
            var x = Canvas.GetLeft(uiElement);
            var y = Canvas.GetTop(uiElement);
            var width = (uiElement as Rectangle).Width;
            var height = (uiElement as Rectangle).Height;
            var transform = rotateTransform.Value;
            Point[] points = new Point[] { new Point(leeRoom, leeRoom), new Point(width - leeRoom, leeRoom), new Point(width - leeRoom, height - leeRoom), new Point(leeRoom, height - leeRoom) };

            double minX = 2000, maxX = 0, minY = 2000, maxY = 0;
            for (int i = 0; i < 4; i++)
            {
                points[i] = points[i] * transform;
                points[i].X += x;
                points[i].Y += y;
            }
            return points;
        }
        public void PressedJump()
        {
            jumpPressed = true;
        }
        public override GameElement DeepCopy()
        {

            return new Bird(CopyElement(), gameWindow);
        }
    }
}