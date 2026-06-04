using System;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.IO;
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
using System.Xml.Linq;

namespace FlappyBirdClone
{
    public class Time
    {
        static double dS;
        static public double deltaSeconds
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
        static public double timeScale = 1;
        
    }
    
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class FlappyBirdWindow : Window
    {
        public Random random;
        Stopwatch sw;
        Size originalSize;
        public Bird bird;
        public Pipe templatePipe;
        public List<Pipe> pipes;
        public List<MovingModulo> backgroundProps;
        static int points_val;
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
        TextBlock scoreCounter;
        public FlappyBirdWindow()
        {
            InitializeComponent();
            scoreCounter = ScoreCounterTextBlock;
            points = 0;
            random = new();
            pipes = new();
            originalSize = new Size(this.Width, Height);
            Time.deltaSeconds=0;
            sw = new();
            sw.Start();
            GameOver = false;
            backgroundProps = new();
            backgroundProps.Add(new MovingModulo(Clouds,this,5));
            backgroundProps.Add(new MovingModulo(Mountains, this, 15));
            backgroundProps.Add(new MovingModulo(Foreground, this, 140));
            bird = new Bird(BirdSprite, this);
            templatePipe = new Pipe(PipeSprite, this, false);
            templatePipe.enabled = false;
            CompositionTarget.Rendering += UpdateGameElements;
            SizeChanged += ResizeCanvas;
            KeyDown += AlertBirdKeyboard;
            MouseDown += AlertBirdMouse;
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
            GameOver = false;
        }
        public void AlertBirdKeyboard(object o, KeyEventArgs e)
        {
            if (e.Key == Key.Up || e.Key == Key.Space || e.Key == Key.W){
                if (GameOver)
                    ResetGame();
                else
                    bird.PressedJump();
            }
        }
        public void AlertBirdMouse(object o, MouseButtonEventArgs e)
        {
            if (GameOver)
                ResetGame();
            else
                bird.PressedJump();
        }
        public void ResizeCanvas(object sender, SizeChangedEventArgs e)
        {
            var scale = (GameCanvas.LayoutTransform as ScaleTransform);
            var diffWidth = e.NewSize.Height / originalSize.Height;
            var diffHeight = e.NewSize.Width / originalSize.Width;
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
            Time.deltaSeconds = sw.Elapsed.TotalSeconds;
            sw.Restart();
            counter += Time.deltaSeconds;
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

        public bool GameOver;
        public void StopGame()
        {
            Trace.WriteLine("Died");
            GameOver=true;
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
            Canvas.SetLeft(uiElement, 0);
            Canvas.SetTop(uiElement, 0);
            this.speed = speed;
            enabled = true;
        }
        public void Restart()
        {
            enabled = true;
            Canvas.SetLeft(uiElement, 0);
        }

        public override GameElement DeepCopy()
        {
            throw new NotImplementedException();
        }

        public override void Update()
        {
            if (gameWindow.GameOver) return;
            if (!enabled) return;
            Move(-speed * Time.deltaSeconds, 0);
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
        static int minY = 131;
        static int maxY = 426;
        static int minYDistance = -701;
        static int maxYDistance = -630;
        static int startingX = 805;
        static int endingX = -105;

        bool gavePoints;

        Pipe topPipe;
        double width, height;
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
            if(topPipe != null)
                topPipe.Restart();
            enabled = false;
        }
        public Pipe(UIElement uiElement, FlappyBirdWindow gameWindow, bool enabled=true) : base(uiElement, gameWindow)
        {
            if(enabled){
                Trace.WriteLine("Created new pipes");
                gameWindow.pipes.Add(this);
                topPipe = CreateTopPipe();
                InitBounds();
                ResetPosition();
            }
        }
        public Pipe(UIElement uiElement, FlappyBirdWindow gameWindow, int dum) : base(uiElement, gameWindow) {
            InitBounds();
        }

        void InitBounds()
        {
            width = (uiElement as Rectangle).Width;
            height = (uiElement as Rectangle).Height;
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
            return new Pipe(CopyElement(),gameWindow,0);
        }

        public override GameElement DeepCopy()
        {
            return new Pipe(CopyElement(),gameWindow);
        }

        public bool IsCollidingWithBird(Point[] birdBounds)
        {
            if (gavePoints)
                return false;
            if (topPipe != null)
                if (topPipe.IsCollidingWithBird(birdBounds))
                    return true;
            foreach(var bPoint in birdBounds)
            {
                if(IsPointInBounds(bPoint))
                    return true;
            }
            return false;
        }
        bool IsPointInBounds(Point point)
        {
            // 0 1
            // 2 3
            var x = Canvas.GetLeft(uiElement);
            var y = Canvas.GetTop(uiElement);
            if (point.X > x && point.X < x + width 
                && point.Y>y && point.Y < y + height)
                return true;
            return false;
        }

        public override void Update()
        {
            if(!enabled) return;
            if (gameWindow.GameOver) return;
            if (topPipe!=null)
                topPipe.Update();
            Move(-140*Time.deltaSeconds, 0);
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
            Canvas.SetTop(uiElement,startingY);
            verticalAcceleration = 0;
            enabled = true;
            rotateTransform.Angle = 0;
        }
        public override void Update()
        {
            if (!enabled) return;
            if(!gameWindow.GameOver){
                if (jumpPressed)
                {
                    verticalAcceleration = 200;
                }
                else
                {
                    verticalAcceleration -= 350*Time.deltaSeconds;
                }

                rotateTransform.Angle += -verticalAcceleration * 5 * Time.deltaSeconds;
                rotateTransform.Angle = Math.Clamp(rotateTransform.Angle, -30, 30);
            }
            else
            {
                verticalAcceleration -= 350 * Time.deltaSeconds;

            }
            Move(0, verticalAcceleration * Time.deltaSeconds);
            if (IsColliding())
                gameWindow.StopGame();
            jumpPressed = false;
        }

        public bool IsColliding()
        {
            var bounds = GetBounds();
            if (bounds[1].Y >= 440)
            {
                enabled = false;
                return true;
            }
            if(!gameWindow.GameOver)
            foreach(var pipe in gameWindow.pipes)
            {
                if (!pipe.enabled) continue;
                if (pipe.IsCollidingWithBird(bounds)) return true;
            }
            return false;
        }
        public Point[] GetBounds()
        {
            var x = Canvas.GetLeft(uiElement);
            var y = Canvas.GetTop(uiElement);
            var width = (uiElement as Rectangle).Width;
            var height = (uiElement as Rectangle).Height;
            var transform = rotateTransform.Value;
            Point[] points = new Point[] { new Point(0, 0), new Point(width, 0), new Point(width, height), new Point(0, height) };
            double minX = 2000, maxX = 0, minY = 2000, maxY = 0;
            for (int i = 0; i < 4; i++)
            {
                var point = points[i] * transform;
                point.X += x;
                point.Y += y;
                if (point.Y > maxY)
                    maxY = point.Y;
                if (point.Y < minY)
                    minY = point.Y;
                if (point.X > maxX)
                    maxX = point.X;
                if (point.X < minX)
                    minX = point.X;
            }
            return new Point[] { new Point(minX, maxY), new Point(maxX,maxY), new Point(maxX, minY), new Point(minX, minY) };
            // 4 3
            // 1 2
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